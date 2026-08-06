
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 成员退出圈子事件处理：实时推送 circle:{id} 频道 + 事件总线。
/// </summary>
public class CircleMemberLeftEventHandler(
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
    ILogger<CircleMemberLeftEventHandler> logger) : INotificationHandler<CircleMemberLeftEvent>
{
    public async Task Handler(CircleMemberLeftEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await deliveryService.PushMemberLeftAsync(notification.CircleGuid, notification.UserGuid);

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.member.left", notification.UserGuid,
                notification.CircleGuid, null, notification), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "成员退出圈子事件处理失败: Circle={CircleGuid}", notification.CircleGuid);
        }
    }
}
