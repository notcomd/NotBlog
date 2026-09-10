using Message.Domain.ValueObjects.Message;
using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Web.API.Application.Commands.Messages;
/// <summary>
/// 转发消息命令处理程序。
/// </summary>
public class ForwardMessageCommandHandler(
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ILogger<ForwardMessageCommandHandler> logger) : IRequestHandler<ForwardMessageCommand, Guid>
{
    public async Task<Guid> Handler(ForwardMessageCommand command, CancellationToken cancellationToken)
    {
        var originalMessage = await messageRepository.GetByIdAsync(command.MessageId);
        if (originalMessage == null)
            throw new KeyNotFoundException("原消息不存在");

        // 修复 S-05：调用者必须在源消息所在会话中（防越权转发他人会话消息）
        var sourceSession = await sessionRepository.GetByIdAsync(originalMessage.SessionId);
        if (sourceSession == null || !sourceSession.IsParticipant(command.ForwardedBy))
            throw new UnauthorizedAccessException("您不是源会话的参与者");

        var targetSession = await ValidateSessionAndSenderAsync(command.TargetSessionId, command.ForwardedBy);

        var forwardedMessage = CreateForwardedMessage(originalMessage, command.TargetSessionId, command.ForwardedBy);
        forwardedMessage.MarkAsForwarded(command.MessageId);
        ApplyPrivateReceiver(forwardedMessage, targetSession, command.ForwardedBy);

        // 发送链路完成即置「已发送」——Mongo 仓库 AddAsync 即时落库，必须先于插入执行；
        // 前端以 0=发送中/1=已发送 渲染自己消息的发送状态
        forwardedMessage.MarkAsSent();
        await messageRepository.AddAsync(forwardedMessage);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("消息 {MessageId} 转发成功：新消息 {NewMessageId}，目标会话={TargetSessionId}",
            command.MessageId, forwardedMessage.MessageId, command.TargetSessionId);
        return forwardedMessage.MessageId;
    }

    /// <summary>
    /// F-04：私聊目标会话设置消息接收者（对端用户），激活离线消息/未读查询；群聊保持 null。
    /// </summary>
    private static void ApplyPrivateReceiver(MessageEntity message, ChatSession session, Guid senderId)
    {
        if (session.SessionType != SessionType.Private)
            return;
        var receiver = session.Participants.FirstOrDefault(p => p != senderId);
        if (receiver != Guid.Empty)
            message.SetReceiver(receiver);
    }

    private async Task<ChatSession> ValidateSessionAndSenderAsync(Guid sessionId, Guid senderId)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException("会话不存在");
        // 修复 S-05：调用者必须是目标会话参与者
        if (!session.IsParticipant(senderId))
            throw new UnauthorizedAccessException("您不是目标会话的参与者");
        return session;
    }

    /// <summary>
    /// 创建转发消息
    /// </summary>
    /// <param name="original"></param>
    /// <param name="targetSessionId"></param>
    /// <param name="forwardedBy"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private MessageEntity CreateForwardedMessage(MessageEntity original, Guid targetSessionId, Guid forwardedBy)
    {
        var content = original.Content;
        return original.MessageType switch
        {
            MessageType.MessageText => MessageEntity.CreateTextMessage(targetSessionId, forwardedBy,
                ((TextContent)content).Value),
            MessageType.MessageImage => MessageEntity.CreateImageMessage(targetSessionId, forwardedBy,
                ((MediaContent)content).MediaUri, ((MediaContent)content).Caption, ((MediaContent)content).ThumbnailUri),
            MessageType.MessageVideo => MessageEntity.CreateVideoMessage(targetSessionId, forwardedBy,
                ((MediaContent)content).MediaUri, ((MediaContent)content).Duration ?? 0,
                ((MediaContent)content).Caption, ((MediaContent)content).ThumbnailUri),
            MessageType.MessageAudio => MessageEntity.CreateAudioMessage(targetSessionId, forwardedBy,
                ((MediaContent)content).MediaUri, ((MediaContent)content).Duration ?? 0, ((MediaContent)content).Caption),
            MessageType.MessageFile => MessageEntity.CreateFileMessage(targetSessionId, forwardedBy,
                ((FileContent)content).FileUri, ((FileContent)content).FileName,
                ((FileContent)content).FileSize, ((FileContent)content).MimeType),
            MessageType.MessageLocation => MessageEntity.CreateLocationMessage(targetSessionId, forwardedBy,
                ((LocationContent)content).Latitude, ((LocationContent)content).Longitude,
                ((LocationContent)content).LocationName),
            MessageType.MessageLink => MessageEntity.CreateLinkMessage(targetSessionId, forwardedBy,
                ((LinkContent)content).Url.ToString(), ((LinkContent)content).Title, ((LinkContent)content).Description),
            MessageType.MessageExpression => MessageEntity.CreateExpressionMessage(targetSessionId, forwardedBy,
                ((ExpressionContent)content).Value),
            _ => throw new NotSupportedException($"不支持的消息类型: {original.MessageType}")
        };
    }
}
