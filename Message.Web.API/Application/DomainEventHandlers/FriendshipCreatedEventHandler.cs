
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 好友请求事件处理：为接收者生成「好友请求」站内通知并实时推送。
/// <para>昵称取自 UserInfo 投影，缺失时回退「一位用户」；通知失败仅记日志，
/// 不影响好友关系落库（读侧仍可经 /api/notifications 补拉）。</para>
/// </summary>
public class FriendshipCreatedEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUserInfoRepository userInfoRepository,
    IUnitOfWork unitOfWork,
    MessageDeliveryService deliveryService,
    ILogger<FriendshipCreatedEventHandler> logger) : INotificationHandler<FriendshipCreatedEvent>
{
    public async Task Handler(FriendshipCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var requester = await userInfoRepository.GetByUserIdAsync(notification.UserId);
            var nick = string.IsNullOrWhiteSpace(requester?.NickName) ? "一位用户" : requester.NickName;

            // RefGuid = 请求者 ID，前端可据此直接处理该请求
            var notify = TweetNotification.Create(
                notification.FriendId,
                NotificationType.FriendRequestReceived,
                "好友请求",
                $"{nick} 请求添加您为好友",
                refType: "User",
                refGuid: notification.UserId);

            await notificationRepository.AddAsync(notify);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            await deliveryService.NotifyNotificationAsync(notify.UserGuid, notify.ToDto(), cancellationToken);

            logger.LogInformation("已生成好友请求通知：接收者 {FriendId}，请求者 {UserId}",
                notification.FriendId, notification.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "好友请求通知处理失败：请求者 {UserId}，接收者 {FriendId}",
                notification.UserId, notification.FriendId);
        }
    }
}
