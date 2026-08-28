using Message.Domain.Entities.Recall;

namespace Message.Domain.Entities.Chat;

/// <summary>
/// 
/// </summary>
public class Message : Entity<Guid>, IAggregateRoot
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
    public Guid? ReceiverId { get; private set; }
    public MessageType MessageType { get; private set; }
    public MessageStatus Status { get; private set; }

    public string? Content { get; private set; }
    public Uri? MediaUri { get; private set; }
    public string? ThumbnailUri { get; private set; }
    public long? FileSize { get; private set; }
    public double? Duration { get; private set; }
    public string? FileName { get; private set; }
    public string? MimeType { get; private set; }
    public string? Caption { get; private set; }
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
        var textContent = TextContent.Create(content);
        var message = new Message(sessionId, senderId, MessageType.MessageText, textContent.Value);
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateImageMessage(Guid sessionId, Guid senderId, Uri mediaUri, string? caption = null,
        string? thumbnailUri = null)
    {
        var mediaContent = MediaContent.Create(mediaUri, thumbnailUri, caption);
        var message = new Message(sessionId, senderId, MessageType.MessageImage, null)
        {
            MediaUri = mediaContent.MediaUri,
            ThumbnailUri = mediaContent.ThumbnailUri,
            Caption = mediaContent.Caption
        };
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateVideoMessage(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null, string? thumbnailUri = null)
    {
        var mediaContent = MediaContent.Create(mediaUri, thumbnailUri, caption, durationSeconds);
        var message = new Message(sessionId, senderId, MessageType.MessageVideo, null)
        {
            MediaUri = mediaContent.MediaUri,
            Duration = mediaContent.Duration,
            ThumbnailUri = mediaContent.ThumbnailUri,
            Caption = mediaContent.Caption
        };
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateAudioMessage(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null)
    {
        var mediaContent = MediaContent.Create(mediaUri, null, caption, durationSeconds);
        var message = new Message(sessionId, senderId, MessageType.MessageAudio, null)
        {
            MediaUri = mediaContent.MediaUri,
            Duration = mediaContent.Duration,
            Caption = mediaContent.Caption
        };
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateFileMessage(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        long fileSize, string mimeType)
    {
        var fileContent = FileContent.Create(mediaUri, fileName, fileSize, mimeType);
        var message = new Message(sessionId, senderId, MessageType.MessageFile, null)
        {
            MediaUri = fileContent.FileUri,
            FileName = fileContent.FileName,
            FileSize = fileContent.FileSize,
            MimeType = fileContent.MimeType
        };
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateLocationMessage(Guid sessionId, Guid senderId, double latitude, double longitude,
        string locationName)
    {
        var locationContent = LocationContent.Create(latitude, longitude, locationName);
        var message = new Message(sessionId, senderId, MessageType.MessageLocation, null)
        {
            Latitude = locationContent.Latitude,
            Longitude = locationContent.Longitude,
            LocationName = locationContent.LocationName
        };
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateLinkMessage(Guid sessionId, Guid senderId, string linkUrl, string? title = null,
        string? description = null)
    {
        var uri = new Uri(linkUrl);
        var linkContent = LinkContent.Create(uri, title, description);
        var message = new Message(sessionId, senderId, MessageType.MessageLink, null)
        {
            LinkUrl = linkContent.Url.ToString(),
            LinkTitle = linkContent.Title,
            LinkDescription = linkContent.Description
        };
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateExpressionMessage(Guid sessionId, Guid senderId, string expressionCode)
    {
        if (string.IsNullOrWhiteSpace(expressionCode))
            throw new ArgumentException("表情代码不能为空", nameof(expressionCode));

        var message = new Message(sessionId, senderId, MessageType.MessageExpression, expressionCode)
        {
            ExpressionCode = expressionCode
        };
        message.RaiseMessageSentEvent();
        return message;
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
        AddDomainEvent(new MessageReceivedEvent(MessageId, ReceiverId ?? Guid.Empty, DateTime.UtcNow));
    }

    public void MarkAsRead()
    {
        if (Status == MessageStatus.Read)
            return;
        Status = MessageStatus.Read;
        ReadTime = DateTime.UtcNow;
        AddDomainEvent(new MessageReadEvent(MessageId, ReceiverId ?? Guid.Empty, DateTime.UtcNow));
    }

    public void Recall(Guid recalledBy, RecallReason reason, string? originalContent)
    {
        if (IsRecalled)
            throw new InvalidOperationException("消息已撤回");
        // 修复 S-05：仅消息发送者可撤回，防止越权撤回他人消息
        if (recalledBy != SenderId)
            throw new InvalidOperationException("只能撤回自己发送的消息");
        if (!MessageRecall.CanRecall(SentTime))
            throw new InvalidOperationException("超过撤回时限");

        IsRecalled = true;
        Status = MessageStatus.Recalled;
        AddDomainEvent(new MessageRecalledEvent(MessageId, recalledBy, reason));
    }

    public void MarkAsForwarded(Guid originalMessageId)
    {
        if (IsForwarded)
            throw new InvalidOperationException("消息已标记为转发");
        IsForwarded = true;
        OriginalMessageId = originalMessageId;
        AddDomainEvent(new MessageForwardedEvent(originalMessageId, MessageId, SenderId, SessionId));
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

    private void RaiseMessageSentEvent()
    {
        AddDomainEvent(new MessageSentEvent(MessageId, SenderId, ReceiverId, SessionId, MessageType));
    }

    /// <summary>
    /// 从持久化数据重建消息聚合根（Mongo 投影读取路径）。
    /// <para>重构已校验、已持久化的实体，不重复触发领域工厂校验、不重新产生领域事件。</para>
    /// </summary>
    public static Message Rebuild(
        Guid messageId, Guid sessionId, Guid senderId, Guid? receiverId,
        MessageType messageType, MessageStatus status,
        string? content, Uri? mediaUri, string? thumbnailUri, long? fileSize, double? duration,
        string? fileName, string? mimeType, string? caption,
        double? latitude, double? longitude, string? locationName,
        string? linkUrl, string? linkTitle, string? linkDescription, string? expressionCode,
        DateTime sentTime, DateTime? deliveredTime, DateTime? readTime,
        bool isRecalled, bool isEncrypted, bool isForwarded,
        Guid? originalMessageId, Guid? replyToMessageId,
        IReadOnlyCollection<FileAttachment> attachments)
    {
        var message = new Message
        {
            MessageId = messageId,
            SessionId = sessionId,
            SenderId = senderId,
            SentTime = sentTime
        };
        message.ReceiverId = receiverId;
        message.MessageType = messageType;
        message.Status = status;
        message.Content = content;
        message.MediaUri = mediaUri;
        message.ThumbnailUri = thumbnailUri;
        message.FileSize = fileSize;
        message.Duration = duration;
        message.FileName = fileName;
        message.MimeType = mimeType;
        message.Caption = caption;
        message.Latitude = latitude;
        message.Longitude = longitude;
        message.LocationName = locationName;
        message.LinkUrl = linkUrl;
        message.LinkTitle = linkTitle;
        message.LinkDescription = linkDescription;
        message.ExpressionCode = expressionCode;
        message.DeliveredTime = deliveredTime;
        message.ReadTime = readTime;
        message.IsRecalled = isRecalled;
        message.IsEncrypted = isEncrypted;
        message.IsForwarded = isForwarded;
        message.OriginalMessageId = originalMessageId;
        message.ReplyToMessageId = replyToMessageId;
        foreach (var attachment in attachments)
            message._attachments.Add(attachment);
        return message;
    }
}