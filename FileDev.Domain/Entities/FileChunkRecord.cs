namespace FileDev.Domain.Entities;

/// <summary>
/// 分片上传任务记录实体，用于跟踪大文件分片上传的完整生命周期并支持断点续传。
/// </summary>
public class FileChunkRecord : Entity, IAggregateRoot
{
    private FileChunkRecord()
    {
        RecordId = Guid.CreateVersion7();
        UploadedChunks = new List<int>();
        CreatedAt = DateTimeOffset.UtcNow;
        Status = ChunkUploadStatus.Pending;
    }

    public FileChunkRecord(
        string fileKey,
        Guid userId,
        string fileName,
        long totalSize,
        int chunkSize,
        int totalChunks,
        string fileMd5,
        FileType fileType,
        FileIdentity fileIdentity,
        HashSet<string>? fileTags = null,
        string? fileDescription = null) : this()
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            throw new ArgumentException("文件标识不能为空", nameof(fileKey));
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        if (totalSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalSize), "文件大小必须大于0");
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "分片大小必须大于0");
        if (totalChunks <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalChunks), "分片总数必须大于0");
        ArgumentNullException.ThrowIfNull(fileMd5);

        FileKey = fileKey;
        UserId = userId;
        FileName = fileName;
        TotalSize = totalSize;
        ChunkSize = chunkSize;
        TotalChunks = totalChunks;
        FileMd5 = fileMd5;
        FileType = fileType;
        FileIdentity = fileIdentity;
        FileTags = fileTags ?? [];
        FileDescription = fileDescription;
    }

    public Guid RecordId { get; init; }

    /// <summary>分片上传任务的唯一标识（如: userId/timestamp_filename）</summary>
    public string FileKey { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public string FileName { get; private set; } = null!;

    public long TotalSize { get; private set; }

    public int ChunkSize { get; private set; }

    public int TotalChunks { get; private set; }

    /// <summary>已完成上传的分片索引集合</summary>
    public List<int> UploadedChunks { get; private set; }

    public string FileMd5 { get; private set; } = null!;

    public FileType FileType { get; private set; }

    public FileIdentity FileIdentity { get; private set; }

    public HashSet<string> FileTags { get; private set; } = [];

    public string? FileDescription { get; private set; }

    public ChunkUploadStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>标记分片已上传（幂等：重复上传同一分片不会产生重复记录）</summary>
    public void MarkChunkUploaded(int chunkIndex)
    {
        if (chunkIndex < 0 || chunkIndex >= TotalChunks)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex),
                $"分片索引 {chunkIndex} 超出范围 [0, {TotalChunks - 1}]");

        // 幂等检查：已上传过的分片直接返回，避免断点续传时产生重复记录
        if (UploadedChunks.Contains(chunkIndex))
            return;

        UploadedChunks.Add(chunkIndex);

        if (Status == ChunkUploadStatus.Pending)
            Status = ChunkUploadStatus.Uploading;
    }

    /// <summary>检查所有分片是否已上传完毕</summary>
    public bool AreAllChunksUploaded() => UploadedChunks.Count >= TotalChunks;

    /// <summary>标记合并完成</summary>
    public void MarkMerged()
    {
        if (!AreAllChunksUploaded())
            throw new InvalidOperationException(
                $"还有 {TotalChunks - UploadedChunks.Count} 个分片未上传，无法合并");

        Status = ChunkUploadStatus.Merged;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>标记失败</summary>
    public void MarkFailed()
    {
        Status = ChunkUploadStatus.Failed;
    }

    /// <summary>标记取消</summary>
    public void MarkCancelled()
    {
        Status = ChunkUploadStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
