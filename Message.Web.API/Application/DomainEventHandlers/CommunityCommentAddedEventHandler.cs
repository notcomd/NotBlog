
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 圈子帖评论事件处理：评论落在圈子帖上时，实时推送到 circle:{id} 频道 + 事件总线。
/// <para>全局帖评论仍由现有 CommentAddedEventHandler 处理通知，两者互不干扰。</para>
/// </summary>
public class CommunityCommentAddedEventHandler(
    ICommentRepository commentRepository,
    ITweetRepository tweetRepository,
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
    ILogger<CommunityCommentAddedEventHandler> logger) : INotificationHandler<CommentAddedEvent>
{
    public async Task Handler(CommentAddedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var tweet = await tweetRepository.GetByIdAsync(notification.TweetGuid);
            if (tweet is null || tweet.CircleGuid is null)
                return; // 全局帖不在此处理

            var comment = await commentRepository.GetByIdAsync(notification.CommentGuid);
            if (comment is null)
                return;

            await deliveryService.PushCommentAddedAsync(
                tweet.CircleGuid.Value, tweet.TweetGuid, MapToDto(comment));

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.post.commented", notification.UserGuid,
                tweet.CircleGuid, notification.TweetGuid, comment), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "圈子帖评论事件处理失败: Comment={CommentGuid}", notification.CommentGuid);
        }
    }

    private static CommentDto MapToDto(Comment comment) => new()
    {
        CommentGuid = comment.CommentGuid,
        TweetGuid = comment.TweetGuid,
        User = new UserBriefDto { UserGuid = comment.UserGuid, UserName = comment.UserGuid.ToString("N")[..8] },
        ParentGuid = comment.ParentGuid,
        ReplyToGuid = comment.ReplyToGuid,
        Content = comment.Content,
        LikeCount = comment.LikeCount,
        ReplyCount = comment.ReplyCount,
        IsDeleted = comment.IsDeleted,
        CreateTime = comment.CreateTime
    };
}
