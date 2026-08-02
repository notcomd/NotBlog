using Message.Infrastructure.Services;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Commands.Messages;

/// <summary>
/// 发送消息命令。
/// <para>CQRS 命令侧：仅返回新消息的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="SenderId">发送者用户 ID</param>
/// <param name="MessageType">消息类型</param>
/// <param name="Content">文本内容（文本消息必填）</param>
/// <param name="MediaUrl">媒体文件地址（图片/视频/音频/文件消息必填）</param>
/// <param name="ThumbnailUrl">缩略图地址（图片/视频消息可选）</param>
/// <param name="FileName">文件名（文件消息必填）</param>
/// <param name="FileSize">文件大小（文件消息必填）</param>
/// <param name="MimeType">MIME 类型（文件消息必填）</param>
/// <param name="Duration">媒体时长（视频/音频消息可选）</param>
/// <param name="Caption">媒体描述（图片/视频/音频消息可选）</param>
/// <param name="Latitude">纬度（位置消息必填）</param>
/// <param name="Longitude">经度（位置消息必填）</param>
/// <param name="LocationName">位置名称（位置消息必填）</param>
/// <param name="LinkUrl">链接地址（链接消息必填）</param>
/// <param name="LinkTitle">链接标题（链接消息可选）</param>
/// <param name="LinkDescription">链接描述（链接消息可选）</param>
/// <param name="ExpressionCode">表情代码（表情消息必填）</param>
public record SendMessageCommand(
    Guid SessionId,
    Guid SenderId,
    MessageType MessageType,
    string? Content,
    Uri? MediaUrl,
    string? ThumbnailUrl,
    string? FileName,
    long? FileSize,
    string? MimeType,
    double? Duration,
    string? Caption,
    double? Latitude,
    double? Longitude,
    string? LocationName,
    string? LinkUrl,
    string? LinkTitle,
    string? LinkDescription,
    string? ExpressionCode) : IRequest<Guid>;

/// <summary>
/// 发送消息命令处理程序。
/// <para>按消息类型分发到领域服务创建消息实体（文本/图片/视频/音频/文件/位置/链接/表情）。</para>
/// </summary>
public class SendMessageCommandHandler(
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ILogger<SendMessageCommandHandler> logger,
    UnreadCountCacheService unreadCountCache,
    SessionCacheService sessionCache) : IRequestHandler<SendMessageCommand, Guid>
{
    public async Task<Guid> Handler(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var message = command.MessageType switch
        {
            MessageType.MessageText => await SendTextMessageAsync(command.SessionId, command.SenderId,
                command.Content ?? "", cancellationToken),
            MessageType.MessageImage => await SendImageMessageAsync(command.SessionId, command.SenderId,
                command.MediaUrl!, command.Caption, command.ThumbnailUrl, cancellationToken),
            MessageType.MessageVideo => await SendVideoMessageAsync(command.SessionId, command.SenderId,
                command.MediaUrl!, command.Duration ?? 0, command.Caption, command.ThumbnailUrl, cancellationToken),
            MessageType.MessageAudio => await SendAudioMessageAsync(command.SessionId, command.SenderId,
                command.MediaUrl!, command.Duration ?? 0, command.Caption, cancellationToken),
            MessageType.MessageFile => await SendFileMessageAsync(command.SessionId, command.SenderId,
                command.MediaUrl!, command.FileName!, command.FileSize ?? 0, command.MimeType!, cancellationToken),
            MessageType.MessageLocation => await SendLocationMessageAsync(command.SessionId, command.SenderId,
                command.Latitude ?? 0, command.Longitude ?? 0, command.LocationName!, cancellationToken),
            MessageType.MessageLink => await SendLinkMessageAsync(command.SessionId, command.SenderId,
                command.LinkUrl!, command.LinkTitle, command.LinkDescription, cancellationToken),
            MessageType.MessageExpression => await SendExpressionMessageAsync(command.SessionId,
                command.SenderId, command.ExpressionCode!, cancellationToken),
            _ => throw new NotSupportedException($"不支持的消息类型: {command.MessageType}")
        };

        // Q-05：未读计数缓存采用"写时失效"策略——私聊消息会改变接收者未读数，直接删除缓存键，
        // 读路径 miss 时回源 DB 重建（群聊消息 ReceiverId 为空，不参与未读计数，无需失效）
        if (message.ReceiverId is { } receiverId)
            await unreadCountCache.InvalidateAsync(receiverId, cancellationToken);

        // Q-05：会话最后消息已变化，失效会话详情缓存
        await sessionCache.InvalidateSessionAsync(command.SessionId, cancellationToken);

        logger.LogInformation("发送消息成功：{MessageId}，类型={MessageType}，会话={SessionId}",
            message.MessageId, message.MessageType, message.SessionId);
        return message.MessageId;
    }

    private async Task<MessageEntity> SendTextMessageAsync(Guid sessionId, Guid senderId, string content,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        // S-17：内容净化 + 长度校验 + 敏感词过滤（拒绝策略）
        content = SafeContentSanitizer.Sanitize(content);
        if (content.Length > 2000)
            throw new ArgumentException("消息内容不能超过2000个字符");
        RejectIfSensitive(content, "消息");

        var message = MessageEntity.CreateTextMessage(sessionId, senderId, content);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, content);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendImageMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        string? caption = null, string? thumbnailUri = null, CancellationToken cancellationToken = default)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        caption = SanitizeField(caption);
        var message = MessageEntity.CreateImageMessage(sessionId, senderId, mediaUri, caption, thumbnailUri);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[图片]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendVideoMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        double durationSeconds, string? caption = null, string? thumbnailUri = null,
        CancellationToken cancellationToken = default)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message =
            MessageEntity.CreateVideoMessage(sessionId, senderId, mediaUri, durationSeconds, caption, thumbnailUri);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[视频]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendAudioMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        double durationSeconds, string? caption = null, CancellationToken cancellationToken = default)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        caption = SanitizeField(caption);
        var message = MessageEntity.CreateAudioMessage(sessionId, senderId, mediaUri, durationSeconds, caption);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[语音]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendFileMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        long fileSize, string mimeType, CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateFileMessage(sessionId, senderId, mediaUri, fileName, fileSize, mimeType);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[文件] {fileName}");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendLocationMessageAsync(Guid sessionId, Guid senderId, double latitude,
        double longitude, string locationName, CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateLocationMessage(sessionId, senderId, latitude, longitude, locationName);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[位置] {locationName}");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendLinkMessageAsync(Guid sessionId, Guid senderId, string linkUrl,
        string? title = null, string? description = null, CancellationToken cancellationToken = default)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        title = SanitizeField(title);
        description = SanitizeField(description);
        var message = MessageEntity.CreateLinkMessage(sessionId, senderId, linkUrl, title, description);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, title ?? linkUrl);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendExpressionMessageAsync(Guid sessionId, Guid senderId, string expressionCode,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateExpressionMessage(sessionId, senderId, expressionCode);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[表情]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    /// <summary>
    /// F-04：私聊会话设置消息接收者（对端用户），激活离线消息/未读查询（GetUnreadMessagesAsync 依赖 ReceiverId）；群聊保持 null。
    /// </summary>
    private static void ApplyPrivateReceiver(MessageEntity message, ChatSession session, Guid senderId)
    {
        if (session.SessionType != SessionType.Private)
            return;
        var receiver = session.Participants.FirstOrDefault(p => p != senderId);
        if (receiver != Guid.Empty)
            message.SetReceiver(receiver);
    }

    /// <summary>S-17：净化可选文本字段（null/空白原样保留，避免引入空字符串语义差异）。</summary>
    private static string? SanitizeField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var sanitized = SafeContentSanitizer.Sanitize(value);
        // P-05：附加文本字段（媒体描述/链接标题/链接描述）长度上限
        if (sanitized.Length > 2000)
            throw new ArgumentException("消息附加文本不能超过2000个字符");
        return sanitized;
    }

    /// <summary>S-17：敏感词拒绝策略，命中时抛出业务异常（API 层映射为 400）。</summary>
    private static void RejectIfSensitive(string content, string fieldName)
    {
        var (isSensitive, matchedWord) = SensitiveWordFilter.ContainsSensitive(content);
        if (isSensitive)
            throw new InvalidOperationException($"{fieldName}包含敏感内容（{matchedWord}），已拒绝发送");
    }

    private async Task<ChatSession> ValidateSessionAndSenderAsync(Guid sessionId, Guid senderId)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException("会话不存在");
        // 修复 S-05：与 MessageHub 行为一致，非参与者禁止向会话发送消息（REST 与 Hub 对齐）
        if (!session.IsParticipant(senderId))
            throw new InvalidOperationException("您不是该会话的参与者");
        return session;
    }

    private async Task UpdateSessionLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        await sessionRepository.UpdateLastMessageAsync(sessionId, messageId, content);
    }
}
