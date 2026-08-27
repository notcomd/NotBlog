using Microsoft.Extensions.Logging;
using MohuTianchi.Lite;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;
using StorageTier = FileDev.Domain.Enum.StorageTier;
using LiteStorageTier = MohuTianchi.Lite.StorageTier;
using LiteDirectoryInfo = MohuTianchi.Lite.DirectoryInfo;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 基于 Mohu-TianChi 精简对象存储（<see cref="IObjectStorage"/>）的 <see cref="INotFileStorageService"/> 适配器，
/// 作为 FileDev 的底层存储实现：内容寻址分片 + 块级去重 + 目录/索引 + 日志 + 安全，全部由 Lite 内核承载。
/// <para>
/// 对上保持 <see cref="INotFileStorageService"/> 契约不变（命令处理器 / HTTP API / gRPC / 下载链路零改动）；
/// 对下将文件级原语（保存/读取/删除/Exists）映射为对象 key 的 Write/Read/Delete/Exists。
/// 分片上传在本适配器内部被实现为「临时对象写入 → 合并为单个最终对象（Lite 内核再次分片去重）→ 清理临时对象」。
/// </para>
/// 注意：Lite 门面按整对象读写（ReadAsync 返回 byte[]），因此流式语义由内存流承担（下载仍支持浏览器 Range，但基于内存）。
/// </summary>
public sealed class MohuObjectStorageService : INotFileStorageService
{
    /// <summary>分片临时对象的 key 前缀，避免与最终对象（{userId}/{guid}{ext}）冲突。</summary>
    private const string ChunkPrefix = "__chunk__/";

    /// <summary>文件内容缓存在 Redis 的 key 前缀（Base64 存储，见 <see cref="IRedisCacheService"/>）。</summary>
    private const string ContentCachePrefix = "file:content:";

    private readonly IObjectStorage _storage;
    private readonly IRedisCacheService _redis;
    private readonly NotFileStorageOptions _config;
    private readonly ILogger<MohuObjectStorageService> _logger;

    /// <summary>构造适配器。</summary>
    /// <param name="storage">Lite 对象存储门面（单例，由应用装配创建）。</param>
    /// <param name="redis">统一 Redis 缓存服务（项目内唯一 Redis 访问入口）。</param>
    /// <param name="configOptions">文件存储配置（含下载缓存大小阈值与 TTL）。</param>
    /// <param name="logger">日志。</param>
    public MohuObjectStorageService(
        IObjectStorage storage,
        IRedisCacheService redis,
        IOptionsSnapshot<NotFileStorageOptions> configOptions,
        ILogger<MohuObjectStorageService> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>分片临时对象 key：<c>__chunk__/{fileKey}#{chunkIndex}</c>。</summary>
    private static string ChunkKey(string fileKey, int chunkIndex) => $"{ChunkPrefix}{fileKey}#{chunkIndex}";

    /// <summary>构造失败响应。</summary>
    private static NotFileStorageResponse Failure(string message) => new() { Success = false, ErrorMessage = message };

    /// <summary>构造成功响应。</summary>
    private static NotFileStorageResponse Success(string path, byte[] content) => new()
    {
        Success = true,
        FullPath = path,
        FileSize = content.Length,
        ActualHash = HashHelper.ComputeHash(content, AlgorithmType.SHA256)
    };

    /// <summary>文件内容在 Redis 的缓存 key。</summary>
    private static string CacheKey(string relativePath) => $"{ContentCachePrefix}{relativePath}";

    /// <summary>惰性回填内容缓存：仅在文件不超过大小阈值时写入 Redis，并带 TTL。缓存失败不影响主流程。</summary>
    private async Task BackfillCacheAsync(string cacheKey, byte[] content)
    {
        if (content.Length <= 0 || content.Length > _config.DownloadCacheMaxBytes)
            return;
        try
        {
            await _redis.StringSetAsync(cacheKey, Convert.ToBase64String(content),
                TimeSpan.FromSeconds(_config.DownloadCacheTtlSeconds)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "回填文件内容缓存失败 Key={Key}", cacheKey);
        }
    }

    /// <summary>失效某文件的内容缓存（删除/覆盖写之后调用，防止误读脏数据）。</summary>
    private async Task InvalidateCacheAsync(string relativePath)
    {
        try
        {
            await _redis.KeyDeleteAsync(CacheKey(relativePath)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "失效文件内容缓存失败 Path={Path}", relativePath);
        }
    }

    /// <summary>
    /// 统一读取入口（带 Redis 缓存）：先查缓存（Base64），未命中再从 Lite 读整对象并惰性回填。
    /// </summary>
    private async Task<(byte[]? Content, NotFileStorageResponse Response)> ReadContentCachedAsync(string relativePath)
    {
        var cacheKey = CacheKey(relativePath);
        try
        {
            var cached = await _redis.StringGetAsync(cacheKey).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(cached))
            {
                var bytes = Convert.FromBase64String(cached);
                return (bytes, Success(relativePath, bytes));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "读取文件内容缓存失败 Key={Key}", cacheKey);
        }

        try
        {
            var content = await _storage.ReadAsync(relativePath).ConfigureAwait(false);
            if (content is null)
            {
                _logger.LogWarning("读取文件失败（对象不存在） Path={Path}", relativePath);
                return (null, Failure($"文件不存在：{relativePath}"));
            }

            await BackfillCacheAsync(cacheKey, content).ConfigureAwait(false);
            return (content, Success(relativePath, content));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取文件失败 FileRelativePath={Path}", relativePath);
            return (null, Failure("读取文件失败"));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <returns></returns>
    public async Task<(byte[] Content, NotFileStorageResponse Response)> GetContentAsync(string fileRelativePath)
    {
        var (content, response) = await ReadContentCachedAsync(fileRelativePath).ConfigureAwait(false);
        return (content!, response);
    }

    /// <summary>
    /// 流式获取文件内容：命中 Redis 缓存时直取字节；未命中则从 Lite 读整对象后回填。
    /// 内存流承接以维持上层流式下载契约（<c>/files</c> 的浏览器 Range 语义仍可用，但基于内存）。
    /// </summary>
    public async Task<(Stream? Content, NotFileStorageResponse Response)> GetContentStreamAsync(string fileRelativePath)
    {
        var (content, response) = await ReadContentCachedAsync(fileRelativePath).ConfigureAwait(false);
        if (content is null)
            return (null, response);
        return (new MemoryStream(content, writable: false), response);
    }

    /// <summary>列出并删除某 fileKey 的全部分片临时对象（幂等，缺失即忽略）。</summary>
    /// <param name="fileKey">分片上传任务标识（最终对象 key）。</param>
    private async Task CleanupChunkObjectsAsync(string fileKey)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return;

        var keys = await _storage.ListAsync($"{ChunkPrefix}{fileKey}", ct: CancellationToken.None).ConfigureAwait(false);
        foreach (var key in keys)
        {
            try
            {
                await _storage.DeleteAsync(key, ct: CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理分片临时对象失败 Key={Key}", key);
            }
        }
    }

    public async Task<NotFileStorageResponse> SaveAsync(NotFileStorageRequest request)
    {
        try
        {
            // ExpectedHash 前置校验（与旧实现一致，SHA256 统一）
            if (!string.IsNullOrWhiteSpace(request.ExpectedHash))
            {
                var actual = HashHelper.ComputeHash(request.FileContent, AlgorithmType.SHA256);
                if (!string.Equals(actual, request.ExpectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("保存文件哈希校验失败 FileRelativePath={Path} Expected={Expected} Actual={Actual}",
                        request.FileRelativePath, request.ExpectedHash, actual);
                    return Failure($"文件哈希校验失败，预期：{request.ExpectedHash}，实际：{actual}");
                }
            }

            await _storage.WriteAsync(request.FileRelativePath, request.FileContent,
                new WriteOptions { Overwrite = request.Overwrite }).ConfigureAwait(false);

            // 覆盖写后失效旧内容缓存，防止误读脏数据
            await InvalidateCacheAsync(request.FileRelativePath).ConfigureAwait(false);

            var response = new NotFileStorageResponse
            {
                Success = true,
                FullPath = request.FileRelativePath,
                FileSize = request.FileContent.Length,
                ActualHash = HashHelper.ComputeHash(request.FileContent, AlgorithmType.SHA256)
            };
            // 对齐 Lite 清单元数据（卷 ID / 分片数 / 更新时间）
            response.ContentHash = response.ActualHash;
            await ApplyManifestMetaAsync(response, request.FileRelativePath).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存文件失败 FileRelativePath={Path}", request?.FileRelativePath);
            return Failure("保存文件失败");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <returns></returns>
    public async Task<NotFileStorageResponse> DeleteAsync(string fileRelativePath)
    {
        try
        {
            var deleted = await _storage.DeleteAsync(fileRelativePath).ConfigureAwait(false);
            if (!deleted)
            {
                _logger.LogWarning("删除文件失败（对象不存在） Path={Path}", fileRelativePath);
                return Failure($"文件不存在：{fileRelativePath}");
            }

            // 对象删除后剔除内容缓存
            await InvalidateCacheAsync(fileRelativePath).ConfigureAwait(false);

            return new NotFileStorageResponse { Success = true, FullPath = fileRelativePath };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文件失败 FileRelativePath={Path}", fileRelativePath);
            return Failure("删除文件失败");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <returns></returns>
    public async Task<bool> ExistsAsync(string fileRelativePath)
    {
        try
        {
            return await _storage.ExistsAsync(fileRelativePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查文件存在性失败 FileRelativePath={Path}", fileRelativePath);
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileKey"></param>
    /// <returns></returns>
    public async Task CleanupChunksAsync(string fileKey)
    {
        try
        {
            await CleanupChunkObjectsAsync(fileKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理分片临时对象失败 FileKey={FileKey}", fileKey);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="chunkIndex"></param>
    /// <param name="chunkContent"></param>
    /// <param name="chunkHash"></param>
    /// <returns></returns>
    public async Task<NotFileStorageResponse> UploadChunkAsync(string fileKey, int chunkIndex, byte[] chunkContent,
        string? chunkHash = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(chunkHash))
            {
                var actual = HashHelper.ComputeHash(chunkContent, AlgorithmType.SHA256);
                if (!string.Equals(actual, chunkHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("分片哈希校验失败 FileKey={FileKey} ChunkIndex={ChunkIndex}", fileKey, chunkIndex);
                    return Failure($"分片{chunkIndex}哈希校验失败");
                }
            }

            var key = ChunkKey(fileKey, chunkIndex);
            await _storage.WriteAsync(key, chunkContent, new WriteOptions { Overwrite = true }).ConfigureAwait(false);

            return new NotFileStorageResponse
            {
                Success = true,
                FullPath = key,
                FileSize = chunkContent.Length,
                ActualHash = HashHelper.ComputeHash(chunkContent, AlgorithmType.SHA256)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分片失败 FileKey={FileKey} ChunkIndex={ChunkIndex}", fileKey, chunkIndex);
            return Failure($"上传分片{chunkIndex}失败");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="totalChunks"></param>
    /// <param name="expectedFileHash"></param>
    /// <param name="overwrite"></param>
    /// <returns></returns>
    public async Task<NotFileStorageResponse> MergeChunksAsync(string fileKey, int totalChunks,
        string? expectedFileHash = null, bool overwrite = true)
    {
        try
        {
            using var buffer = new MemoryStream();
            for (var i = 0; i < totalChunks; i++)
            {
                var chunkKey = ChunkKey(fileKey, i);
                var chunkContent = await _storage.ReadAsync(chunkKey).ConfigureAwait(false);
                if (chunkContent is null)
                    return Failure($"分片{i}缺失");
                await buffer.WriteAsync(chunkContent).ConfigureAwait(false);
            }

            var merged = buffer.ToArray();

            if (!string.IsNullOrWhiteSpace(expectedFileHash))
            {
                var actualHash = HashHelper.ComputeHash(merged, AlgorithmType.SHA256);
                if (!string.Equals(actualHash, expectedFileHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("文件合并后哈希校验失败 FileKey={FileKey}", fileKey);
                    return Failure($"文件合并后哈希校验失败，预期：{expectedFileHash}，实际：{actualHash}");
                }
            }

            await _storage.WriteAsync(fileKey, merged, new WriteOptions { Overwrite = overwrite }).ConfigureAwait(false);
   
            await CleanupChunkObjectsAsync(fileKey).ConfigureAwait(false);

            await InvalidateCacheAsync(fileKey).ConfigureAwait(false);

            var response = new NotFileStorageResponse
            {
                Success = true,
                FullPath = fileKey,
                FileSize = merged.Length,
                ActualHash = HashHelper.ComputeHash(merged, AlgorithmType.SHA256)
            };
            // 对齐 Lite 清单元数据（卷 ID / 分片数 / 更新时间）
            response.ContentHash = response.ActualHash;
            await ApplyManifestMetaAsync(response, fileKey).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合并分片失败 FileKey={FileKey}", fileKey);
            return Failure("合并分片失败");
        }
    }

    /// <summary>
    /// 获取分片的数量
    /// </summary>
    /// <param name="fileSize"></param>
    /// <returns></returns>
    public Task<int> GetTotalChunkCountAsync(long fileSize)
    {
        if (fileSize <= 0) return Task.FromResult(1);
        return Task.FromResult((int)Math.Ceiling((double)fileSize / _config.ChunkFileSize));
    }



    /// <summary>将 Lite 清单回填到响应中的存储元数据（卷 ID / 分片数 / 更新时间 / 过期时间）。</summary>
    /// <param name="response">待回填的响应。</param>
    /// <param name="key">对象 key。</param>
    private async Task ApplyManifestMetaAsync(NotFileStorageResponse response, string key)
    {
        try
        {
            var manifest = await _storage.GetManifestAsync(key, ct: CancellationToken.None).ConfigureAwait(false);
            if (manifest is null) return;
            response.VolumeId = manifest.VolumeId ?? string.Empty;
            response.ShardCount = manifest.Shards?.Count ?? 1;
            response.ExpiresAt = manifest.ExpiresAt;
            response.UpdatedUtc = manifest.UpdatedUtc;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "回填清单元数据失败 Key={Key}", key);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<StorageManifestDto?> GetManifestAsync(string fileRelativePath, CancellationToken ct = default)
    {
        try
        {
            var manifest = await _storage.GetManifestAsync(fileRelativePath, ct: ct).ConfigureAwait(false);
            if (manifest is null) return null;
            return new StorageManifestDto
            {
                VolumeId = manifest.VolumeId ?? string.Empty,
                ShardCount = manifest.Shards?.Count ?? 1,
                ContentHash = HashHelper.ComputeHash(
                    (await _storage.ReadAsync(fileRelativePath, ct: ct).ConfigureAwait(false)) ?? [], AlgorithmType.SHA256),
                Tier = StorageTier.Hot,
                ExpiresAt = manifest.ExpiresAt,
                UpdatedUtc = manifest.UpdatedUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取对象清单失败 Path={Path}", fileRelativePath);
            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <param name="tier"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<StorageManifestDto?> ChangeStorageTierAsync(string fileRelativePath, StorageTier tier,
        CancellationToken ct = default)
    {
        try
        {
            if (!await _storage.ExistsAsync(fileRelativePath, ct: ct).ConfigureAwait(false))
            {
                _logger.LogWarning("调整存储层失败（对象不存在） Path={Path}", fileRelativePath);
                return null;
            }

            await _storage.UpdateTierAsync(fileRelativePath, (LiteStorageTier)tier, ct: ct)
                .ConfigureAwait(false);
            var manifest = await GetManifestAsync(fileRelativePath, ct).ConfigureAwait(false);
            if (manifest is not null)
                manifest.Tier = tier;
            return manifest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调整存储层失败 Path={Path}", fileRelativePath);
            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private static NotFileVolumeInfoDto MapVolume(VolumeRecord record) => new()
    {
        VolumeId = record.VolumeId ?? string.Empty,
        TenantId = record.TenantId,
        RootPath = record.RootPath ?? string.Empty,
        Kind = string.IsNullOrEmpty(record.VolumeId)
            ? VolumeKind.Default
            : string.IsNullOrEmpty(record.TenantId) ? VolumeKind.Shared : VolumeKind.Tenant,
        TotalBytes = record.TotalBytes,
        PartCount = record.PartCount,
        ObjectCount = record.ObjectCount
    };

    /// <summary>
    /// 添加卷
    /// </summary>
    /// <param name="rootPath"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<NotFileVolumeInfoDto> AddVolumeAsync(string rootPath, CancellationToken ct = default)
    {
        var record = _storage.AddVolume(rootPath);
        var dto = MapVolume(record);
        dto.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(dto);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="rootPath"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<NotFileVolumeInfoDto> AddTenantVolumeAsync(string tenantId, string rootPath,
        CancellationToken ct = default)
    {
        var record = _storage.AddTenantVolume(tenantId, rootPath);
        var dto = MapVolume(record);
        dto.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(dto);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<IEnumerable<NotFileVolumeInfoDto>> GetVolumeStatsAsync(CancellationToken ct = default)
    {
        var records = _storage.GetVolumeStats();
        return Task.FromResult<IEnumerable<NotFileVolumeInfoDto>>(records?.Select(MapVolume).ToList() ?? []);
    }

    /// <summary>
    /// 将lite目录信息进行转换
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    private static NotFileDirectoryInfoDto MapDirectory(LiteDirectoryInfo info) => new()
    {
        Name = info.Name ?? string.Empty,
        TotalBytes = info.TotalBytes,
        PartCount = info.PartCount,
        ObjectCount = info.ObjectCount
    };

    /// <summary>
    /// 获取目录统计信息
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<IEnumerable<NotFileDirectoryInfoDto>> GetDirectoryStatsAsync(CancellationToken ct = default)
    {
        var infos = _storage.GetDirectoryStats();
        return Task.FromResult<IEnumerable<NotFileDirectoryInfoDto>>(infos?.Select(MapDirectory).ToList() ?? []);
    }
}