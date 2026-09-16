namespace Message.Domain.Entities.Chat;

/// <summary>
/// 消息聚合根。
/// <para>
/// 内容载荷由单一多态值对象 <see cref="MessageContent"/> 承载（字段收敛，取代按类型拍平悬浮的散字段），
/// 各消息业务类型经 <see cref="MessageContent.MessageType"/> 判别；<see cref="MessageType"/> 由内容派生，
/// 实体的单一职责边界清晰、高内聚低耦合。
/// </para>
/// </summary>
public class Message : Entity<Guid>, IAggregateRoot
{
    private readonly List<FileAttachment> _attachments = new();

    private Message(Guid sessionId, Guid senderId, MessageContent content)
    {
        MessageId = Guid.NewGuid();
        SessionId = sessionId;
        SenderId = senderId;
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
    public MessageType MessageType => Content.MessageType;
    public MessageStatus Status { get; private set; }

    /// <summary>消息内容载荷（多态值对象，单一来源）。</summary>
    public MessageContent Content { get; private set; } = default!;

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
        var message = new Message(sessionId, senderId, TextContent.Create(content));
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateImageMessage(Guid sessionId, Guid senderId, Uri mediaUri, string? caption = null,
        string? thumbnailUri = null)
    {
        var message = new Message(sessionId, senderId, ImageContent.Create(mediaUri, thumbnailUri, caption));
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateVideoMessage(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null, string? thumbnailUri = null)
    {
        var message = new Message(sessionId, senderId, VideoContent.Create(mediaUri, durationSeconds, thumbnailUri, caption));
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateAudioMessage(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null)
    {
        var message = new Message(sessionId, senderId, AudioContent.Create(mediaUri, durationSeconds, caption));
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateFileMessage(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        long fileSize, string mimeType)
    {
        var message = new Message(sessionId, senderId, FileContent.Create(mediaUri, fileName, fileSize, mimeType));
        message.RaiseMessageSentEvent();
        return message;
    }


    public static Message CreateLocationMessage(Guid sessionId, Guid senderId, double latitude, double longitude,
        string locationName)
    {
        var message = new Message(sessionId, senderId, LocationContent.Create(latitude, longitude, locationName));
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateLinkMessage(Guid sessionId, Guid senderId, string linkUrl, string? title = null,
        string? description = null)
    {
        var uri = new Uri(linkUrl);
        var message = new Message(sessionId, senderId, LinkContent.Create(uri, title, description));
        message.RaiseMessageSentEvent();
        return message;
    }

    public static Message CreateExpressionMessage(Guid sessionId, Guid senderId, string expressionCode)
    {
        var message = new Message(sessionId, senderId, ExpressionContent.Create(expressionCode));
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

    public void Recall(Guid recalledBy, RecallReason reason, string? originalContent, IMessageRecallPolicy recallPolicy)
    {
        if (IsRecalled)
            throw new InvalidOperationException("消息已撤回");
        // 修复 S-05：仅消息发送者可撤回，防止越权撤回他人消息
        if (recalledBy != SenderId)
            throw new InvalidOperationException("只能撤回自己发送的消息");
        if (!recallPolicy.CanRecall(SentTime))
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
        MessageContent content, MessageStatus status,
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
            SentTime = sentTime,
            ReceiverId = receiverId,
            Content = content,
            Status = status,
            DeliveredTime = deliveredTime,
            ReadTime = readTime,
            IsRecalled = isRecalled,
            IsEncrypted = isEncrypted,
            IsForwarded = isForwarded,
            OriginalMessageId = originalMessageId,
            ReplyToMessageId = replyToMessageId
        };
        foreach (var attachment in attachments)
            message._attachments.Add(attachment);
        return message;
    }
}