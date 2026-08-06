namespace Message.Web.API.Dto.Request;
public class SendMessageRequest
{
    public Guid SessionId { get; init; }
    public MessageType MessageType { get; init; }
    public string? Content { get; init; }

    /// <summary>FileDev 文件 ID（图片/视频/音频/文件消息必填，来自上传接口返回的 FileRef.FileId）</summary>
    public Guid? FileId { get; init; }

    /// <summary>缩略图 FileDev 文件 ID（图片/视频消息可选）</summary>
    public Guid? ThumbnailFileId { get; init; }

    public double? Duration { get; init; }
    public string? Caption { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationName { get; init; }
    public string? LinkUrl { get; init; }
    public string? LinkTitle { get; init; }
    public string? LinkDescription { get; init; }
    public string? ExpressionCode { get; init; }
    public Guid? ReplyToMessageId { get; init; }
}

