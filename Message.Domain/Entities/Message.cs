using Message.Domain.Entities.Recall;
using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.Entities;

/// <summary>
///  消息
/// </summary>
public class Message : Entity, IAggregateRoot
{
    private readonly List<FileAttachment> _attachments = new();

    private Message(Guid sessionId, Guid senderId, MessageType messageType, string? content)
    {
        MessageId = Guid.NewGuid();
        SessionId = sessionId;
        SenderId = senderId;
        MessageType = messageType;
        Content = content;
        Status = MessageStatus.Pending;
        SentTime = DateTime.UtcNow;
        IsRecalled = false;
        IsEncrypted = false;
        IsForwarded = false;
    }

    private Message()
    {
        MessageId = Guid.NewGuid();
        SentTime = DateTime.UtcNow;
        Status = MessageStatus.Pending;
        IsRecalled = false;
        IsEncrypted = false;
        IsForwarded = false;
    }

    public Guid MessageId { get; init; }
    public Guid SessionId { get; init; }
    public Guid SenderId { get; init; }
    public Guid? ReceiverId { get; set; }
    public MessageType MessageType { get; private set; }
    public MessageStatus Status { get; private set; }
    public string? Content { get; private set; }
    public Uri? MediaUri { get; private set; }
    public string? ThumbnailUri { get; private set; }
    public double? FileSize { get; private set; }
    public double? Duration { get; private set; }
    public string? FileName { get; private set; }
    public string? MimeType { get; private set; }
    public string? Caption { get; set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string? LocationName { get; private set; }
    public string? LinkUrl { get; private set; }
    public string? LinkTitle { get; private set; }
    public string? LinkDescription { get; private set; }
    public string? ExpressionCode { get; private set; }
    public DateTime SentTime { get; init; }
    public DateTime? DeliveredTime { get; private set; }
    public DateTime? ReadTime { get; private set; }
    public bool IsRecalled { get; private set; }
    public bool IsEncrypted { get; private set; }
    public bool IsForwarded { get; private set; }
    public Guid? OriginalMessageId { get; private set; }
    public Guid? ReplyToMessageId { get; private set; }
    public IReadOnlyCollection<FileAttachment> Attachments => _attachments.AsReadOnly();

    public static Message CreateTextMessage(Guid sessionId, Guid senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("消息内容不能为空", nameof(content));

        return new Message(sessionId, senderId, MessageType.MessageText, content);
    }

    public static Message CreateImageMessage(Guid sessionId, Guid senderId, Uri mediaUri, string? caption = null,
        string? thumbnailUri = null)
    {
        var message = new Message(sessionId, senderId, MessageType.MessageImage, null)
        {
            MediaUri = mediaUri,
            ThumbnailUri = thumbnailUri,
            Caption = caption
        };
        return message;
    }

    public static Message CreateVideoMessage(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null, string? thumbnailUri = null)
    {
        var message = new Message(sessionId, senderId, MessageType.MessageVideo, null)
        {
            MediaUri = mediaUri,
            Duration = durationSeconds,
            ThumbnailUri = thumbnailUri,
            Caption = caption
        };
        return message;
    }

    public static Message CreateAudioMessage(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null)
    {
        var message = new Message(sessionId, senderId, MessageType.MessageAudio, null)
        {
            MediaUri = mediaUri,
            Duration = durationSeconds,
            Caption = caption
        };
        return message;
    }

    public static Message CreateFileMessage(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        double fileSize, string mimeType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));

        var message = new Message(sessionId, senderId, MessageType.MessageFile, null)
        {
            MediaUri = mediaUri,
            FileName = fileName,
            FileSize = fileSize,
            MimeType = mimeType
        };
        return message;
    }

    public static Message CreateLocationMessage(Guid sessionId, Guid senderId, double latitude, double longitude,
        string locationName)
    {
        var message = new Message(sessionId, senderId, MessageType.MessageLocation, null)
        {
            Latitude = latitude,
            Longitude = longitude,
            LocationName = locationName
        };
        return message;
    }

    public static Message CreateLinkMessage(Guid sessionId, Guid senderId, string linkUrl, string? title = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(linkUrl))
            throw new ArgumentException("链接地址不能为空", nameof(linkUrl));

        var message = new Message(sessionId, senderId, MessageType.MessageLink, null)
        {
            LinkUrl = linkUrl,
            LinkTitle = title,
            LinkDescription = description
        };
        return message;
    }

    public static Message CreateExpressionMessage(Guid sessionId, Guid senderId, string expressionCode)
    {
        if (string.IsNullOrWhiteSpace(expressionCode))
            throw new ArgumentException("表情代码不能为空", nameof(expressionCode));

        return new Message(sessionId, senderId, MessageType.MessageExpression, expressionCode)
        {
            ExpressionCode = expressionCode
        };
    }

    public void SetReceiver(Guid receiverId)
    {
        if (ReceiverId.HasValue)
            throw new InvalidOperationException("接收者已设置");
        ReceiverId = receiverId;
    }

    public void MarkAsSent()
    {
        if (Status != MessageStatus.Pending)
            throw new InvalidOperationException("只有待发送的消息可以标记为已发送");
        Status = MessageStatus.Sent;
    }

    public void MarkAsDelivered()
    {
        if (Status != MessageStatus.Sent)
            throw new InvalidOperationException("只有已发送的消息可以标记为已送达");
        Status = MessageStatus.Delivered;
        DeliveredTime = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        if (Status == MessageStatus.Read)
            return;
        Status = MessageStatus.Read;
        ReadTime = DateTime.UtcNow;
    }

    public void Recall(Guid recalledBy, RecallReason reason, string? originalContent)
    {
        if (IsRecalled)
            throw new InvalidOperationException("消息已撤回");
        if (!MessageRecall.CanRecall(SentTime))
            throw new InvalidOperationException("超过撤回时限");

        IsRecalled = true;
        Status = MessageStatus.Recalled;
    }

    public void MarkAsForwarded(Guid originalMessageId)
    {
        if (IsForwarded)
            throw new InvalidOperationException("消息已标记为转发");
        IsForwarded = true;
        OriginalMessageId = originalMessageId;
    }

    public void SetReplyTo(Guid originalMessageId)
    {
        ReplyToMessageId = originalMessageId;
    }

    public void Encrypt()
    {
        if (IsEncrypted)
            throw new InvalidOperationException("消息已加密");
        IsEncrypted = true;
    }

    public void AddAttachment(FileAttachment attachment)
    {
        _attachments.Add(attachment);
    }

    public void RemoveAttachment(Guid attachmentId)
    {
        var attachment = _attachments.FirstOrDefault(a => a.AttachmentId == attachmentId);
        if (attachment != null)
            _attachments.Remove(attachment);
    }
}