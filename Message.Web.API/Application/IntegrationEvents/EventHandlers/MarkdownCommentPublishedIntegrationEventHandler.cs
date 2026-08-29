namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;

/// <summary>
///     Markdown 评论发布集成事件消费者（顶级评论→文章新评论通知；回复→评论被回复通知）。
///     <para>流程与 MarkdownInteraction 消费者一致：自发布过滤 → 文案组装 → 落库 → 实时推送。</para>
/// </summary>
[EventBusName("MarkdownCommentPublished")]
public class MarkdownCommentPublishedIntegrationEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUserInfoRepository userInfoRepository,
    IUnitOfWork unitOfWork,
    MessageDeliveryService deliveryService,
    ILogger<MarkdownCommentPublishedIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<MarkdownCommentPublishedMessageIntegrationEvent>
{
    public override async Task Handler(MarkdownCommentPublishedMessageIntegrationEvent @event)
    {
        try
        {
            // 1. 自发布过滤：评论者 == 被通知者（博客作者/父评论作者）时不通知
            if (@event.ActorUserId == @event.TargetUserId)
                return;

            // 2. 组装通知文案（昵称经 UserInfo 查询；内容预览来自发布侧截断字段）
            var actor = await userInfoRepository.GetByUserIdAsync(@event.ActorUserId);
            var (type, title, content) = NotificationCopyBuilder.Build(@event, actor?.NickName, @event.CommentContent);

            // 3. 落库（RefType 统一 MarkReview，跳转评论用）
            var notify = TweetNotification.Create(
                @event.TargetUserId, type, title, content,
                refType: "MarkReview", refGuid: @event.ReviewGuid);
            await notificationRepository.AddAsync(notify);
            await unitOfWork.SaveEntitiesAsync();

            // 4. 实时推送（离线静默，读侧补拉）
            await deliveryService.NotifyNotificationAsync(@event.TargetUserId, notify.ToDto());

            logger.LogInformation("Markdown 评论通知已生成：Type={Type}, Target={TargetUserId}, Review={ReviewGuid}",
                type, @event.TargetUserId, @event.ReviewGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Markdown 评论通知消费失败：Review={ReviewGuid}, Markdown={MarkDownGuid}",
                @event.ReviewGuid, @event.MarkDownGuid);
        }
    }
}

/// <summary>
///     Markdown 评论发布集成事件数据副本（字段与 Markdown 服务发布侧一致；跨服务不共享程序集）。
///     routing key = MarkdownCommentPublished（与 Handler 类特性对齐）
/// </summary>
[EventBusName("MarkdownCommentPublished")]
public record MarkdownCommentPublishedMessageIntegrationEvent(
    Guid MarkDownGuid,
    string MarkDownName,
    Guid ReviewGuid,
    Guid? ParentReviewGuid,
    string CommentContent,
    Guid ActorUserId,
    Guid TargetUserId,
    DateTimeOffset OccurredAt) : IntegrationEvent;