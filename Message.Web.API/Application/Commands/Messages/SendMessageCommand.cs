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
    ILogger<SendMessageCommandHandler> logger) : IRequestHandler<SendMessageCommand, Guid>
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

        logger.LogInformation("发送消息成功：{MessageId}，类型={MessageType}，会话={SessionId}",
            message.MessageId, message.MessageType, message.SessionId);
        return message.MessageId;
    }

    private async Task<MessageEntity> SendTextMessageAsync(Guid sessionId, Guid senderId, string content,
        CancellationToken cancellationToken)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateTextMessage(sessionId, senderId, content);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, content);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendImageMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        string? caption = null, string? thumbnailUri = null, CancellationToken cancellationToken = default)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateImageMessage(sessionId, senderId, mediaUri, caption, thumbnailUri);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[图片]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendVideoMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        double durationSeconds, string? caption = null, string? thumbnailUri = null,
        CancellationToken cancellationToken = default)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message =
            MessageEntity.CreateVideoMessage(sessionId, senderId, mediaUri, durationSeconds, caption, thumbnailUri);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[视频]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendAudioMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        double durationSeconds, string? caption = null, CancellationToken cancellationToken = default)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateAudioMessage(sessionId, senderId, mediaUri, durationSeconds, caption);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[语音]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendFileMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        long fileSize, string mimeType, CancellationToken cancellationToken)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateFileMessage(sessionId, senderId, mediaUri, fileName, fileSize, mimeType);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[文件] {fileName}");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendLocationMessageAsync(Guid sessionId, Guid senderId, double latitude,
        double longitude, string locationName, CancellationToken cancellationToken)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateLocationMessage(sessionId, senderId, latitude, longitude, locationName);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[位置] {locationName}");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendLinkMessageAsync(Guid sessionId, Guid senderId, string linkUrl,
        string? title = null, string? description = null, CancellationToken cancellationToken = default)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateLinkMessage(sessionId, senderId, linkUrl, title, description);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, title ?? linkUrl);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendExpressionMessageAsync(Guid sessionId, Guid senderId, string expressionCode,
        CancellationToken cancellationToken)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateExpressionMessage(sessionId, senderId, expressionCode);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[表情]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task ValidateSessionAndSenderAsync(Guid sessionId, Guid senderId)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException("会话不存在");
    }

    private async Task UpdateSessionLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        await sessionRepository.UpdateLastMessageAsync(sessionId, messageId, content);
    }
}
