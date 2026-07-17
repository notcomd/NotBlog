namespace FileDev.Web.API.Application.Command;

using FileDev.Domain.Entities;

public class StreamUploadCommand : IRequest<NotFile>
{
    public Guid UserId { get; set; }
    public string FileName { get; set; } = null!;
    public Stream FileStream { get; set; } = null!;
    public long FileSize { get; set; }
    public FileType FileType { get; set; }
    public FileIdentity FileIdentity { get; set; } = FileIdentity.FilePrivate;
    public HashSet<string>? FileTags { get; set; }
    public string? FileDescription { get; set; }
}
