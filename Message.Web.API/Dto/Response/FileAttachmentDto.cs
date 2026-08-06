namespace Message.Web.API.Dto.Response;
public class FileAttachmentDto
{
    public Guid AttachmentId { get; init; }

    /// <summary>FileDev 文件 ID（附件引用的文件唯一标识）</summary>
    public Guid FileId { get; init; }

    /// <summary>所属消息 ID</summary>
    public Guid MessageId { get; init; }

    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string FileUrl { get; init; } = string.Empty;
    public string? MimeType { get; init; }
    public string? ThumbnailUrl { get; init; }
    public DateTime UploadTime { get; init; }
    public int DownloadCount { get; init; }
}

