
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 圈子帖互动事件处理：点赞/收藏发生在圈子帖上时，实时推送到 circle:{id} 频道 + 事件总线。
/// </summary>
public class CommunityTweetInteractionEventHandler(
    ITweetRepository tweetRepository,
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
    ILogger<CommunityTweetInteractionEventHandler> logger) : INotificationHandler<TweetInteractionEvent>
{
    public async Task Handler(TweetInteractionEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var tweet = await tweetRepository.GetByIdAsync(notification.TweetGuid);
            if (tweet is null || tweet.CircleGuid is null)
                return; // 全局帖不在此处理

            var circleGuid = tweet.CircleGuid.Value;
            if (notification.IsAdd)
            {
                if (notification.InteractionType == InteractionType.Like)
                    await deliveryService.PushPostLikedAsync(circleGuid, tweet.TweetGuid, notification.UserGuid, tweet.LikeCount);
                else if (notification.InteractionType == InteractionType.Favorite)
                    await deliveryService.PushPostFavoritedAsync(circleGuid, tweet.TweetGuid, notification.UserGuid, tweet.FavoriteCount);
            }

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                $"circle.post.{(notification.InteractionType.ToString().ToLowerInvariant())}",
                notification.UserGuid, circleGuid, notification.TweetGuid, notification), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "圈子帖互动事件处理失败: Tweet={TweetGuid}", notification.TweetGuid);
        }
    }
}
