namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;

/// <summary>视频交互类型（与 Video 服务发布侧枚举一致；跨服务不共享程序集）</summary>
public enum VideoInteractionType
{
    VideoLiked,
    VideoCoined
}

/// <summary>
///     Video 交互集成事件消费者（视频点赞/投币的站内作者通知）。
///     <para>
///     职责：自互动过滤（操作者==被通知者跳过）→ 文案组装（昵称查 UserInfo）→ 落库
///     TweetNotification → SignalR 实时推送（在线即达；离线经 /api/notifications 补拉）。
///     失败仅记日志，不重投（与既有消费者一致）。
///     </para>
/// </summary>
[EventBusName("VideoInteraction")]
public class VideoInteractionIntegrationEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUserInfoRepository userInfoRepository,
    IUnitOfWork unitOfWork,
    MessageDeliveryService deliveryService,
    ILogger<VideoInteractionIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<VideoInteractionMessageIntegrationEvent>
{
    public override async Task Handler(VideoInteractionMessageIntegrationEvent @event)
    {
        try
        {
            // 1. 自互动过滤：操作者 == 被通知作者时不通知
            if (@event.ActorUserId == @event.TargetUserId)
                return;

            // 2. 组装通知文案（昵称经 UserInfo 查询）
            var actor = await userInfoRepository.GetByUserIdAsync(@event.ActorUserId);
            var (type, title, content) = VideoNotificationCopyBuilder.Build(@event, actor?.NickName);

            // 3. 落库（复用通用站内通知实体，refGuid 跳转视频详情）
            var notify = TweetNotification.Create(
                @event.TargetUserId, type, title, content,
                refType: "Video", refGuid: @event.VideoGuid);
            await notificationRepository.AddAsync(notify);
            await unitOfWork.SaveEntitiesAsync();

            // 4. 实时推送（离线静默，读侧补拉）
            await deliveryService.NotifyNotificationAsync(@event.TargetUserId, notify.ToDto());

            logger.LogInformation("Video 交互通知已生成：Type={Type}, Target={TargetUserId}, Video={VideoGuid}",
                @event.InteractionType, @event.TargetUserId, @event.VideoGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Video 交互通知消费失败：Type={Type}, Video={VideoGuid}",
                @event.InteractionType, @event.VideoGuid);
        }
    }
}

/// <summary>
///     Video 交互集成事件数据副本（字段与 Video 服务发布侧一致；跨服务不共享程序集）。
///     routing key = VideoInteraction（与 Handler 类特性对齐）
/// </summary>
[EventBusName("VideoInteraction")]
public record VideoInteractionMessageIntegrationEvent(
    VideoInteractionType InteractionType,
    Guid VideoGuid,
    string VideoName,
    Guid ActorUserId,
    Guid TargetUserId,
    DateTimeOffset OccurredAt) : IntegrationEvent;