
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 关注事件处理：给被关注者生成新粉丝通知 + 发布事件总线。
/// </summary>
public class UserFollowedEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ICommunityEventPublisher eventPublisher,
    ILogger<UserFollowedEventHandler> logger) : INotificationHandler<UserFollowedEvent>
{
    public async Task Handler(UserFollowedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var notify = TweetNotification.Create(
                notification.FolloweeGuid,
                NotificationType.NewFollower,
                "新粉丝",
                "有人关注了你",
                "User",
                notification.FollowerGuid);

            await notificationRepository.AddAsync(notify);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "user.followed", notification.FollowerGuid,
                null, notification.FolloweeGuid, notification), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "关注事件处理失败: Follower={FollowerGuid}, Followee={FolloweeGuid}",
                notification.FollowerGuid, notification.FolloweeGuid);
        }
    }
}
