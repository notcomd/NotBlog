using System.Text.Json;
using System.Text.Json.Serialization;
using CacheMemory.Core;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// 权限映射双层持久化 — Redis（主存）+ JSON 文件（本地回退）
///
/// 设计要点：
///   - Redis 为主存：跨实例共享、多 Yarp 副本读取同一份映射、TTL 防过期
///   - JSON 为本地回退：Redis 不可用或首次启动时从文件加载
///   - 写入：双写（Redis + JSON），任一失败不影响另一个
///   - 读取：Redis 优先 → JSON 回退 → null
///   - 文件路径：{ContentRoot}/permission-mappings.json
///   - Redis Key：yarp:permission-mappings
///   - Redis TTL：24h（轮询 5 分钟续期，事件驱动即时刷新）
/// </summary>
public class PermissionMappingStore(
    IWebHostEnvironment env,
    ILogger<PermissionMappingStore> logger,
    IRedisCacheService? redis = null)
{
    private const string RedisKey = "yarp:permission-mappings";
    private static readonly TimeSpan RedisTtl = TimeSpan.FromHours(24);

    private readonly string _filePath = Path.Combine(env.ContentRootPath, "permission-mappings.json");
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 保存映射到 Redis + JSON 文件（双写）
    /// </summary>
    public async Task SaveAsync(IReadOnlyList<PermissionMappingDto> mappings, CancellationToken ct = default)
    {
        // 1. 写 Redis（主存，跨实例共享）
        if (redis is not null)
        {
            try
            {
                var json = JsonSerializer.Serialize(mappings, JsonOptions);
                await redis.StringSetAsync(RedisKey, json, RedisTtl, ct);
                logger.LogDebug("[MappingStore] 已写入 Redis: {Count} 条映射", mappings.Count);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[MappingStore] 写入 Redis 失败（不影响 JSON 写入）");
            }
        }

        // 2. 写 JSON 文件（本地回退）
        await _fileSemaphore.WaitAsync(ct);
        try
        {
            var container = new MappingFileContainer
            {
                SavedAt = DateTimeOffset.UtcNow,
                MappingCount = mappings.Count,
                Mappings = mappings.ToList()
            };

            var tempPath = _filePath + ".tmp";
            await using (var fs = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(fs, container, JsonOptions, ct);
            }

            // 原子替换（File.Move 在同卷下是原子操作）
            File.Move(tempPath, _filePath, overwrite: true);

            logger.LogDebug("[MappingStore] 已持久化 {Count} 条映射到 JSON 文件", mappings.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MappingStore] 持久化 JSON 文件失败（不影响 Redis）");
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    /// <summary>
    /// 从 Redis 加载映射；Redis 不可用时回退到 JSON 文件。
    /// 两者都不可用时返回 null。
    /// </summary>
    public async Task<IReadOnlyList<PermissionMappingDto>?> LoadAsync(CancellationToken ct = default)
    {
        // 1. 尝试 Redis（主存，跨实例共享）
        if (redis is not null)
        {
            try
            {
                var json = await redis.StringGetAsync(RedisKey, ct);
                if (!string.IsNullOrEmpty(json))
                {
                    var mappings = JsonSerializer.Deserialize<List<PermissionMappingDto>>(json, JsonOptions);
                    if (mappings is { Count: > 0 })
                    {
                        logger.LogInformation(
                            "[MappingStore] 从 Redis 加载 {Count} 条映射", mappings.Count);
                        return mappings.AsReadOnly();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[MappingStore] 从 Redis 加载失败，回退到 JSON 文件");
            }
        }

        // 2. 回退到 JSON 文件
        return await LoadFromFileAsync(ct);
    }

    /// <summary>JSON 文件是否存在（用于启动时判断是否有本地缓存）</summary>
    public bool FileExists => File.Exists(_filePath);

    /// <summary>从 JSON 文件加载映射（内部方法，无 Redis 依赖）</summary>
    private async Task<IReadOnlyList<PermissionMappingDto>?> LoadFromFileAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return null;

        await _fileSemaphore.WaitAsync(ct);
        try
        {
            using var fs = File.OpenRead(_filePath);
            var container = await JsonSerializer.DeserializeAsync<MappingFileContainer>(fs, JsonOptions, ct);

            if (container?.Mappings is null || container.Mappings.Count == 0)
            {
                logger.LogWarning("[MappingStore] JSON 文件存在但无有效映射: {Path}", _filePath);
                return null;
            }

            logger.LogInformation(
                "[MappingStore] 从本地 JSON 加载 {Count} 条映射（保存于 {SavedAt:u}）",
                container.Mappings.Count, container.SavedAt);

            return container.Mappings;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MappingStore] 加载本地 JSON 映射失败: {Path}", _filePath);
            return null;
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private sealed class MappingFileContainer
    {
        public DateTimeOffset SavedAt { get; set; }
        public int MappingCount { get; set; }
        public List<PermissionMappingDto> Mappings { get; set; } = [];
    }
}
