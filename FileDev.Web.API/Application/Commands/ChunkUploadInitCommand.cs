namespace FileDev.Web.API.Application.Commands;

public class ChunkUploadInitCommand : IRequest<FileChunkRecord>
{
    public Guid UserId { get; set; }
    public string FileName { get; set; } = null!;
    public long TotalSize { get; set; }
    public int ChunkSize { get; set; }

    /// <summary>客户端指定的总分片数（&gt;0 时优先采用，否则按 TotalSize/ChunkSize 向上取整）</summary>
    public int TotalChunks { get; set; }

    public string FileMd5 { get; set; } = null!;
    public FileType FileType { get; set; }
    public FileIdentity FileIdentity { get; set; } = FileIdentity.FilePrivate;
    public HashSet<string>? FileTags { get; set; }
    public string? FileDescription { get; set; }
}
