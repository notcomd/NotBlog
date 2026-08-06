namespace Message.Web.API.Dto.Response;
public class MessageDto
{
    public Guid MessageId { get; init; }
    public Guid SessionId { get; init; }
    public Guid SenderId { get; init; }
    public Guid? ReceiverId { get; init; }
    public MessageType MessageType { get; init; }
    public MessageStatus Status { get; init; }
    public string? Content { get; init; }
    public string? MediaUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? FileName { get; init; }
    public long? FileSize { get; init; }
    public string? MimeType { get; init; }
    public double? Duration { get; init; }
    public string? Caption { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationName { get; init; }
    public string? LinkUrl { get; init; }
    public string? LinkTitle { get; init; }
    public string? LinkDescription { get; init; }
    public string? ExpressionCode { get; init; }
    public DateTime SentTime { get; init; }
    public DateTime? DeliveredTime { get; init; }
    public DateTime? ReadTime { get; init; }
    public bool IsRecalled { get; init; }
    public bool IsForwarded { get; init; }
    public Guid? OriginalMessageId { get; init; }
    public Guid? ReplyToMessageId { get; init; }
    public List<FileAttachmentDto>? Attachments { get; init; }
}

