
namespace Message.Web.API.Application.Commands.Messages;
/// <summary>
/// 撤回消息命令处理程序。
/// </summary>
public class RecallMessageCommandHandler(
    IMessageRepository messageRepository,
    ILogger<RecallMessageCommandHandler> logger,
     MessageCacheService redisCache,
    MessageDeliveryService delivery,
    IChatSessionRepository sessionRepository) : IRequestHandler<RecallMessageCommand, bool>
{
    public async Task<bool> Handler(RecallMessageCommand command, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(command.MessageId);
        if (message == null)
            throw new KeyNotFoundException("消息不存在");

        message.Recall(command.UserId, command.Reason, null);
        await messageRepository.UpdateAsync(message);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Q-05：撤回改变消息状态，失效发送者/接收者的消息详情缓存（Key 带用户维度，与 MessagesApi.GetMessageAsync 一致）
        await redisCache.RemoveAsync(MessageCacheKey(message.SenderId, command.MessageId), cancellationToken);
        if (message.ReceiverId is { } receiverId)
            await redisCache.RemoveAsync(MessageCacheKey(receiverId, command.MessageId), cancellationToken);

        // R-01：REST 撤回路径补齐实时推送（与 MessageHub.RecallMessage 行为一致；推送失败不阻断命令）
        try
        {
            var session = await sessionRepository.GetByIdAsync(message.SessionId);
            if (session is not null)
                await delivery.NotifyMessageRecalledAsync(
                    message.SessionId, message.MessageId, session.Participants, ct: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "REST 撤回消息后实时推送失败：{MessageId}", message.MessageId);
        }

        logger.LogInformation("消息 {MessageId} 已撤回，原因={Reason}", command.MessageId, command.Reason);
        return true;
    }

    /// <summary>消息详情缓存 Key（带用户维度，与 MessagesApi.GetMessageAsync 保持一致）</summary>
    private static string MessageCacheKey(Guid userId, Guid messageId) => $"message:msg:{userId}:{messageId}";
}
