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

    /// <summary>会话ID。</summary>
    public Guid SessionId { get; set; }
    /// <summary>发送者ID。</summary>
    public Guid SenderId { get; set; }
    /// <summary>接收者ID（群或频道消息可为空）。</summary>
    public Guid? ReceiverId { get; set; }
    /// <summary>消息类型。</summary>
    public MessageType MessageType { get; set; }
    /// <summary>消息状态。</summary>
    public MessageStatus Status { get; set; }

    /// <summary>文本内容（文本消息）。</summary>
    public string? Content { get; set; }
    /// <summary>媒体资源URI（图片/视频/音频/文件）。</summary>
    public string? MediaUri { get; set; }
    /// <summary>缩略图URI。</summary>
    public string? ThumbnailUri { get; set; }
    /// <summary>文件大小（字节）。</summary>
    public long? FileSize { get; set; }
    /// <summary>媒体时长（秒）。</summary>
    public double? Duration { get; set; }
    /// <summary>文件名。</summary>
    public string? FileName { get; set; }
    /// <summary>MIME类型。</summary>
    public string? MimeType { get; set; }
    /// <summary>媒体说明文字。</summary>
    public string? Caption { get; set; }
    /// <summary>纬度（位置消息）。</summary>
    public double? Latitude { get; set; }
    /// <summary>经度（位置消息）。</summary>
    public double? Longitude { get; set; }
    /// <summary>位置名称。</summary>
    public string? LocationName { get; set; }
    /// <summary>链接地址（链接消息）。</summary>
    public string? LinkUrl { get; set; }
    /// <summary>链接标题。</summary>
    public string? LinkTitle { get; set; }
    /// <summary>链接描述。</summary>
    public string? LinkDescription { get; set; }
    /// <summary>表情标识（表情消息）。</summary>
    public string? ExpressionCode { get; set; }

    /// <summary>发送时间。</summary>
    public DateTime SentTime { get; set; }
    /// <summary>送达时间。</summary>
    public DateTime? DeliveredTime { get; set; }
    /// <summary>读取时间。</summary>
    public DateTime? ReadTime { get; set; }
    /// <summary>是否已撤回。</summary>
    public bool IsRecalled { get; set; }
    /// <summary>是否加密。</summary>
    public bool IsEncrypted { get; set; }
    /// <summary>是否转发。</summary>
    public bool IsForwarded { get; set; }
    /// <summary>原始消息ID（转发来源）。</summary>
    public Guid? OriginalMessageId { get; set; }
    /// <summary>回复的目标消息ID。</summary>
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>内嵌附件</summary>
    public List<ChatFileAttachmentDocument> Attachments { get; set; } = new();
}

/// <summary>
/// 聊天附件（<see cref="FileAttachment"/>）的文档投影，内嵌于 <see cref="ChatMessageDocument"/>。
/// </summary>
public sealed class ChatFileAttachmentDocument
{
    /// <summary>附件ID。</summary>
    public Guid AttachmentId { get; set; }
    /// <summary>所属消息ID。</summary>
    public Guid MessageId { get; set; }
    /// <summary>文件ID。</summary>
    public Guid FileId { get; set; }
    /// <summary>文件名。</summary>
    public string FileName { get; set; } = null!;
    /// <summary>文件类型。</summary>
    public string FileType { get; set; } = null!;
    /// <summary>文件大小（字节）。</summary>
    public long FileSize { get; set; }
    /// <summary>文件URI。</summary>
    public string FileUri { get; set; } = null!;
    /// <summary>缩略图URI。</summary>
    public string? ThumbnailUri { get; set; }
    /// <summary>MIME类型。</summary>
    public string? MimeType { get; set; }
    /// <summary>描述。</summary>
    public string? Description { get; set; }
    /// <summary>上传时间。</summary>
    public DateTime UploadTime { get; set; }
    /// <summary>下载时间。</summary>
    public DateTime? DownloadTime { get; set; }
    /// <summary>下载次数。</summary>
    public int DownloadCount { get; set; }
    /// <summary>是否已删除。</summary>
    public bool IsDeleted { get; set; }
}