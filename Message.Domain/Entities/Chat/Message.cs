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
        MessageId = Guid.CreateVersion7();
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
        MessageId = Guid.CreateVersion7();
        SentTime = DateTime.UtcNow;
        Status = MessageStatus.Pending;
        IsRecalled = false;
        IsEncrypted = false;
        IsForwarded = false;
    }

    /// <summary>消息ID</summary>
    public Guid MessageId { get; init; }
    /// <summary>所属会话ID</summary>
    public Guid SessionId { get; init; }
    /// <summary>发送者用户ID</summary>
    public Guid SenderId { get; init; }
    /// <summary>接收者用户ID（私聊接收方，可空）</summary>
    public Guid? ReceiverId { get; private set; }
    /// <summary>消息类型（由内容载荷派生）</summary>
    public MessageType MessageType => Content.MessageType;
    /// <summary>消息状态</summary>
    public MessageStatus Status { get; private set; }

    /// <summary>消息内容载荷（多态值对象，单一来源）。</summary>
    public MessageContent Content { get; private set; } = default!;

    /// <summary>发送时间</summary>
    public DateTime SentTime { get; init; }
    /// <summary>送达时间</summary>
    public DateTime? DeliveredTime { get; private set; }
    /// <summary>读取时间</summary>
    public DateTime? ReadTime { get; private set; }
    /// <summary>是否已撤回</summary>
    public bool IsRecalled { get; private set; }
    /// <summary>是否已加密</summary>
    public bool IsEncrypted { get; private set; }
    /// <summary>是否已转发</summary>
    public bool IsForwarded { get; private set; }
    /// <summary>原消息ID（转发时指向被转发的源消息）</summary>
    public Guid? OriginalMessageId { get; private set; }
    /// <summary>回复的目标消息ID</summary>
    public Guid? ReplyToMessageId { get; private set; }

    /// <summary>消息附件集合</summary>
    public IReadOnlyCollection<FileAttachment> Attachments => _attachments.AsReadOnly();

    /// <summary>创建文本消息</summary>
    public static Message CreateTextMessage(Guid sessionId, Guid senderId, string content)
    {
        var message = new Message(sessionId, senderId, TextContent.Create(content));
        message.RaiseMessageSentEvent();
        return message;
    }

    /// <summary>创建图片消息</summary>
    public static Message CreateImageMessage(Guid sessionId, Guid senderId, Uri mediaUri, string? caption = null,
        string? thumbnailUri = null)
    {
        var message = new Message(sessionId, senderId, ImageContent.Create(mediaUri, thumbnailUri, caption));
        message.RaiseMessageSentEvent();
        return message;
    }

    /// <summary>创建视频消息</summary>
    public static Message CreateVideoMessage(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null, string? thumbnailUri = null)
    {
        var message = new Message(sessionId, senderId, VideoContent.Create(mediaUri, durationSeconds, thumbnailUri, caption));
        message.RaiseMessageSentEvent();
        return message;
    }

    /// <summary>创建语音消息</summary>
    public static Message CreateAudioMessage(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null)
    {
        var message = new Message(sessionId, senderId, AudioContent.Create(mediaUri, durationSeconds, caption));
        message.RaiseMessageSentEvent();
        return message;
    }

    /// <summary>创建文件消息</summary>
    public static Message CreateFileMessage(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        long fileSize, string mimeType)
    {
        var message = new Message(sessionId, senderId, FileContent.Create(mediaUri, fileName, fileSize, mimeType));
        message.RaiseMessageSentEvent();
        return message;
    }


    /// <summary>创建位置消息</summary>
    public static Message CreateLocationMessage(Guid sessionId, Guid senderId, double latitude, double longitude,
        string locationName)
    {
        var message = new Message(sessionId, senderId, LocationContent.Create(latitude, longitude, locationName));
        message.RaiseMessageSentEvent();
        return message;
    }

    /// <summary>创建链接消息</summary>
    public static Message CreateLinkMessage(Guid sessionId, Guid senderId, string linkUrl, string? title = null,
        string? description = null)
    {
        var uri = new Uri(linkUrl);
        var message = new Message(sessionId, senderId, LinkContent.Create(uri, title, description));
        message.RaiseMessageSentEvent();
        return message;
    }

    /// <summary>创建表情消息</summary>
    public static Message CreateExpressionMessage(Guid sessionId, Guid senderId, string expressionCode)
    {
        var message = new Message(sessionId, senderId, ExpressionContent.Create(expressionCode));
        message.RaiseMessageSentEvent();
        return message;
    }

    /// <summary>设置接收者（仅可设置一次）</summary>
    public void SetReceiver(Guid receiverId)
    {
        if (ReceiverId.HasValue)
            throw new InvalidOperationException("接收者已设置");
        ReceiverId = receiverId;
    }

    /// <summary>标记为已发送</summary>
    public void MarkAsSent()
    {
        if (Status != MessageStatus.Pending)
            throw new InvalidOperationException("只有待发送的消息可以标记为已发送");
        Status = MessageStatus.Sent;
    }

    /// <summary>标记为已送达</summary>
    public void MarkAsDelivered()
    {
        if (Status != MessageStatus.Sent)
            throw new InvalidOperationException("只有已发送的消息可以标记为已送达");
        Status = MessageStatus.Delivered;
        DeliveredTime = DateTime.UtcNow;
        AddDomainEvent(new MessageReceivedEvent(MessageId, ReceiverId ?? Guid.Empty, DateTime.UtcNow));
    }

    /// <summary>标记为已读</summary>
    public void MarkAsRead()
    {
        if (Status == MessageStatus.Read)
            return;
        Status = MessageStatus.Read;
        ReadTime = DateTime.UtcNow;
        AddDomainEvent(new MessageReadEvent(MessageId, ReceiverId ?? Guid.Empty, DateTime.UtcNow));
    }

    /// <summary>撤回消息（仅发送者可撤回，且需满足撤回策略）</summary>
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

    /// <summary>标记为转发（记录原消息ID）</summary>
    public void MarkAsForwarded(Guid originalMessageId)
    {
        if (IsForwarded)
            throw new InvalidOperationException("消息已标记为转发");
        IsForwarded = true;
        OriginalMessageId = originalMessageId;
        AddDomainEvent(new MessageForwardedEvent(originalMessageId, MessageId, SenderId, SessionId));
    }

    /// <summary>设置回复的目标消息</summary>
    public void SetReplyTo(Guid originalMessageId)
    {
        ReplyToMessageId = originalMessageId;
    }

    /// <summary>加密消息</summary>
    public void Encrypt()
    {
        if (IsEncrypted)
            throw new InvalidOperationException("消息已加密");
        IsEncrypted = true;
    }

    /// <summary>添加附件</summary>
    public void AddAttachment(FileAttachment attachment)
    {
        _attachments.Add(attachment);
    }

    /// <summary>移除指定附件</summary>
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