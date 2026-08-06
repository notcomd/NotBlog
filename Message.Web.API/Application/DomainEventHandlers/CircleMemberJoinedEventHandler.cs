
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 成员加入圈子事件处理：实时推送 circle:{id} 频道 + 事件总线。
/// </summary>
public class CircleMemberJoinedEventHandler(
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
    ILogger<CircleMemberJoinedEventHandler> logger) : INotificationHandler<CircleMemberJoinedEvent>
{
    public async Task Handler(CircleMemberJoinedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await deliveryService.PushMemberJoinedAsync(notification.CircleGuid, notification.UserGuid, notification.Role.ToString());

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.member.joined", notification.UserGuid,
                notification.CircleGuid, null, notification), cancellationToken);

            logger.LogInformation("成员加入圈子已推送: Circle={CircleGuid}, User={UserGuid}", notification.CircleGuid, notification.UserGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "成员加入圈子事件处理失败: Circle={CircleGuid}", notification.CircleGuid);
        }
    }
}
