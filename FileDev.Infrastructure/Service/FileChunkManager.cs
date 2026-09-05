using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using Microsoft.Extensions.Logging;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 分片上传综合管理器。
/// <para>
/// 方案 B 后分片跟踪全部落于 MongoDB：仓储以 <c>$addToSet</c> 原子去重维护已上传分片索引，
/// 跨实例天然安全，无需进程内信号量或 Redis 双写。本管理器只负责业务编排，
/// 状态转移（Uploading/Merged/Cancelled）由仓储原子更新驱动。
/// </para>
/// </summary>
public class FileChunkManager : IFileChunkManager
{
    private readonly IFileChunkRepository _repository;
    private readonly INotFileStorageService _storageService;
    private readonly ILogger<FileChunkManager> _logger;

    public FileChunkManager(
        IFileChunkRepository repository,
        INotFileStorageService storageService,
        ILogger<FileChunkManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

        // 幂等落库：Mongo 以 FileKey 唯一索引兜底，重试/并发初始化不会产生重复记录。
        await _repository.InsertAsync(record, ct).ConfigureAwait(false);
        _logger.LogInformation("[ChunkInit] 分片上传任务已初始化: FileKey={FileKey}", fileKey);

        return record;
    }

    // ── 分片标记 ──

    public async Task MarkChunkUploadedAsync(string fileKey, int chunkIndex, CancellationToken ct = default)
    {
        // Mongo $addToSet 原子去重并提升状态，跨实例安全，无需进程内信号量。
        await _repository.MarkChunkUploadedAsync(fileKey, chunkIndex, ct).ConfigureAwait(false);
    }



    public async Task<List<int>> GetUploadedChunksAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        return record?.UploadedChunks ?? [];
    }

    public async Task<FileChunkRecord?> GetUploadStatusAsync(string fileKey, CancellationToken ct = default)
    {
        return await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
    }



    public async Task<bool> AreAllChunksUploadedAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await _repository.GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record == null) return false;

        return record.UploadedChunks.Distinct().Count() >= record.TotalChunks;
    }

    public async Task MarkMergedAsync(string fileKey, CancellationToken ct = default)
    {
        await _repository.MarkMergedAsync(fileKey, ct).ConfigureAwait(false);

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
        await _repository.MarkCancelledAsync(fileKey, ct).ConfigureAwait(false);

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