# Video 服务：互动计数补全与作者通知 —— 实施文档

> 版本：v1.0　日期：2026-08-29　状态：待评审
> 范围：Video 服务（视频点赞/投币/收藏/观看计数、评论赞踩 ReviewQuote）＋ 跨服务站内通知（Message 服务推送视频作者/评论作者）
> 关联文档：`Markdown-交互一次化与通知-实施文档.md`（通知链路模板）、`Message-开发文档.md`、`交接文档-2026-08-Markdown重构与通话功能.md`

---

## 1. 背景与目标

Video 服务已具备基础的视频元数据与互动能力，但存在以下缺口：

1. **收藏计数未接入**：`VideoQuote` 已内置 `Stars` 字段，但 `AddVideoToCollectionCommandHandler` / `RemoveVideoFromCollectionCommandHandler` 只维护收藏夹关系，从不修改视频的收藏数，导致「收藏数」恒为 0。
2. **无作者通知**：用户点赞/投币视频、给视频留言、回复评论时，作者没有任何感知渠道。Message 服务已具备通用站内通知体系（`TweetNotification` 表 + `/api/notifications` + SignalR 推送，Markdown 服务已接入），Video 服务**零消费端接线**。
3. **评论点赞/点踩缺少专用数据模型**：评论互动目前复用 `VideoQuote`（upvote/down/ballot/share），语义混乱（评论没有投币/分享），且无独立 `ReviewQuote` 值对象，无法对齐 Markdown 的评论赞踩模型。

本次改造目标：

- **计数补全**：视频收藏（`Stars`）接入增/减；观看（`Watch`）、点赞（`Upvote`）、投币（`Ballot`）维持现状并纳入一致性检查；
- **作者通知（本次核心）**：
  - 视频被**点赞**、被**投币**（仅首次发生）→ 站内通知推送视频作者；
  - 视频收到**新评论** → 通知视频作者；评论收到**回复** → 通知被回复的评论作者；
  - 通道为 **Message 服务站内通知 + SignalR 实时推送**，链路、过滤、降级策略与 Markdown 交互通知完全一致；
- **ReviewQuote**：新增评论专用值对象（Like/Dislike），替换 `VideoReview` 上的 `VideoQuote`，统一评论赞踩语义。

## 2. 需求确认与决策记录

| # | 决策点 | 结论 | 影响 |
|---|--------|------|------|
| D-1 | 视频级「点踩」 | **维持现状**（`Down` 计数仍在，不触发通知） | VideoQuote 不动 |
| D-2 | 收藏计数 | **Add→UpStars / Remove→DownStars**，库存量增减 | 两个 Collection 命令处理器接入视频实体 |
| D-3 | 粉丝收藏是否通知作者 | **不通知**（收藏语义为私域行为，避免骚扰） | 收藏仅计数 |
| D-4 | 视频**多作者**（`Affiliated` 集合）通知谁 | **通知「首个非操作者的作者」**（`Affiliated[0]` 语义即主作者，操作者自己除外） | 事件 `TargetUserId` 单值；多人协作场景后续可扩展为逐作者 |
| D-5 | 「首次」通知去重方案 | Group by 交互：`like` / `coin` 用 **Redis SETNX 长周期去重键**（`video:interact:once:{videoGuid}:{userGuid}:{field}`，TTL 30 天）；**取消操作（unlike）不发通知且不删除去重键** | 无明细表前提下的轻量一次化；Redis 丢失可能重复通知（尽力而为，可接受，见 §11） |
| D-6 | 评论点赞/点踩是否通知 | **仅维护 ReviewQuote 数据，不发布通知**（用户需求明确为「数据」）；与 Markdown 对齐的通知（ReviewLiked/Disliked）列为后续增强，事件字段已预留 | 本期少 2 类通知 |
| D-7 | ReviewQuote 与既有 `VideoReview.VideoQuote` | **替换**：`VideoReview` 挂载新 `ReviewQuote`；`QuoteVideoReviewCommandHandler` 仅保留 `upvote/down`（映射 Like/Dislike），移除 ballot/share | 评论响应 DTO、互动查询、缓存字段同步改名 |
| D-8 | 评论发布通知触发 | **每次新评论都发**（评论本身无一次化约束），仅自互动跳过；顶级评论→视频作者，回复→被回复评论作者 | `AddVideoReviewCommandHandler` 发布事件 |
| D-9 | 事件发布可靠性 | **尽力而为**：沿用 Markdown 的 `PublishSafelyAsync` 模式，总线故障仅记日志，不阻塞互动主流程 | Video 侧新增 EventPublishing 帮助类 |
| D-10 | 通知文案构建 | **新增独立 `VideoNotificationCopyBuilder`**（不污染 Markdown 的 `NotificationCopyBuilder`，遵循单一职责） | Message 侧新增文件 |

## 3. 现状分析（关键代码坐标）

| 能力 | 位置 | 说明 |
|------|------|------|
| 视频计数值对象 | [VideoQuote](file:///f:/NotBlog/Video.Domain/ValueObjects/VideoQuote.cs) | 已含 `Upvote/Stars/Watch/Down/Ballot/Share` 原子计数，**Stars 无写入口** |
| 视频点赞/投币/分享 | [LikeVideoCommandHandler](file:///f:/NotBlog/Video.Web.API/Application/Commands/LikeVideoCommandHandler.cs) | `upvote/down/ballot/share` 已落库+失效缓存；**无条件限制，可无限增减，无事件** |
| 观看计数 | [VideoStreamEndpoints.StreamVideoAsync](file:///f:/NotBlog/Video.Web.API/Apis/VideoStreamEndpoints.cs#L86) / [RecordVideoWatchCommandHandler](file:///f:/NotBlog/Video.Web.API/Application/Commands/RecordVideoWatchCommandHandler.cs#L89) | 已 `UpWatch()`，S-18 用 `video:watch-window` SetNX 5 分钟去重 |
| 收藏关系 | [AddVideoToCollectionCommandHandler](file:///f:/NotBlog/Video.Web.API/Application/Commands/AddVideoToCollectionCommandHandler.cs) / Remove 同文件 | **不触碰 `VideoQuote.Stars`** |
| 评论互动 | [QuoteVideoReviewCommandHandler](file:///f:/NotBlog/Video.Web.API/Application/Commands/QuoteVideoReviewCommandHandler.cs) | 复用 `VideoQuote` 做 upvote/down/ballot/share，**无 ReviewQuote、无事件** |
| 评论创建 | [AddVideoReviewCommandHandler](file:///f:/NotBlog/Video.Web.API/Application/Commands/AddVideoReviewCommandHandler.cs) | 顶级/回复评论统一创建，**无事件** |
| 评论列表/互动查询 | [VideoReviewEndpoints](file:///f:/NotBlog/Video.Web.API/Apis/VideoReviewEndpoints.cs#L154-L160) | 响应读 `VideoQuote.Upvote/Stars/Watch`，替换后需同步 |
| 视频缓存键 | [VideoCacheKeys](file:///f:/NotBlog/Video.Domain/Cache/VideoCacheKeys.cs#L63-L80) | **`video:review-quote:{guid}` 键已预留**（ReviewQuoteTtl=2h） |
| 缓存服务契约 | [IVideoCacheService](file:///f:/NotBlog/Video.Domain/Cache/IVideoCacheService.cs#L63-L70) | `IncrementReviewQuoteFieldAsync / GetReviewQuoteFieldsAsync / SetReviewQuoteAsync` **均已预留** |
| 既有集成事件 | [VideoPublishedIntegrationEvent](file:///f:/NotBlog/Video.Web.API/Application/IntegrationEvents/Events/VideoPublishedIntegrationEvent.cs) | 仅视频发布事件，**Message 侧无消费者** |
| 视频事件发布 | [VideoPublishedDomainEventHandler](file:///f:/NotBlog/Video.Web.API/Application/DomainEvents/VideoPublishedDomainEventHandler.cs) | 直连 `PublishAsync`，无安全包装，发放时统一走新帮助类 |
| 通知实体 | [TweetNotification](file:///f:/NotBlog/Message.Domain/Entities/Tweet/TweetNotification.cs) | 通用站内通知，`Create(targetUserId, type, title, content, refType, refGuid)`，**无需改动** |
| 通知类型枚举 | [NotificationType](file:///f:/NotBlog/Message.Domain/Enums/NotificationType.cs) | 已有 Markdown 系列，**需追加 Video 系列 4 类** |
| 消费端模板 | [MarkdownInteractionIntegrationEventHandler](file:///f:/NotBlog/Message.Web.API/Application/IntegrationEvents/EventHandlers/MarkdownInteractionIntegrationEventHandler.cs) | 过滤→文案→落库→SignalR 推送的完整范式，Video 消费者照此实现 |
| 文案构建 | [NotificationCopyBuilder](file:///f:/NotBlog/Message.Web.API/Application/IntegrationEvents/EventHandlers/NotificationCopyBuilder.cs) | Markdown 专用，Video 另建 |
| 实时推送 | MessageDeliveryService.NotifyNotificationAsync | 在线即达，离线经 `/api/notifications` 补拉，**无改动** |
| 消费者注册 | [Program.cs](file:///f:/NotBlog/Message.Web.API/Program.cs)（`AddEventBus(…, Assembly)` 自动扫描 `[EventBusName]`） | 新 Handler 无需手动注册 |

## 4. 总体设计

### 4.1 计数接入矩阵

| 目标 | 计数 | 当前 | 本次动作 | 触发入口 |
|------|------|------|----------|----------|
| 视频 | 点赞 Upvote | 已接入 | 维持 + 首次发通知 | LikeVideoCommandHandler |
| 视频 | 投币 Ballot | 已接入 | 维持 + 首次发通知 | LikeVideoCommandHandler |
| 视频 | **收藏 Stars** | **未接入** | **新增：入夹 +1 / 出夹 −1** | Add/RemoveVideoToCollectionCommandHandler |
| 视频 | 观看 Watch | 已接入 | 维持（SetNX 5min 去重） | StreamVideo / RecordVideoWatch |
| 视频 | 点踩 Down / 分享 Share | 已接入 | 维持，**不通知** | LikeVideoCommandHandler |
| 评论 | **点赞 Like** | 复用 VideoQuote | **改走 ReviewQuote.Like** | QuoteVideoReviewCommandHandler (`upvote`) |
| 评论 | **点踩 Dislike** | 复用 VideoQuote | **改走 ReviewQuote.Dislike** | QuoteVideoReviewCommandHandler (`down`) |

### 4.2 通知事件流（时序）

```
客户端               Video.Web.API                Redis(去重)      RabbitMQ              Message.Web.API               作者客户端
  │  POST like/coin  │                                                                    │                            │
  ├────────────────► │ STEP1: 计数落库 + 缓存失效                                          │                            │
  │                  │ STEP2: 仅首次? SETNX(video:interact:once…) 成功 ──►                │                            │
  │                  │            PublishSafelyAsync(VideoInteractionEvent) ◄───────────  │                            │
  │                  ◄─ 计数结果 ──                                                        │ 消费：自互动过滤          │
  │                  │                                                                    │ 文案组装（昵称）          │
  │  POST review     │                                                                    │ 写 TweetNotification      │
  ├────────────────► │ 评论落库                                                           │ SignalR PushNotification ◄── 在线即时推送 │
  │                  │ PublishSafelyAsync(VideoCommentPublishedEvent) ◄─────────────────► │ (离线：/api/notifications 补拉) │
```

设计要点（与 Markdown 通知链路逐条对齐）：

- **异步集成事件投递**（RabbitMQ），发布失败仅记日志（`PublishSafelyAsync`），不阻塞互动主流程；
- **只发「首次」通知**：点赞/投币由 Redis 去重键判定，重复操作（幂等分支）不发；取消操作（unlike）不发通知；
- **自互动过滤**：操作者 == 被通知作者时跳过（`ActorUserId == TargetUserId`）；
- **评论发布即通知**：评论无一次化约束，每次发布均通知，仅跳过自互动；顶级评论通知视频作者，回复通知被回复的评论作者；
- **事件内直接携带 `TargetUserId`**，Message 服务不反向查询 Video 域数据。

### 4.3 通知触发规则汇总

| 动作 | 通知？ | 通知对象 | 判定 |
|------|--------|----------|------|
| 视频点赞（首次） | ✅ | 视频作者 | SETNX 去重键成功 |
| 视频投币（首次） | ✅ | 视频作者 | SETNX 去重键成功 |
| 视频点赞/投币（重复/取消） | ❌ | — | 去重键已存在或 unlike |
| 视频点踩/分享/收藏/观看 | ❌ | — | 不在此次通知范围 |
| 顶级评论 | ✅ | 视频作者 | 评论者 != 作者 |
| 回复评论 | ✅ | 被回复评论作者 | 评论者 != 被回复者 |

### 4.4 缓存与去重键设计

| Key | 类型 | TTL | 用途 |
|-----|------|-----|------|
| `video:quote:{videoGuid}` | Hash | 2h | 视频计数（预留，本期读侧可不动） |
| `video:review-quote:{reviewGuid}` | Hash | 2h | **评论赞/踩增量（本期接入）** |
| `video:interact:once:{videoGuid}:{userGuid}:{field}` | String(SetNX) | **30 天** | **本次新增**：互动「首次」去重 |
| `video:watch-window:{videoGuid}:{userGuid}` | String(SetNX) | 5min | 观看去重（现状） |

> 去重键 TTL 折中：30 天内重复互动不重复通知；超过 30 天视为新互动可再次通知（单条通知可容忍）。Redis 故障/清库导致的重复通知属尽力而为模型的已知局限。

## 5. 消息契约（新集成事件）

> 跨服务不共享程序集：Video 服务发布侧放定义，Message 服务消费侧放**同名字段的数据副本**（`routing key = EventBusName`），模式与 Markdown 事件一致。

### 5.1 VideoInteraction（视频点赞/投币，仅首次）

```csharp
// Video.Web.API/Application/IntegrationEvents/VideoInteractionIntegrationEvent.cs
public enum VideoInteractionType { VideoLiked, VideoCoined }

/// <summary>视频交互集成事件（点赞/投币；仅首次发生发布）</summary>
[EventBusName("VideoInteraction")]
public record VideoInteractionIntegrationEvent(
    VideoInteractionType InteractionType,   // VideoLiked / VideoCoined
    Guid VideoGuid,
    string VideoName,
    Guid ActorUserId,                        // 操作者（服务端解析，不可信入参已剔除）
    Guid TargetUserId,                       // 通知对象：视频作者（Affiliated 首个非操作者）
    DateTimeOffset OccurredAt) : IntegrationEvent;
```

### 5.2 VideoCommentPublished（评论/回复评论发布）

```csharp
// Video.Web.API/Application/IntegrationEvents/VideoCommentPublishedIntegrationEvent.cs
/// <summary>视频评论发布集成事件（顶级评论→视频作者；回复→被回复评论作者）</summary>
[EventBusName("VideoCommentPublished")]
public record VideoCommentPublishedIntegrationEvent(
    Guid VideoGuid,
    string VideoName,
    Guid ReviewGuid,
    Guid? RootReviewGuid,        // null = 顶级评论；非 null = 回复（通知被回复评论作者）
    Guid ActorUserId,            // 评论者
    Guid TargetUserId,           // 通知对象：视频作者或被回复评论作者
    string CommentPreview,       // 正文前 50 字预览，空则省略
    DateTimeOffset OccurredAt) : IntegrationEvent;
```

字段说明：
- `RootReviewGuid == null` → `TargetUserId = 视频主作者（Affiliated[0] 非操作者）`；
- `RootReviewGuid != null` → `TargetUserId = 被回复评论的 UserGuid`；
- `CommentPreview` 由发布侧从 `review.VideoReviewBody` 截断生成（`StringHelper.Truncate(body, 50)`），避免消费端反查评论正文。

## 6. Video 侧详细设计

### 6.1 收藏计数接入（D-2）

`AddVideoToCollectionCommandHandler` / `RemoveVideoFromCollectionCommandHandler` 增加步骤：

```csharp
// 在「收藏关系变更」被持久化前后，加载视频实体并调整收藏计数
var video = await videoRepository.FindByVideoAsync(request.VideoGuid); // 幂等/不存在处理沿用
if (isAdd) video.VideoQuote.UpStars();   // 加入收藏夹 +1
else       video.VideoQuote.DownStars(); // 移出收藏夹 −1
await videoRepository.UpdateByQuoteAsync(video.VideoGuid, video.VideoQuote);
```

要点：
- `FindByVideoAsync` 不存在时按集合仓库现行为准（抛异常→返回失败或跳过计数，二者择一并记录日志）；
- **同一视频多次添加不重复计数**（现有 `collection.VideoGuid.Contains` 幂等分支先行返回，天然满足）；
- 收藏不触发通知（D-3）。

### 6.2 点赞/投币「首次」通知（D-4、D-5）

`LikeVideoCommandHandler` 增加 `IRedisCacheService` 依赖（经 `ICacheMemory` 已注册），在计数分支成功后：

```csharp
// only first-time interactions (like / coin) publish a notification
if (request.IsLike && normalized is "upvote" or "ballot")
{
    var dedupKey = VideoCacheKeys.VideoInteractionOnce(request.VideoGuid, request.UserGuid, normalized);
    var firstTime = await redis.SetIfNotExistsAsync(dedupKey, "1",
        expiry: VideoCacheKeys.VideoInteractionOnceTtl);   // 30 天
    if (firstTime)
    {
        var target = video.Affiliated?.FirstOrDefault(id => id != request.UserGuid) ?? Guid.Empty;
        if (target != Guid.Empty)
            await EventPublishing.PublishSafelyAsync(bus, new VideoInteractionIntegrationEvent(
                normalized == "upvote" ? VideoInteractionType.VideoLiked : VideoInteractionType.VideoCoined,
                video.VideoGuid, video.VideoName, request.UserGuid, target, DateTimeOffset.UtcNow));
    }
}
```

要点：
- `VideoInteractionOnce` 键见 §4.4；`SetIfNotExistsAsync` 即 SETNX 语义；
- unlike 分支**不删键**（已通知过即不再通知）；
- `VideoName` 用于「{昵称} 赞了您的视频《{名}》」文案；
- 新增 `VideoCacheKeys.VideoInteractionOnce(guid, userId, field)` + `VideoInteractionOnceTtl`。

### 6.3 ReviewQuote 值对象与评论赞/踩（D-6、D-7）

```csharp
// Video.Domain/ValueObjects/ReviewQuote.cs
/// <summary>评论互动计数（点赞/点踩）— 专用于 VideoReview，语义与视频 VideoQuote 分离</summary>
public record ReviewQuote
{
    private long _like;
    private long _dislike;
    public long Like    => _like;
    public long Dislike => _dislike;
    private ReviewQuote() { }
    public static ReviewQuote ReviewQuoteBuilder() => new();
    public void UpLike()    => Interlocked.Increment(ref _like);
    public void UpDislike() => Interlocked.Increment(ref _dislike);
    public void DownLike()    => AtomicDecrement(ref _like);
    public void DownDislike() => AtomicDecrement(ref _dislike);
    private static void AtomicDecrement(ref long field)
    {
        long current = field;
        while (current > 0 && Interlocked.CompareExchange(ref field, current - 1, current) != current)
            current = field;
    }
}
```

改动坐标：
1. `VideoReview`：`VideoQuote VideoQuote` → `ReviewQuote Quote`（构造器 `ReviewQuote.ReviewQuoteBuilder()`）；`AddByVideoReview` 不变；
2. `QuoteVideoReviewCommandHandler`：`request.IsLike` 分支映射 `"upvote"→UpLike/DownLike`、`"down"→UpDislike/DownDislike`，`validFields` 收敛为 `{ "upvote", "down" }`；
3. 缓存接入：`cacheService.IncrementReviewQuoteFieldAsync(reviewGuid, "like"|"dislike", ±1)`（键与字段沿用 `ReviewQuote` 预留设计）；
4. 查询侧：`VideoReviewEndpoints` 两处读取 `r.VideoQuote.Upvote/Stars/Watch`（列表）与 `interaction` 对象（详情）改为 `r.Quote.Like/Dislike`；
5. `VideoReviewResponse` DTO（[VideoReviewResponse.cs](file:///f:/NotBlog/Video.Web.API/Dto/Response/VideoReviewResponse.cs)）：字段由 `Upvote/Stars/Watch` 调整为 `Like/Dislike`，构造参数顺序与两处 `Select` 调用（[VideoReviewEndpoints.cs](file:///f:/NotBlog/Video.Web.API/Apis/VideoReviewEndpoints.cs#L154-L160)）同步修改。
6. `VideoDbContext` 映射：`ReviewQuote` 作为 owned 值对象映射（字段 `Quote_Like` / `Quote_Dislike`，命名策略对照既有 `ReviewContent` 的 OwnsOne 配置）。

### 6.4 评论/回复评论发布通知（D-8）

`AddVideoReviewCommandHandler` 在 `SaveEntitiesAsync` 成功后组装并安全发布：

```csharp
await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

// 通知：顶级评论→视频作者；回复→被回复评论作者（消费端做自互动过滤）
await EventPublishing.PublishSafelyAsync(bus, new VideoCommentPublishedIntegrationEvent(
    VideoGuid: request.VideoGuid,
    VideoName: video.VideoName,
    ReviewGuid: newReviewGuid,             // 新建评论的 VideoReviewGuid
    RootReviewGuid: request.RootReview,    // null=顶级
    ActorUserId: request.UserGuid,
    TargetUserId: request.RootReview is { } rootId && rootId != Guid.Empty
        ? video.VideoReviews!.First(r => r.VideoReviewGuid == rootId).UserGuid
        : video.Affiliated?.FirstOrDefault() ?? Guid.Empty,
    CommentPreview: Truncate(request.Body, 50),
    OccurredAt: DateTimeOffset.UtcNow));
```

要点：
- `VideoReview` 构造时已 `Guid.CreateVersion7()` 生成 `VideoReviewGuid`；为拿到新建评论的 Guid，可在 `video.AddByVideoReview` 后读取 `video.VideoReviews!.Last()`（或命令传入 `VideoReviewGuid` 由构造器覆写——**推荐**：给 `AddByVideoReview` 增加可选 `reviewGuid` 参数，避免 Last() 时序依赖）；
- `TargetUserId == Guid.Empty` 时跳过发布（无作者场景）；
- 消费者侧过滤 `ActorUserId == TargetUserId`（与 Markdown 一致）。

### 6.5 安全发布（D-9）

新建 `EventPublishing`（对齐 Markdown 同名列的能力）：

```csharp
// Video.Web.API/Application/IntegrationEvents/EventPublishing.cs
/// <summary>集成事件安全发布：总线故障仅记日志，不拖垮业务主流程</summary>
public static class EventPublishing
{
    public static async Task PublishSafelyAsync(IEventBus bus, IntegrationEvent @event,
        ILogger? logger = null)
    {
        try { await bus.PublishAsync(@event); }
        catch (Exception ex) { logger?.LogError(ex, "Event publish failed: {Event}", @event.GetType().Name); }
    }
}
```

既有 `VideoPublishedDomainEventHandler` 的直连 `PublishAsync` 一并切换为安全发布，保持全站一致。

### 6.6 Video 侧新增文件清单

| 文件 | 动作 |
|------|------|
| `Video.Domain/ValueObjects/ReviewQuote.cs` | 新增 |
| `Video.Domain/Cache/VideoCacheKeys.cs` | 追加 `VideoInteractionOnce(key+TTL)`、`ReviewQuoteFields(like/dislike)` 常量 |
| `Video.Web.API/Application/IntegrationEvents/EventPublishing.cs` | 新增 |
| `Video.Web.API/Application/IntegrationEvents/VideoInteractionIntegrationEvent.cs` | 新增 |
| `Video.Web.API/Application/IntegrationEvents/VideoCommentPublishedIntegrationEvent.cs` | 新增 |
| `Video.Domain/Entities/VideoReview.cs` | 修改（挂 ReviewQuote） |
| `Video.Web.API/Application/Commands/QuoteVideoReviewCommandHandler.cs` | 修改（upvote/down → Like/Dislike + 缓存） |
| `Video.Web.API/Application/Commands/LikeVideoCommandHandler.cs` | 修改（首次去重 + 通知发布） |
| `Video.Web.API/Application/Commands/AddVideoToCollectionCommandHandler.cs` / Remove | 修改（Stars 增减） |
| `Video.Web.API/Application/Commands/AddVideoReviewCommandHandler.cs` | 修改（评论发布通知） |
| `Video.Web.API/Application/DomainEvents/VideoPublishedDomainEventHandler.cs` | 修改（切 PublishSafelyAsync） |
| `Video.Web.API/Apis/VideoReviewEndpoints.cs` + `Dto/Response/VideoReviewResponse.cs` | 修改（Quote 字段） |

## 7. Message 侧详细设计

### 7.1 NotificationType 扩展

追加 4 类：

```csharp
// Message.Domain/Enums/NotificationType.cs（追加于 Markdown 系列之后）
// ===== Video 服务作者通知（互动计数与作者通知 v1.0 实施文档） =====
VideoLiked,          // 视频被点赞
VideoCoined,         // 视频被投币
VideoCommentAdded,   // 视频收到新评论（顶级评论）
VideoCommentReplied, // 评论被回复
```

### 7.2 消费者（两个 Handler，模式照抄 Markdown）

```csharp
// Message.Web.API/Application/IntegrationEvents/EventHandlers/VideoInteractionIntegrationEventHandler.cs
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
            if (@event.ActorUserId == @event.TargetUserId) return;            // 1. 自互动过滤

            var actor = await userInfoRepository.GetByUserIdAsync(@event.ActorUserId);
            var (type, title, content) =
                VideoNotificationCopyBuilder.Build(@event, actor?.NickName);   // 2. 文案

            var notify = TweetNotification.Create(
                @event.TargetUserId, type, title, content,
                refType: "Video", refGuid: @event.VideoGuid);                  // 3. 落库
            await notificationRepository.AddAsync(notify);
            await unitOfWork.SaveEntitiesAsync();

            await deliveryService.NotifyNotificationAsync(@event.TargetUserId, notify.ToDto()); // 4. 推送

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

/// <summary>消费侧数据副本（字段与 Video 服务发布侧一致）；routing key = VideoInteraction</summary>
[EventBusName("VideoInteraction")]
public record VideoInteractionMessageIntegrationEvent(
    VideoInteractionType InteractionType,
    Guid VideoGuid,
    string VideoName,
    Guid ActorUserId,
    Guid TargetUserId,
    DateTimeOffset OccurredAt) : IntegrationEvent;
```

`VideoCommentPublishedIntegrationEventHandler` 结构相同，`refType: "VideoReview"`、`refGuid: ReviewGuid`，文案区分顶级/回复。

### 7.3 文案构建（D-10）

```csharp
// Message.Web.API/Application/IntegrationEvents/EventHandlers/VideoNotificationCopyBuilder.cs
/// <summary>Video 服务通知文案构建（事件 → 通知类型 + 标题 + 内容）</summary>
public static class VideoNotificationCopyBuilder
{
    public static (NotificationType Type, string Title, string Content) Build(
        VideoInteractionMessageIntegrationEvent e, string? actorNickname)
    {
        var nick = Nick(actorNickname);
        return e.InteractionType switch
        {
            VideoInteractionType.VideoLiked => (NotificationType.VideoLiked, "视频被点赞",
                $"{nick} 赞了您的视频《{e.VideoName}》"),
            _ => (NotificationType.VideoCoined, "收到投币",
                $"{nick} 为您的视频《{e.VideoName}》投了币")
        };
    }

    public static (NotificationType Type, string Title, string Content) Build(
        VideoCommentPublishedMessageIntegrationEvent e, string? actorNickname, string? commentPreview)
    {
        var nick = Nick(actorNickname);
        var preview = string.IsNullOrWhiteSpace(commentPreview) ? "" : $"：{commentPreview}";
        return e.RootReviewGuid is null
            ? (NotificationType.VideoCommentAdded, "视频收到新评论",
                $"{nick} 评论了您的视频《{e.VideoName}》{preview}")
            : (NotificationType.VideoCommentReplied, "评论被回复",
                $"{nick} 回复了您在《{e.VideoName}》下的评论{preview}");
    }

    private static string Nick(string? nickname)
        => string.IsNullOrWhiteSpace(nickname) ? "一位用户" : nickname;
}
```

### 7.4 Message 侧新增/修改文件清单

| 文件 | 动作 |
|------|------|
| `Message.Domain/Enums/NotificationType.cs` | 追加 4 枚举 |
| `Message.Web.API/Application/IntegrationEvents/EventHandlers/VideoInteractionIntegrationEventHandler.cs` | 新增（含数据副本 record） |
| `Message.Web.API/Application/IntegrationEvents/EventHandlers/VideoCommentPublishedIntegrationEventHandler.cs` | 新增（含数据副本 record） |
| `Message.Web.API/Application/IntegrationEvents/EventHandlers/VideoNotificationCopyBuilder.cs` | 新增 |
| `Message.Web.API/Application/IntegrationEvents/EventHandlers/`（`VideoInteractionType` 枚举定义放此处或独立 Enums 文件） | 新增 |

注册：`Program.cs` 的 `AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly())` 自动扫描 `[EventBusName]` 并绑定 routing key，**无需手动注册**。

## 8. 接口 / DTO 变更

| 接口/端点 | 变更 |
|-----------|------|
| `POST /api/videoreview/{reviewGuid}/like` | 行为同前（`Field: upvote/down`），计数落到 ReviewQuote；返回体 `NewCount` 语义为 Like/Dislike 计数 |
| `GET /api/videoreview/{videoGuid}` / `GET /api/videoreview/replies/{reviewGuid}` | 响应 `VideoReviewResponse` 替换为 `Like/Dislike` 字段 |
| `GET /api/videoreview/{reviewGuid}/interaction` | 返回 `VideoReviewGuid, Like, Dislike` |
| `GET /api/video` 系列 | 若列表 DTO 含收藏数则自动生效（Stars 由仓储读取）；否则维持 |
| `/api/notifications` | 无改动（新增类型自动渲染，前端需适配标题文案展示，见 §9） |

## 9. 前端接线（notblog-ui）

与 Markdown 通知一致，仅需在现有通知订阅处确认新类型的展示：

- [notification.ts](file:///f:/NotBlog/notblog-ui/src/stores/notification.ts) 订阅逻辑不变（`PushNotification` 已有）；
- 若按类型渲染 icon/跳转：新增 `VideoLiked/VideoCoined/VideoCommentAdded/VideoCommentReplied` 的文案与跳转（视频详情需带 `VideoGuid` 路由参数——通知 `refGuid` 即 VideoGuid/ReviewGuid）；
- 本期非阻塞项，可在服务端完成后面向联调再补。

## 10. 数据库与迁移

1. **`VideoReview.Quote` 字段替换**：若原有 `VideoQuote` 已作 owned 映射（需审计 `VideoDbContext`），迁移中新增 `Quote_Like`、`Quote_Dislike` 列并回填既有 `VideoQuote_Upvote/Down` 存量；若未映射（纯内存计数），则直接新增 owned 列，无数据搬运；
2. **评论存量计数回填**：以现有 `VideoQuote.Down` 回填 `Quote_Dislike`、`Upvote` 回填 `Quote_Like`（如已映射）；
3. **收藏存量**：无需迁移（Stars 从零开始累计，与缓存一致）；
4. **无新索引**：去重依赖 Redis，不落库。

> EF Core 迁移生成 `Add-Migration VideoInteractionNotifications` 于 Video 服务，执行 `dotnet ef database update`。

## 11. 测试计划

**Video 服务（单测/集成）**
- [VideoQuoteTests]（新建）：Stars 增减、ReviewQuote Like/Dislike 原子增减、负值保护（AtomicDecrement 下限 0）；
- `LikeVideoCommandHandler`：首次 like/coin 发通知、重复 like 不发、unlike 不发、`TargetUserId` 取 Affiliated 首个非操作者、自互动（操作者==作者）不产生事件；
- `AddVideoToCollectionCommandHandler`：加入→Stars+1、移除→Stars−1、重复加入幂等不叠加计数；
- `AddVideoReviewCommandHandler`：顶级评论 TargetUserId=作者、回复评论 TargetUserId=被回复者、预览截断 50 字；
- `QuoteVideoReviewCommandHandler`：upvote→Like / down→Dislike、缓存字段增量正确。

**Message 服务（集成）**
- `VideoInteractionIntegrationEventHandler`：自互动跳过、文案正确、TweetNotification 落库、SignalR 推送被调用（GrpcTestHelper/MessageHubTests 沿用现有测试基建）；
- `VideoCommentPublishedIntegrationEventHandler`：顶级/回复两条路径。

**端到端**
- 用户 B 点赞用户 A 的视频 → A 收到站内通知 + 在线 SignalR 推送；
- 用户 B 回复用户 B 自己评论下的回复场景（自互动）→ 不通知；
- 视频列表/评论列表/互动查询返回新字段不与前端类型冲突（`npm run type-check`）。

## 12. 风险与后续

| 风险/事项 | 说明 | 处置 |
|-----------|------|------|
| 互动「首次」依赖 Redis | Redis 清库/故障会重复通知或漏去重 | 尽力而为可接受（与 Markdown 一致）；后续可升级用户-交互明细表（`VideoUserInteraction(videoGuid,userId,field)` 唯一索引）实现强一次化 |
| `VideoReview.Quote` 字段替换破坏既有前端 | 前端若已读 Upvote/Stars/Watch 字段，联调期需同步改 | 与 notblog-ui 一起提交，避免灰度不一致 |
| 多作者视频只通知主作者 | `Affiliated` 集合语义 | 后续可扩展事件携带 `List<Guid> TargetUserIds` 逐作者通知 |
| 评论点赞/点踩暂无通知 | 用户需求限定为数据 | 预留 `VideoReviewLiked/Disliked` 扩展（事件字段已具备），按需二期接入 |
| 评论预览截断无统一工具 | 发布侧内联实现 | 若多处需要，抽 `StringHelper.Truncate` 至 Commons |

## 13. 验收清单

- [ ] 视频收藏数在加入/移出收藏夹时正确增减并落库；
- [ ] 首次点赞/投币视频 → 作者收到 `VideoLiked/VideoCoined` 站内通知并实时推送；重复与取消不重复通知；
- [ ] 顶级评论 → 视频作者收到 `VideoCommentAdded`；回复 → 被回复评论作者收到 `VideoCommentReplied`；评论者==通知对象跳过；
- [ ] 评论点赞/点踩落 `ReviewQuote`（Like/Dislike），列表/详情/互动接口返回新字段；
- [ ] Video 服务事件发布全部走 `PublishSafelyAsync`（含既有 `VideoPublishedDomainEventHandler`）；
- [ ] solution 构建通过、Video/Message 单测与集成测试通过、notblog-ui `npm run type-check` 通过；
- [ ] 迁移执行成功，存量评论赞踩回填正确。