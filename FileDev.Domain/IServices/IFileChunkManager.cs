namespace FileDev.Domain.IServices;

/// <summary>
/// 分片上传综合管理器接口，协调 Redis 缓存和数据库持久化的双写策略，
/// 提供分片上传全生命周期的管理能力。
/// </summary>
public interface IFileChunkManager
{
    /// <summary>初始化分片上传任务（Redis + DB 双写）</summary>
    Task<FileChunkRecord> InitializeUploadAsync(
        string fileKey, Guid userId, string fileName, long totalSize,
        int chunkSize, int totalChunks, string fileMd5,
        FileType fileType, FileIdentity fileIdentity,
        HashSet<string>? fileTags = null, string? fileDescription = null,
        CancellationToken ct = default);

    /// <summary>记录分片上传完成（Redis Set + DB 更新）</summary>
    Task MarkChunkUploadedAsync(string fileKey, int chunkIndex, CancellationToken ct = default);

    /// <summary>从 Redis 优先获取已上传分片列表（Redis 不可用时降级到 DB）</summary>
    Task<List<int>> GetUploadedChunksAsync(string fileKey, CancellationToken ct = default);

    /// <summary>获取完整的断点续传状态（含 fileKey、总分片数、已上传分片）</summary>
    Task<FileChunkRecord?> GetUploadStatusAsync(string fileKey, CancellationToken ct = default);

    /// <summary>标记合并完成并清理 Redis 缓存</summary>
    Task MarkMergedAsync(string fileKey, CancellationToken ct = default);

    /// <summary>取消上传并清理所有关联数据（Redis + 临时文件）</summary>
    Task CancelUploadAsync(string fileKey, CancellationToken ct = default);

    /// <summary>检查是否所有分片已上传完毕</summary>
    Task<bool> AreAllChunksUploadedAsync(string fileKey, CancellationToken ct = default);
}
