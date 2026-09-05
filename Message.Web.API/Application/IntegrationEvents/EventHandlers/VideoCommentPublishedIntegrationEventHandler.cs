namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;

/// <summary>
///     Video 评论发布集成事件消费者（顶级评论→视频新评论通知；回复→评论被回复通知）。
///     <para>流程与 VideoInteraction 消费者一致：自互动过滤 → 文案组装 → 落库 → 实时推送。</para>
/// </summary>
[EventBusName("VideoCommentPublished")]
public class VideoCommentPublishedIntegrationEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUserInfoRepository userInfoRepository,
    IUnitOfWork unitOfWork,
    MessageDeliveryService deliveryService,
    ILogger<VideoCommentPublishedIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<VideoCommentPublishedMessageIntegrationEvent>
{
    public override async Task Handler(VideoCommentPublishedMessageIntegrationEvent @event)
    {
        try
        {
            // 1. 自互动过滤：评论者 == 被通知者（视频作者/父评论作者）时不通知
            if (@event.ActorUserId == @event.TargetUserId)
                return;

            // 2. 组装通知文案（昵称经 UserInfo 查询；内容预览来自发布侧截断字段）
            var actor = await userInfoRepository.GetByUserIdAsync(@event.ActorUserId);
            var (type, title, content) =
                VideoNotificationCopyBuilder.Build(@event, actor?.NickName, @event.CommentPreview);

            // 3. 落库（refType VideoReview，refGuid 跳转评论）
            var notify = TweetNotification.Create(
                @event.TargetUserId, type, title, content,
                refType: "VideoReview", refGuid: @event.ReviewGuid);
            await notificationRepository.AddAsync(notify);
            await unitOfWork.SaveEntitiesAsync();

            // 4. 实时推送（离线静默，读侧补拉）
            await deliveryService.NotifyNotificationAsync(@event.TargetUserId, notify.ToDto());

            logger.LogInformation("Video 评论通知已生成：Type={Type}, Target={TargetUserId}, Review={ReviewGuid}",
                type, @event.TargetUserId, @event.ReviewGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Video 评论通知消费失败：Review={ReviewGuid}, Video={VideoGuid}",
                @event.ReviewGuid, @event.VideoGuid);
        }
    }
}

/// <summary>
///     Video 评论发布集成事件数据副本（字段与 Video 服务发布侧一致；跨服务不共享程序集）。
///     routing key = VideoCommentPublished（与 Handler 类特性对齐）
/// </summary>
[EventBusName("VideoCommentPublished")]
public record VideoCommentPublishedMessageIntegrationEvent(
    Guid VideoGuid,
    string VideoName,
    Guid ReviewGuid,
    Guid? RootReviewGuid,
    Guid ActorUserId,
    Guid TargetUserId,
    string CommentPreview,
    DateTimeOffset OccurredAt) : IntegrationEvent;