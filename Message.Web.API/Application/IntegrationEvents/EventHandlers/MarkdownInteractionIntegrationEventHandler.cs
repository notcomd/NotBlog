namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;

/// <summary>
///     Markdown 交互集成事件消费者（点赞/投币/评论点赞/评论踩的站内作者通知）。
///     <para>
///     职责：自互动过滤（操作者==被通知者跳过）→ 文案组装（昵称查 UserInfo）→ 落库
///     TweetNotification → SignalR 实时推送（在线即达；离线经 /api/notifications 补拉）。
///     失败仅记日志，不重投（与既有消费者一致）。
///     </para>
/// </summary>
[EventBusName("MarkdownInteraction")]
public class MarkdownInteractionIntegrationEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUserInfoRepository userInfoRepository,
    IUnitOfWork unitOfWork,
    MessageDeliveryService deliveryService,
    ILogger<MarkdownInteractionIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<MarkdownInteractionMessageIntegrationEvent>
{
    public override async Task Handler(MarkdownInteractionMessageIntegrationEvent @event)
    {
        try
        {
            // 1. 自互动过滤：操作者 == 被通知作者时不通知
            if (@event.ActorUserId == @event.TargetUserId)
                return;

            // 2. 组装通知文案（昵称经 UserInfo 查询）
            var actor = await userInfoRepository.GetByUserIdAsync(@event.ActorUserId);
            var (type, title, content) = NotificationCopyBuilder.Build(@event, actor?.NickName);

            // 3. 落库（复用通用站内通知实体）
            var notify = TweetNotification.Create(
                @event.TargetUserId, type, title, content,
                refType: @event.ReviewGuid is null ? "Markdown" : "MarkReview",
                refGuid: @event.ReviewGuid ?? @event.MarkDownGuid);
            await notificationRepository.AddAsync(notify);
            await unitOfWork.SaveEntitiesAsync();

            // 4. 实时推送（离线静默，读侧补拉）
            await deliveryService.NotifyNotificationAsync(@event.TargetUserId, notify.ToDto());

            logger.LogInformation("Markdown 交互通知已生成：Type={Type}, Target={TargetUserId}, Markdown={MarkDownGuid}",
                @event.InteractionType, @event.TargetUserId, @event.MarkDownGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Markdown 交互通知消费失败：Type={Type}, Markdown={MarkDownGuid}",
                @event.InteractionType, @event.MarkDownGuid);
        }
    }
}

/// <summary>
///     Markdown 交互集成事件数据副本（字段与 Markdown 服务发布侧一致；跨服务不共享程序集）。
///     routing key = MarkdownInteraction（与 Handler 类特性对齐）
/// </summary>
[EventBusName("MarkdownInteraction")]
public record MarkdownInteractionMessageIntegrationEvent(
    string InteractionType,
    Guid MarkDownGuid,
    string MarkDownName,
    Guid? ReviewGuid,
    Guid ActorUserId,
    Guid TargetUserId,
    long Amount,
    DateTimeOffset OccurredAt) : IntegrationEvent;