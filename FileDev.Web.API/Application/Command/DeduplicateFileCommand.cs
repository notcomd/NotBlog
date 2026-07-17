namespace FileDev.Web.API.Application.Command;

public class DeduplicateFileCommand : IRequest<DeduplicateFileResponse>
{
    public string FileMd5 { get; set; } = null!;
    public long FileSize { get; set; }
}

public class DeduplicateFileResponse
{
    public bool Exists { get; set; }
    public Guid? FileId { get; set; }
    public string? FileUri { get; set; }
}
