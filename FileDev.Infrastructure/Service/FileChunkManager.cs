using CacheMemory.Core;
using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<FileChunkManager> _logger;

    public FileChunkManager(
        IFileChunkRepository repository,
        IRedisCacheService redis,
        ILogger<FileChunkManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string ChunkSetKey(string fileKey) => $"{ChunkSetKeyPrefix}{fileKey}";

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
        // Redis 优先写入
        try
        {
            await _redis.SetAddAsync(ChunkSetKey(fileKey), chunkIndex.ToString(), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkUpload] Redis 写入失败: FileKey={FileKey}, Chunk={Chunk}",
                fileKey, chunkIndex);
        }

        // DB 双写
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record != null)
        {
            record.MarkChunkUploaded(chunkIndex);
            await _repository.UpdateAsync(record, ct).ConfigureAwait(false);
            await _repository.UnitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
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
                return members.Select(int.Parse).ToList<int>();
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
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record == null) return null;

        // 尝试从 Redis 同步最新状态到内存对象
        try
        {
            var redisChunks = await _redis.SetMembersAsync(ChunkSetKey(fileKey), ct).ConfigureAwait(false);
            if (redisChunks.Any())
            {
                foreach (var c in redisChunks.Select(int.Parse))
                    record.UploadedChunks.Add(c);
            }
        }
        catch
        {
            // Redis 不可用，使用 DB 中的值即可
        }

        return record;
    }

    // ── 完成与清理 ──

    public async Task<bool> AreAllChunksUploadedAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record == null) return false;

        var uploaded = await GetUploadedChunksAsync(fileKey, ct).ConfigureAwait(false);
        return uploaded.Count >= record.TotalChunks;
    }

    public async Task MarkMergedAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record != null)
        {
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

        _logger.LogInformation("[ChunkCancel] 分片上传已取消: FileKey={FileKey}", fileKey);
    }
}
