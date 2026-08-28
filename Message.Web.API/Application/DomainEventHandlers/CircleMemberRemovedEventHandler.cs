
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 成员被移出圈子事件处理：实时推送 circle:{id} 频道 + 事件总线 + 同步社区聊天会话参与者。
/// </summary>
public class CircleMemberRemovedEventHandler(
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
    IChatSessionRepository sessionRepository,
    ILogger<CircleMemberRemovedEventHandler> logger) : INotificationHandler<CircleMemberRemovedEvent>
{
    public async Task Handler(CircleMemberRemovedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await deliveryService.PushMemberRemovedAsync(notification.CircleGuid, notification.UserGuid);

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.member.removed", notification.OperatorGuid,
                notification.CircleGuid, notification.UserGuid, notification), cancellationToken);

            // 事件驱动：同步移出社区聊天会话（会话不存在或成员不在会话中则跳过）
            var session = await sessionRepository.GetByCircleIdAsync(notification.CircleGuid);
            if (session is not null && session.IsParticipant(notification.UserGuid))
            {
                session.RemoveParticipant(notification.UserGuid);
                await sessionRepository.UpdateAsync(session);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "成员被移出事件处理失败: Circle={CircleGuid}", notification.CircleGuid);
        }
    }
}
