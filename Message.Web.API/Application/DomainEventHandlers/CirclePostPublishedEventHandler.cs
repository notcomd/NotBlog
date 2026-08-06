
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 圈子新帖事件处理：实时推送到 circle:{id} 频道 + 发布到社区事件总线（AI/MCP 出口）。
/// </summary>
public class CirclePostPublishedEventHandler(
    ITweetRepository tweetRepository,
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
    ILogger<CirclePostPublishedEventHandler> logger) : INotificationHandler<CirclePostPublishedEvent>
{
    public async Task Handler(CirclePostPublishedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var tweet = await tweetRepository.GetByIdAsync(notification.TweetGuid);
            if (tweet is null)
            {
                logger.LogWarning("圈子新帖事件处理时帖子不存在: {TweetGuid}", notification.TweetGuid);
                return;
            }

            await deliveryService.PushPostPublishedAsync(notification.CircleGuid, tweet.ToCommunityDto());

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.post.published", notification.AuthorGuid,
                notification.CircleGuid, notification.TweetGuid, tweet), cancellationToken);

            logger.LogInformation("圈子新帖已推送: Tweet={TweetGuid}, Circle={CircleGuid}", notification.TweetGuid, notification.CircleGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "圈子新帖事件处理失败: Tweet={TweetGuid}", notification.TweetGuid);
        }
    }
}
