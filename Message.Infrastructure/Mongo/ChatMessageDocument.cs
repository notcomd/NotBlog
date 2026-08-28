using Message.Domain.Entities.Chat;

namespace Message.Infrastructure.Mongo;

/// <summary>
/// 聊天消息的 MongoDB 文档投影（D2-1：message 集合）。
/// <para>
/// 纯 POCO，含 Mongo 与外部的物理耦合；附体内嵌。仅作持久化中间态，
/// 读写时由 <see cref="MongoMessageRepository"/> 与领域聚合 <see cref="Message"/> 互转，
/// 避免领域程序集引入 MongoDB 依赖。
/// </para>
/// </summary>
public sealed class ChatMessageDocument
{
    /// <summary>消息ID（类映射声明为 _id）</summary>
    public Guid MessageId { get; set; }

    public Guid SessionId { get; set; }
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public MessageType MessageType { get; set; }
    public MessageStatus Status { get; set; }

    public string? Content { get; set; }
    public string? MediaUri { get; set; }
    public string? ThumbnailUri { get; set; }
    public long? FileSize { get; set; }
    public double? Duration { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public string? Caption { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkTitle { get; set; }
    public string? LinkDescription { get; set; }
    public string? ExpressionCode { get; set; }

    public DateTime SentTime { get; set; }
    public DateTime? DeliveredTime { get; set; }
    public DateTime? ReadTime { get; set; }
    public bool IsRecalled { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsForwarded { get; set; }
    public Guid? OriginalMessageId { get; set; }
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>内嵌附件</summary>
    public List<ChatFileAttachmentDocument> Attachments { get; set; } = new();
}

/// <summary>
/// 聊天附件（<see cref="FileAttachment"/>）的文档投影，内嵌于 <see cref="ChatMessageDocument"/>。
/// </summary>
public sealed class ChatFileAttachmentDocument
{
    public Guid AttachmentId { get; set; }
    public Guid MessageId { get; set; }
    public Guid FileId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public long FileSize { get; set; }
    public string FileUri { get; set; } = null!;
    public string? ThumbnailUri { get; set; }
    public string? MimeType { get; set; }
    public string? Description { get; set; }
    public DateTime UploadTime { get; set; }
    public DateTime? DownloadTime { get; set; }
    public int DownloadCount { get; set; }
    public bool IsDeleted { get; set; }
}