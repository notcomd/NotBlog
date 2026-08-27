namespace FileDev.Domain.Entities;

/// <summary>
/// 分片上传任务记录文档，用于跟踪大文件分片上传的完整生命周期并支持断点续传。
/// <para>
/// 方案 B 后本类为 MongoDB 文档（非 EF 实体）：作为 <c>MongoFileChunkRepository</c> 的文档类型，
/// 采用 <c>$addToSet</c> 原子加分片索引（天然去重、跨实例安全），无领域事件、无工作单元语义。
/// 状态转移（Uploading/Merged/Cancelled）由仓储原子更新驱动，故不在此保留内存行为方法。
/// </para>
/// </summary>
public class FileChunkRecord
{
    /// <summary>仅供 MongoDB 驱动反序列化使用的参数化空构造。</summary>
    public FileChunkRecord()
    {
        RecordId = Guid.CreateVersion7();
        UploadedChunks = [];
        FileTags = [];
        CreatedAt = DateTimeOffset.UtcNow;
        Status = ChunkUploadStatus.Pending;
    }

    /// <summary>业务初始化构造（含参数校验），分片上传任务初始化时使用。</summary>
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

    /// <summary>文档主键（MongoDB _id，BsonId）</summary>
    public Guid RecordId { get; set; }

    /// <summary>分片上传任务的唯一标识（如: userId/timestamp_filename），业务唯一键</summary>
    public string FileKey { get; set; } = null!;

    public Guid UserId { get; set; }

    public string FileName { get; set; } = null!;

    public long TotalSize { get; set; }

    public int ChunkSize { get; set; }

    public int TotalChunks { get; set; }

    /// <summary>已完成上传的分片索引集合（MongoDB 数组，$addToSet 原子追加）</summary>
    public List<int> UploadedChunks { get; set; }

    public string FileMd5 { get; set; } = null!;

    public FileType FileType { get; set; }

    public FileIdentity FileIdentity { get; set; }

    public HashSet<string> FileTags { get; set; } = [];

    public string? FileDescription { get; set; }

    public ChunkUploadStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}