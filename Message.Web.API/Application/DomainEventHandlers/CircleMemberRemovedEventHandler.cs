
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 成员被移出圈子事件处理：实时推送 circle:{id} 频道 + 事件总线。
/// </summary>
public class CircleMemberRemovedEventHandler(
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "成员被移出事件处理失败: Circle={CircleGuid}", notification.CircleGuid);
        }
    }
}
