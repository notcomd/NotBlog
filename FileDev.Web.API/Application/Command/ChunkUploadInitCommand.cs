namespace FileDev.Web.API.Application.Command;

public class ChunkUploadInitCommand : IRequest<FileChunkRecord>
{
    public Guid UserId { get; set; }
    public string FileName { get; set; } = null!;
    public long TotalSize { get; set; }
    public int ChunkSize { get; set; }
    public string FileMd5 { get; set; } = null!;
    public FileType FileType { get; set; }
    public FileIdentity FileIdentity { get; set; } = FileIdentity.FilePrivate;
    public HashSet<string>? FileTags { get; set; }
    public string? FileDescription { get; set; }
}
