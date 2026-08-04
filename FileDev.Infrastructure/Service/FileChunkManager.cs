using System.Collections.Concurrent;
using CacheMemory.Core;
using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 分片上传综合管理器，实现 Redis 缓存 + 数据库持久化双写策略。
/// Redis 作为热数据层提供低延迟查询，数据库作为持久层保证数据可靠性。
/// Redis 不可用时自动降级到数据库查询。
/// </summary>
public class FileChunkManager : IFileChunkManager
{
    private const string ChunkSetKeyPrefix = "file:chunks:";

    private readonly IFileChunkRepository _repository;
    private readonly IRedisCacheService _redis;
    private readonly INotFileStorageService _storageService;
    private readonly NotFileStorageOptions _config;
    private readonly ILogger<FileChunkManager> _logger;

    /// <summary>
    /// 按 fileKey 分组的进程内信号量，用于序列化同一文件分片上传的 DB 读改写操作，
    /// 防止并发分片上传导致 DB 中 UploadedChunks 列表的丢失更新（lost update）。
    /// 跨进程场景由 Redis Set 保证正确性，DB 为降级备份。
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    public FileChunkManager(
        IFileChunkRepository repository,
        IRedisCacheService redis,
        INotFileStorageService storageService,
        IOptionsSnapshot<NotFileStorageOptions> configOptions,
        ILogger<FileChunkManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _config = configOptions.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string ChunkSetKey(string fileKey) => $"{ChunkSetKeyPrefix}{fileKey}";

    /// <summary>分片进度键的过期时间（与分片上传生命周期匹配，默认24小时）</summary>
    private TimeSpan ChunkKeyExpiry => TimeSpan.FromHours(_config.ChunkExpirationHours);

    /// <summary>获取（或创建）指定 fileKey 的信号量</summary>
    private static SemaphoreSlim GetLock(string fileKey) =>
        _fileLocks.GetOrAdd(fileKey, _ => new SemaphoreSlim(1, 1));

    // ── 初始化 ──

    public async Task<FileChunkRecord> InitializeUploadAsync(
        string fileKey, Guid userId, string fileName, long totalSize,
        int chunkSize, int totalChunks, string fileMd5,
        FileType fileType, FileIdentity fileIdentity,
        HashSet<string>? fileTags = null, string? fileDescription = null,
        CancellationToken ct = default)
    {
        var record = new FileChunkRecord(
            fileKey, userId, fileName, totalSize, chunkSize, totalChunks,
            fileMd5, fileType, fileIdentity, fileTags, fileDescription);

        // DB 双写
        await _repository.InsertAsync(record, ct).ConfigureAwait(false);
        await _repository.UnitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        // Redis 双写：初始化空的已上传分片集合，设置过期时间
        try
        {
            await _redis.KeyDeleteAsync(ChunkSetKey(fileKey), ct).ConfigureAwait(false);
            // 为分片进度键设置过期时间，避免占位键永久保留导致 Redis 内存增长
            await _redis.KeyExpireAsync(ChunkSetKey(fileKey), ChunkKeyExpiry, ct).ConfigureAwait(false);
            _logger.LogInformation("[ChunkInit] DB+Redis 双写成功: FileKey={FileKey}", fileKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkInit] Redis 写入失败（已降级到 DB）: FileKey={FileKey}", fileKey);
        }

        return record;
    }

    // ── 分片标记 ──

    public async Task MarkChunkUploadedAsync(string fileKey, int chunkIndex, CancellationToken ct = default)
    {
        // Redis 优先写入（Set 天然去重，无需额外检查）
        try
        {
            await _redis.SetAddAsync(ChunkSetKey(fileKey), chunkIndex.ToString(), ct).ConfigureAwait(false);
            // 分片写入时刷新过期时间（续期），与上传生命周期匹配
            await _redis.KeyExpireAsync(ChunkSetKey(fileKey), ChunkKeyExpiry, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkUpload] Redis 写入失败: FileKey={FileKey}, Chunk={Chunk}",
                fileKey, chunkIndex);
        }

        // DB 双写：使用信号量序列化同一 fileKey 的读改写，防止丢失更新
        var semaphore = GetLock(fileKey);
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
            if (record != null)
            {
                // 去重：避免同一分片重复上传时在 DB 列表中产生重复条目
                if (!record.UploadedChunks.Contains(chunkIndex))
                {
                    record.MarkChunkUploaded(chunkIndex);
                    await _repository.UpdateAsync(record, ct).ConfigureAwait(false);
                    await _repository.UnitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    // ── 状态查询（Redis优先，DB降级） ──

    public async Task<List<int>> GetUploadedChunksAsync(string fileKey, CancellationToken ct = default)
    {
        // 优先 Redis
        try
        {
            var members = await _redis.SetMembersAsync(ChunkSetKey(fileKey), ct).ConfigureAwait(false);
            if (members.Any())
            {
                // 使用 TryParse 防止 Redis 中存在非数字成员时抛出异常
                return members
                    .Where(m => int.TryParse(m, out _))
                    .Select(int.Parse)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkStatus] Redis 查询失败，降级到 DB: FileKey={FileKey}", fileKey);
        }

        // 降级 DB
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        return record?.UploadedChunks ?? [];
    }

    public async Task<FileChunkRecord?> GetUploadStatusAsync(string fileKey, CancellationToken ct = default)
    {
        // 使用 AsNoTracking 读取（仓储层已配置），避免修改被跟踪实体导致意外保存
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record == null) return null;

        // 尝试从 Redis 同步最新状态到内存对象（按 chunkIndex 去重合并，避免重复统计）
        try
        {
            var redisChunks = await _redis.SetMembersAsync(ChunkSetKey(fileKey), ct).ConfigureAwait(false);
            if (redisChunks.Any())
            {
                var merged = new HashSet<int>(record.UploadedChunks);
                foreach (var c in redisChunks)
                {
                    if (int.TryParse(c, out var idx))
                        merged.Add(idx);
                }
                record.UploadedChunks.Clear();
                record.UploadedChunks.AddRange(merged);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkStatus] Redis 同步失败，使用 DB 值: FileKey={FileKey}", fileKey);
        }

        return record;
    }

    // ── 完成与清理 ──

    public async Task<bool> AreAllChunksUploadedAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record == null) return false;

        var uploaded = await GetUploadedChunksAsync(fileKey, ct).ConfigureAwait(false);
        // 使用 Distinct().Count() 防止 DB 中可能存在的重复分片索引导致误判
        return uploaded.Distinct().Count() >= record.TotalChunks;
    }

    public async Task MarkMergedAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record != null)
        {
            // 在调用 MarkMerged() 前从 Redis 同步最新分片状态到 DB 记录，
            // 防止 DB 中 UploadedChunks 因并发丢失更新导致 AreAllChunksUploaded() 误判失败
            try
            {
                var redisChunks = await _redis.SetMembersAsync(ChunkSetKey(fileKey), ct).ConfigureAwait(false);
                if (redisChunks.Any())
                {
                    var merged = new HashSet<int>(record.UploadedChunks);
                    foreach (var c in redisChunks)
                    {
                        if (int.TryParse(c, out var idx))
                            merged.Add(idx);
                    }
                    record.UploadedChunks.Clear();
                    record.UploadedChunks.AddRange(merged);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ChunkMerge] Redis 同步失败: FileKey={FileKey}", fileKey);
            }

            record.MarkMerged();
            await _repository.UpdateAsync(record, ct).ConfigureAwait(false);
            await _repository.UnitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // 清理 Redis
        try
        {
            await _redis.KeyDeleteAsync(ChunkSetKey(fileKey), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkMerge] Redis 清理失败: FileKey={FileKey}", fileKey);
        }

        // 清理临时分片文件（合并完成后 MergeChunksAsync 已删除分片，此处为兜底）
        try
        {
            await _storageService.CleanupChunksAsync(fileKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkMerge] 临时分片文件清理失败: FileKey={FileKey}", fileKey);
        }

        _logger.LogInformation("[ChunkMerge] 分片合并完成: FileKey={FileKey}", fileKey);
    }

    public async Task CancelUploadAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record != null)
        {
            record.MarkCancelled();
            await _repository.UpdateAsync(record, ct).ConfigureAwait(false);
            await _repository.UnitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // 清理 Redis
        try
        {
            await _redis.KeyDeleteAsync(ChunkSetKey(fileKey), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkCancel] Redis 清理失败: FileKey={FileKey}", fileKey);
        }

        // 清理临时分片文件（接口契约要求"清理所有关联数据：Redis + 临时文件"）
        try
        {
            await _storageService.CleanupChunksAsync(fileKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkCancel] 临时分片文件清理失败: FileKey={FileKey}", fileKey);
        }

        _logger.LogInformation("[ChunkCancel] 分片上传已取消: FileKey={FileKey}", fileKey);
    }
}
