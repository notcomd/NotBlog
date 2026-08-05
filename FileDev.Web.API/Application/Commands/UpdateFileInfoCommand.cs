namespace FileDev.Web.API.Application.Commands;

/// <summary>更新文件元数据（S-08：仅文件所有者可更新）</summary>
public class UpdateFileInfoCommand : IRequest<NotFile>
{
    public Guid UserId { get; set; }
    public Guid FileId { get; set; }
    public string FileName { get; set; } = null!;
    public HashSet<string>? FileTags { get; set; }
    public string FileDescription { get; set; } = string.Empty;
    public FileIdentity FileIdentity { get; set; }
    public string FileMd5 { get; set; } = string.Empty;
}
