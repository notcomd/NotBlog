using CacheMemory.Core;

namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;

/// <summary>
///     Markdown 文章发布集成事件消费者：通知作者的**好友与关注者**（Message 关系），并进行时间窗聚合。
///     <para>
///     聚合模式（Redis 可用）：每个接收者一条 Hash 桶（field=发布者+文章，value=文章名|昵称），
///     在 10 分钟窗口内由后台聚合器 <see cref="Message.Web.API.Background.MarkdownPublishAggregationService"/>
///     统一合并为一条通知（如「您的 N 位好友/关注者发布了新文章」），避免轰炸。
///     降级路径（Redis 不可用/本地模式）：立即逐条发送，保证通知不丢（不聚合）。
///     </para>
/// </summary>
[EventBusName("MarkdownCreated")]
public class MarkdownCreatedIntegrationEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUserInfoRepository userInfoRepository,
    IMessageFriendsRepository friendsRepository,
    IUserFollowRepository followRepository,
    IUnitOfWork unitOfWork,
    MessageDeliveryService deliveryService,
    IServiceProvider serviceProvider,
    ILogger<MarkdownCreatedIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<MarkdownCreatedMessageIntegrationEvent>
{
    /// <summary>聚合窗口（与应用层聚合器周期一致：10 分钟）</summary>
    public static readonly TimeSpan AggregateWindow = TimeSpan.FromMinutes(10);

    /// <summary>桶 TTL：略大于 2 个聚合窗口，防聚合器处理前被过期清空导致丢事件</summary>
    public static readonly TimeSpan BucketTtl = TimeSpan.FromMinutes(25);

    /// <summary>聚合桶 key 前缀（+ 接收者 Guid:N）</summary>
    public const string BucketKeyPrefix = "markdown:pub:agg:";

    /// <summary>接收者索引（Set：哪些用户有未处理的聚合桶），供聚合器扫描</summary>
    public const string ReceiversIndexKey = "markdown:pub:agg:receivers";

    public override async Task Handler(MarkdownCreatedMessageIntegrationEvent @event)
    {
        try
        {
            // 1. 接收者 = 好友 ∪ 关注者（并集去重；关系不含作者本人，天然免自通知）
            var friendIds = await friendsRepository.GetFriendIdsAsync(@event.MarkUserGuid);
            var followerIds = await followRepository.GetFollowerIdsAsync(@event.MarkUserGuid);
            var receiverIds = friendIds.Concat(followerIds).Distinct().ToList();
            if (receiverIds.Count == 0)
                return;

            // 2. 发布者昵称（聚合文案用；缺失回退）
            var publisher = await userInfoRepository.GetByUserIdAsync(@event.MarkUserGuid);
            var nick = string.IsNullOrWhiteSpace(publisher?.NickName) ? "一位好友" : publisher.NickName;
            var payload = $"{@event.FileName}|{nick}";

            // 3. 聚合模式：写入 Redis 桶，等待后台聚合器（10 分钟窗）统一发送
            var redis = ResolveRedis();
            if (redis is not null)
            {
                foreach (var receiverId in receiverIds)
                {
                    // field 含发布者+文章 → 同一发布者同一篇只会有一个 field，天然去重
                    var field = $"{@event.MarkUserGuid:N}:{@event.MarkDownGuid:N}";
                    await redis.HashSetAsync(BucketKey(receiverId), field, payload);
                    await redis.KeyExpireAsync(BucketKey(receiverId), BucketTtl); // 每次写入刷新 TTL
                    await redis.SetAddAsync(ReceiversIndexKey, receiverId.ToString("N"));
                }

                logger.LogInformation("Markdown 发布已入聚合桶：文章 {MarkDownGuid}，接收者 {Count} 人（{Window} 分钟窗聚合）",
                    @event.MarkDownGuid, receiverIds.Count, (int)AggregateWindow.TotalMinutes);
                return;
            }

            // 4. 降级路径（Redis 不可用/本地模式）：立即逐条发送，保证通知不丢失（不做聚合）
            await SendIndividuallyAsync(@event, receiverIds, nick);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Markdown 发布通知消费失败：文章 {MarkDownGuid}", @event.MarkDownGuid);
        }
    }

    /// <summary>聚合桶 key（按接收者）</summary>
    public static string BucketKey(Guid receiverId) => $"{BucketKeyPrefix}{receiverId:N}";

    /// <summary>逐条直发（降级路径）：每个接收者一条独立通知 + 实时推送</summary>
    private async Task SendIndividuallyAsync(
        MarkdownCreatedMessageIntegrationEvent @event, List<Guid> receiverIds, string nick)
    {
        var title = "好友/关注者发布了新文章";
        var content = $"{nick} 发布了新文章《{@event.FileName}》";
        var saved = new List<TweetNotification>(receiverIds.Count);
        foreach (var receiverId in receiverIds)
        {
            var notify = TweetNotification.Create(
                receiverId, NotificationType.MarkdownPublished, title, content,
                refType: "Markdown", refGuid: @event.MarkDownGuid);
            await notificationRepository.AddAsync(notify);
            saved.Add(notify);
        }

        await unitOfWork.SaveEntitiesAsync();

        foreach (var notify in saved)
        {
            await deliveryService.NotifyNotificationAsync(notify.UserGuid, notify.ToDto());
        }

        logger.LogInformation("Markdown 发布通知已生成（降级直发，未聚合）：文章 {MarkDownGuid}，接收者 {Count} 人",
            @event.MarkDownGuid, saved.Count);
    }

    /// <summary>解析 Redis 服务（未注册/不可用时返回 null → 走降级路径）</summary>
    private IRedisCacheService? ResolveRedis()
    {
        try
        {
            return serviceProvider.GetService<IRedisCacheService>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis 不可用，Markdown 发布通知降级为逐条直发");
            return null;
        }
    }
}

/// <summary>
///     Markdown 文章发布集成事件数据副本（字段与 Markdown 服务发布侧 MarkdownCreatedIntegrationEvent 一致；跨服务不共享程序集）。
///     routing key = MarkdownCreated（与 Handler 类特性对齐）
/// </summary>
[EventBusName("MarkdownCreated")]
public record MarkdownCreatedMessageIntegrationEvent(
    Guid MarkDownGuid,
    Guid MarkUserGuid,
    string FileName,
    DateTimeOffset CreatedAt) : IntegrationEvent;