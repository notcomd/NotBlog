
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 邀请创建事件处理：直邀给被邀请人生成通知（TweetNotification）并实时推送提醒；所有邀请类型发布事件总线。
/// </summary>
public class CircleInvitationCreatedEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
    ILogger<CircleInvitationCreatedEventHandler> logger) : INotificationHandler<CircleInvitationCreatedEvent>
{
    public async Task Handler(CircleInvitationCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            // 直邀：给被邀请人发通知 + 实时推送
            if (notification.Type == CircleInvitationType.Direct && notification.InviteeGuid.HasValue)
            {
                var notify = TweetNotification.Create(
                    notification.InviteeGuid.Value,
                    NotificationType.CircleInvited,
                    "圈子邀请",
                    "有人邀请你加入一个圈子",
                    "Circle",
                    notification.CircleGuid);

                await notificationRepository.AddAsync(notify);
                await unitOfWork.SaveEntitiesAsync(cancellationToken);

                await deliveryService.PushInvitedAsync(
                    notification.InviteeGuid.Value, notification.CircleGuid, notification.InviteGuid);
            }

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.invitation.created", notification.InviterGuid,
                notification.CircleGuid, notification.InviteGuid, notification), cancellationToken);

            logger.LogInformation("邀请创建事件已处理: Invite={InviteGuid}, Circle={CircleGuid}",
                notification.InviteGuid, notification.CircleGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "邀请创建事件处理失败: Invite={InviteGuid}", notification.InviteGuid);
        }
    }
}
