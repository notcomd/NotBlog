
namespace Message.Web.API.Application.Commands.Messages;
/// <summary>
/// 标记消息为已读命令处理程序。
/// </summary>
public class MarkMessageAsReadCommandHandler(
    IMessageRepository messageRepository,
    ILogger<MarkMessageAsReadCommandHandler> logger,
    UnreadCountCacheService unreadCountCache,
    MessageDeliveryService delivery,
    IChatSessionRepository sessionRepository) : IRequestHandler<MarkMessageAsReadCommand, bool>
{
    public async Task<bool> Handler(MarkMessageAsReadCommand command, CancellationToken cancellationToken)
    {
        await messageRepository.MarkAsReadAsync(command.MessageId, command.UserId);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Q-05：已读会改变未读数，写时失效未读计数缓存（读时回源 DB 重建）
        await unreadCountCache.InvalidateAsync(command.UserId, cancellationToken);

        // R-01：REST 已读路径补齐实时推送（与 MessageHub.MarkAsRead 行为一致；推送失败不阻断命令）
        try
        {
            var message = await messageRepository.GetByIdAsync(command.MessageId);
            if (message is not null)
            {
                var session = await sessionRepository.GetByIdAsync(message.SessionId);
                if (session is not null)
                    await delivery.NotifyMessageReadAsync(
                        message.SessionId, message.MessageId, command.UserId, session.Participants, ct: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "REST 标记已读后实时推送失败：{MessageId}", command.MessageId);
        }

        logger.LogInformation("消息 {MessageId} 已标记为已读，用户={UserId}", command.MessageId, command.UserId);
        return true;
    }
}
