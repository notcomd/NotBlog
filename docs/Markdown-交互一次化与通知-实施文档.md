# Markdown 交互「一次化」与作者通知 —— 实施文档

> 版本：v1.3　日期：2026-08-29　状态：待评审
> 范围：Markdown 服务（点赞/投币/评论互动）一次化约束 + 跨服务站内通知（Message 服务）
> v1.1 变更：新增「发布评论 → 通知博客作者 / 回复评论 → 通知被回复的评论作者」（§5.1.3、§5.2.1、§4.3.1）
> v1.2 变更：新增「发布文章 → 通知作者的好友」（复用已有 MarkdownCreated 事件，Message 侧订阅并批量通知好友，§5.1.4、§5.2.1）
> v1.3 变更：接收范围扩为「好友 ∪ 关注者」；同一接收者 10 分钟窗口内多条发布聚合为一条提醒（Redis 桶 + 后台聚合器，§5.1.4、§5.2.2）
> 关联文档：`Markdown-FileStorage-Redesign.md`、`Message-开发文档.md`、`交接文档-2026-08-Markdown重构与通话功能.md`

---

## 1. 背景与目标

当前 Markdown 文档互动存在两个问题：

1. **投币可重复**：`MarkCoin` 无 `(MarkDownGuid, UserId)` 唯一约束，同一用户可对同一篇博客多次投币，不符合「一个用户对一篇博客投币一次」的交互预期，也放大了热度分的可刷性。
2. **无作者通知**：文档被点赞、被投币、评论被点赞/点踩时，作者没有任何感知渠道。Message 服务已具备通用站内通知体系（`TweetNotification` 表 + `/api/notifications` + SignalR 长连接），但 Markdown 侧仅发布了 `MarkReviewLiked` 等集成事件，**无任何消费者**。

本次改造目标：

- 文档：点赞一次（维持现状）、**投币一次（本次收紧）**；
- 评论：点赞一次、点踩一次（均维持现状）；
- 以上四类「实际发生」的互动均生成**站内通知**推送文档/评论作者，通道为 **Message 服务站内通知 + SignalR 实时推送**；
- **发布评论**时通知博客作者，**回复子评论**时通知被回复的评论作者（本次新增，见 §5.1.3）。

## 2. 需求确认与决策记录

| # | 决策点 | 结论 | 影响 |
|---|--------|------|------|
| D-1 | 文档级「点踩」 | **不新增**（仅评论有点踩） | 不动 MarkDown 模型，`MarkQuote` 不加 Dislike 字段 |
| D-2 | 重复投币处理 | **幂等返回现有硬币总额**，不抛异常 | `MarkCoin` 加唯一约束，仓储幂等化 |
| D-3 | 评论投币 | **不支持**，评论仅点赞/点踩+通知评论作者 | 不新增评论投币实体 |
| D-4 | 通知通道 | **站内通知落库 + SignalR 实时推送** | 复用 `TweetNotification` / `/api/notifications`，扩展 `MessageHub` |
| D-5 | 评论发布通知 | 顶级评论 → 通知博客作者；子评论（回复）→ 通知被回复的评论作者；发布者 = 被通知者则跳过 | 新增 `MarkdownCommentPublished` 事件，Message 侧独立 Handler |
| D-6 | 文章发布→好友/关注者通知 | 创建文章后通知**作者的好友 ∪ 关注者**（并集去重）；同一接收者 10 分钟窗口内多条发布**聚合为一条提醒** | 复用 `MarkdownCreated` 事件；Message 侧 Redis 桶收集 + 后台聚合器（10 分钟窗） |

## 3. 现状分析（关键代码坐标）

| 能力 | 位置 | 说明 |
|------|------|------|
| 文档点赞/取消 | [MarkDownRepository.LikeDocumentAsync / RemoveLikeDocumentAsync](file:///f:/NotBlog/Markdown.Infrastructure/Repository/MarkDownRepository.cs) | 已有 `(MarkDownGuid,UserId)` 唯一约束 + 23505 捕获幂等 |
| 文档投币 | [MarkDownRepository.CoinDocumentAsync](file:///f:/NotBlog/Markdown.Infrastructure/Repository/MarkDownRepository.cs#L767-L787) | **无唯一约束，可重复投** |
| 文档实体计数 | [MarkQuote.CoinSome / AddCoin](file:///f:/NotBlog/Markdown.Domain/ValueObject/MarkQuote.cs) | `CoinSome` 为累计总额，无回滚方法 |
| 评论点赞/踩 | [MarkDownRepository.LikeReviewAsync / DislikeReviewAsync](file:///f:/NotBlog/Markdown.Infrastructure/Repository/MarkDownRepository.cs) | 已有唯一约束；API 层无条件发 `MarkReviewLiked` 事件（**重复点赞也会发**，需一并修正） |
| 评论创建/子评论 | [CreateMarkReviewCommandHandler](file:///f:/NotBlog/Markdown.Web.API/Application/Commands/CreateMarkReviewCommandHandler.cs) / [AddChildReviewCommandHandler](file:///f:/NotBlog/Markdown.Web.API/Application/Commands/AddChildReviewCommandHandler.cs) | 暂无事件发布，本次接入评论发布通知 |
| 集成事件安全发布 | [EventPublishing.PublishSafelyAsync](file:///f:/NotBlog/Markdown.Web.API/Application/IntegrationEvents/EventPublishing.cs) | 总线故障不拖垮业务，仅记日志 |
| 现有集成事件 | [MarkReviewIntegrationEvents.cs](file:///f:/NotBlog/Markdown.Web.API/Application/IntegrationEvents/MarkReviewIntegrationEvents.cs) | 无消费者 |
| 通知实体 | [TweetNotification](file:///f:/NotBlog/Message.Domain/Entities/Tweet/TweetNotification.cs) | 通用站内通知（被关注/评论/圈子等复用） |
| 通知类型枚举 | [NotificationType](file:///f:/NotBlog/Message.Domain/Enums/NotificationType.cs) | 需追加 Markdown 6 类 |
| 通知读侧 | [NotificationsApi](file:///f:/NotBlog/Message.Web.API/Apis/NotificationsApi.cs) | `/api/notifications` 列表/未读数/已读，DTO 已通用，**无需改动** |
| 实时推送基建 | [MessageDeliveryService](file:///f:/NotBlog/Message.Web.API/Services/MessageDeliveryService.cs) | `GetOnlineConnectionsAsync(userIds)` + `IHubContext` 按连接推送，跨实例由 Redis 连接管理器保证无重 |
| SignalR 契约 | [IMessageClient](file:///f:/NotBlog/Message.Web.API/Hubs/IMessageClient.cs) + [MessageHub](file:///f:/NotBlog/Message.Web.API/Hubs/MessageHub.cs) | 需新增 `PushNotification` 方法 |
| 前端长连接 | [notblog-ui/src/socket/signalr.ts](file:///f:/NotBlog/notblog-ui/src/socket/signalr.ts) | 已连 `/MessageHub`，需订阅新方法 |

## 4. 总体设计

### 4.1 一次化约束矩阵

| 目标 | 动作 | 一次化 | 依据 |
|------|------|--------|------|
| 文档 | 点赞 / 取消点赞 | 已有 | `MarkDocumentLike` 唯一约束 |
| 文档 | **投币** | **本次收紧** | `MarkCoin` 新增唯一约束，重复投币幂等返回 |
| 评论 | 点赞 / 取消点赞 | 已有 | `MarkReviewLike` 唯一约束 |
| 评论 | 点踩 / 取消点踩 | 已有 | `MarkReviewDislike` 唯一约束 |

### 4.2 通知事件流（时序）

```
客户端                 Markdown.Web.API               RabbitMQ                 Message.Web.API              作者客户端
  │   POST /like/coin  │                                                             │                        │
  ├──────────────────► │ 越权校验(HasPermission+审核门控)                               │                        │
  │                    │ 交互幂等落库(首次? 成功?)                                       │                        │
  │                    │   ├─ 首次 ── PublishSafelyAsync(MarkdownInteractionEvent) ──► │                        │
  │                    │   └─ 重复 ── 幂等返回(不发事件)                                │                        │
  │                    ◄─ 计数/结果 ──                                                │                        │
  │                    │                                                             │ 消费事件               │
  │                    │                                                             │ 1. 操作者==作者? ──跳过   │
  │                    │                                                             │ 2. 写 TweetNotification │
  │                    │                                                             │ 3. 查作者昵称组装Title/Content │
  │                    │                                                             │ 4. SignalR PushNotification ◄── 在线即时推送 │
  │                    │                                                             │ (离线：OnConnected 或前端轮询 /api/notifications 补拉) │
```

设计要点：

- **投递模型**：异步集成事件（RabbitMQ）。发布失败仅记日志（沿用 `PublishSafelyAsync`），不阻塞互动主流程；通知丢失可接受，后续可升级 Outbox（文档 §9 已列）。
- **只发「首次」通知**：重复点赞/重复投币（幂等分支）不发事件，避免轰炸与误导。
- **取消类动作（unlike / undislike）不发通知**。
- **自互动过滤**：操作者与目标作者相同时跳过（`ActorUserId == TargetUserId`）。
- **评论作者定位**：事件内直接携带 `TargetUserId`（评论作者），避免 Message 服务反向查询 Markdown 域数据。
- **评论发布即通知**：发布评论无一次化约束（每次都是新评论），除自互动外每次都发通知；回复子评论通知被回复的评论作者（D-5）。

### 4.3 消息契约（新集成事件）

Markdown 侧发布**单一事件**，Message 侧由**单个 Handler** 按 `InteractionType` 分发，保持类型安全且易扩展：

```csharp
// Markdown.Web.API/Application/IntegrationEvents/MarkdownInteractionIntegrationEvent.cs
/// <summary>Markdown 交互集成事件（点赞/投币/评论点赞/评论踩；仅首次发生发布）</summary>
[EventBusName("MarkdownInteraction")]   // routing key 与 Message 侧 Handler 类特性对齐
public record MarkdownInteractionIntegrationEvent(
    MarkdownInteractionType InteractionType,  // DocumentLiked / DocumentCoined / ReviewLiked / ReviewDisliked
    Guid MarkDownGuid,
    string MarkDownName,
    Guid? ReviewGuid,                          // 评论类交互才有
    Guid ActorUserId,                          // 操作者
    Guid TargetUserId,                         // 通知对象：文档作者或评论作者
    long Amount,                               // 投币数量（仅 DocumentCoined）
    DateTimeOffset OccurredAt
) : IntegrationEvent;
```

字段说明：
- `ReviewGuid`：评论类交互定位到具体评论（前端跳转用）；文档类为 `null`。
- `Amount`：投币数量，用于「XX 打赏了您的文章 N 枚硬币」文案；其余交互为 0。
- `TargetUserId`：通知对象。文档交互取 `MarkDown.MarkUserGuid`；评论交互取 `MarkReview.UserId`。

> 兼容说明：既有 `MarkReviewLikedIntegrationEvent`（无消费者、字段缺 `TargetUserId`）**保留不删**，但点赞/点踩通知统一走新事件；删除类事件（MarkReviewDeleted 等）维持现状。

#### 4.3.1 评论发布事件（新增，独立于交互事件）

```csharp
// Markdown.Web.API/Application/IntegrationEvents/MarkdownCommentPublishedIntegrationEvent.cs
/// <summary>Markdown 评论发布集成事件（顶级评论通知博客作者；子评论通知被回复的评论作者）</summary>
[EventBusName("MarkdownCommentPublished")]   // routing key 与 Message 侧 Handler 类特性对齐
public record MarkdownCommentPublishedIntegrationEvent(
    Guid MarkDownGuid,
    string MarkDownName,
    Guid ReviewGuid,                 // 新评论标识（前端跳转用）
    Guid? ParentReviewGuid,          // 子评论（回复）才有；顶级评论为 null
    string CommentContent,           // 截断后的评论预览（如 50 字）
    Guid ActorUserId,                // 评论者
    Guid TargetUserId,               // 通知对象：顶级评论=博客作者；子评论=父评论作者
    DateTimeOffset OccurredAt
) : IntegrationEvent;
```

与交互事件的分工：`MarkdownInteraction` 覆盖「对已有对象的计数互动」（赞/币/评论赞/评论踩）；`MarkdownCommentPublished` 覆盖「新评论产生」（顶级/回复）。两者消费端 Handler 独立（§5.2.1）。

### 4.4 通知类型扩展（Message 域）

```csharp
// Message.Domain/Enums/NotificationType.cs 追加
MarkdownDocumentLiked,
MarkdownDocumentCoined,
MarkdownReviewLiked,
MarkdownReviewDisliked,
MarkdownCommentAdded,      // 博客收到新评论
MarkdownCommentReplied,    // 我的评论被回复
MarkdownPublished          // 好友发布了新文章（v1.2）
```

## 5. 详细设计（按服务拆分）

### 5.1 Markdown 服务改动

#### 5.1.1 投币一次化（领域 + 仓储 + 端点）

**(a) 实体**：`MarkQuote` 增加回滚方法（与 `RemoveLove` 同构，供唯一约束冲突时回滚内存计数）：

```csharp
/// <summary>减少硬币数（线程安全，下限钳制为 0；仅用于唯一约束冲突回滚）</summary>
public long RemoveCoin(long count = 1)
{
    if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "减少的数量不能为负数");
    var newValue = Interlocked.Add(ref _coinSome, -count);
    if (newValue < 0) { Interlocked.Exchange(ref _coinSome, 0); return 0; }
    return newValue;
}
```

**(b) EF 配置**：`MarkDownDbContext.OnModelCreating` 中 `MarkCoin` 增加唯一索引（`Markdown.Infrastructure/EntityFramework/MarkDownDbContext.cs`，在现有 `entity.HasIndex(x => x.MarkDownGuid);` 附近）：

```csharp
// MarkCoin：一用户对一篇文档仅可投币一次
entity.HasIndex(x => new { x.MarkDownGuid, x.UserId }).IsUnique();
```

> 需新增 EF 迁移（`Markdown.Infrastructure/Migrations`），并在 `Program.cs` 的 `AddMigration<MarkDownDbContext>()` 启动迁移机制下自动应用。**存量数据**：若存在同一用户对同一文档多条投币，迁移前需先清理（执行 `MIN` 保留首条或按文档为删重策略，见 §6.2）。

**(c) 仓储**：`CoinDocumentAsync` 幂等化，并返回「是否首次」供端点决定发事件。同步调整交互方法返回结构，统一幂等语义：

```csharp
/// <summary>
/// 文档投币（一用户一篇一次）：已有投币记录则幂等返回现有总额（IsFirst=true 时新增）。
/// 记录与计数同批提交；唯一约束冲突时整批回滚并回滚内存计数。
/// </summary>
public async Task<InteractionResult> CoinDocumentAsync(Guid markDownGuid, Guid userId, long amount)
{
    var markdown = await GetMarkDownTrackedAsync(markDownGuid)
        ?? throw new KeyNotFoundException($"MarkDown 文档不存在：{markDownGuid}");

    // 已投币：幂等返回当前总额，不新增记录，不发事件
    var exists = await markDownDbContext.MarkCoins
        .AnyAsync(c => c.MarkDownGuid == markDownGuid && c.UserId == userId);
    if (exists)
        return new InteractionResult(markdown.MarkQuote.CoinSome, IsFirst: false);

    markDownDbContext.MarkCoins.Add(new MarkCoin(markDownGuid, userId, amount));
    var count = markdown.AddCoin(amount);
    try
    {
        await markDownDbContext.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
    {
        // 并发重复投币：回滚内存计数，幂等返回当前总额
        markdown.RemoveCoin(amount);
        return new InteractionResult(markdown.MarkQuote.CoinSome, IsFirst: false);
    }
    return new InteractionResult(count, IsFirst: true);
}

/// <summary>交互结果：计数 + 是否首次发生（首次才发作者通知）</summary>
public readonly record struct InteractionResult(long Count, bool IsFirst);
```

> **同步调整**：将 `LikeDocumentAsync` / `LikeReviewAsync` / `DislikeReviewAsync` 的返回类型由 `long` 统一调整为 `InteractionResult`（`Count` 兼容原返回值语义）。**这是行为修正点**：现行 `MarkdownReviewApi.LikeReviewAsync` 在重复点赞（幂等分支）时也会 `PublishSafelyAsync(MarkReviewLiked...)`，本次改为仅 `IsFirst==true` 时发布通知事件。`RemoveLikeDocumentAsync` / `RemoveLikeReviewAsync` / `RemoveDislikeReviewAsync` 仍返回 `long`，取消类不发事件。

**(d) 端点**：`MarkdownApi.CoinDocumentAsync` 与 `LikeDocumentAsync`、`MarkdownReviewApi.LikeReviewAsync` / `DislikeReviewAsync` 在**首次成功分支**发布统一事件（示例，投币）：

```csharp
var result = await markdownRepository.CoinDocumentAsync(markDownGuid, userId, request.Amount);
if (result.IsFirst)
{
    // 组装通知对象：文档作者（markdown.MarkUserGuid）
    await EventPublishing.PublishSafelyAsync(eventBus, new MarkdownInteractionIntegrationEvent(
        MarkdownInteractionType.DocumentCoined,
        markDownGuid, markdown.MarkDownName, ReviewGuid: null,
        ActorUserId: userId, TargetUserId: markdown.MarkUserGuid,
        Amount: request.Amount, OccurredAt: DateTimeOffset.UtcNow), logger);
}
await hotBoardService.UpdateScoreAsync(markDownGuid);
return Results.Ok(ApiResponse<long>.Ok(result.Count, result.IsFirst ? "打赏成功" : "您已打赏过该文章，不重复累计"));
```

`InteractionType` 枚举（Markdown.Web.API 侧新增）：

```csharp
public enum MarkdownInteractionType { DocumentLiked, DocumentCoined, ReviewLiked, ReviewDisliked }
```

#### 5.1.2 越权与门控

各端点维持现有模式（`HasPermission` + `IsApproved` 门控，越权一律 404），本方案不改变权限逻辑，仅新增事件发布。

#### 5.1.3 评论发布事件发布点

在评论命令处理器**落库成功后**发布（顶级与子评论分别处理）：

```csharp
// CreateMarkReviewCommandHandler（顶级评论）：落库成功后
await EventPublishing.PublishSafelyAsync(eventBus, new MarkdownCommentPublishedIntegrationEvent(
    MarkDownGuid: request.MarkDownGuid,
    MarkDownName: markdownName,                    // 命令内已加载的文档名
    ReviewGuid: reviewGuid,
    ParentReviewGuid: null,
    CommentContent: Truncate(request.MarkReviewContent, 50),
    ActorUserId: request.MarkUserGuid,             // 评论者
    TargetUserId: markdown.MarkUserGuid,           // 博客作者
    OccurredAt: DateTimeOffset.UtcNow), logger);

// AddChildReviewCommandHandler（子评论）：成功分支
//   TargetUserId = 父评论.UserId（被回复的评论作者）
//   ParentReviewGuid = parentReviewGuid；ActorUserId = 子评论作者
```

要点：
- **顶级评论**：`TargetUserId = MarkDown.MarkUserGuid`（博客作者），`ParentReviewGuid = null`，Message 侧映射 `MarkdownCommentAdded`；
- **子评论（回复）**：`TargetUserId = 父评论.UserId`，`ParentReviewGuid = 父评论 Guid`，Message 侧映射 `MarkdownCommentReplied`；
- **发布即通知**（无一次化约束），但发布者为被通知者本人时跳过（在 Message 侧 Handler 统一过滤，见 §5.2.1）；
- 评论内容截断（如 50 字）写入事件，供通知文案预览；空内容用「图片评论」等占位。

> 实现依赖：命令处理器需已持有 `MarkDown`（作者）或 `ParentReview`（其 `UserId`）；`CreateMarkReviewCommandHandler` 通常已通过仓储加载文档做校验，直接复用；若未加载需补一次 `FindMarkDownAsync(markDownGuid)` 查询（代价可接受）。

#### 5.1.4 文章发布 → 好友/关注者聚合通知（复用已有事件）

**Markdown 侧零改动**：`CreateMarkdownCommandHandler` 已发布 `MarkdownCreatedEventData`（`[EventBusName("MarkdownCreated")]`，含 `MarkDownGuid / MarkUserGuid(作者) / FileName / CreatedAt`）。通知好友与关注者、以及 10 分钟窗口聚合均在 Message 侧消费实现（§5.2.1）：

```csharp
// 消费侧逻辑要点（Message.Web.API/MarkdownCreatedIntegrationEventHandler.cs）
// 1. 接收者 = 好友 ∪ 关注者（GetFriendIdsAsync ∪ GetFollowerIdsAsync，并集去重）
// 2. 聚合模式（Redis 可用）：
//    对每个接收者写 Hash 桶 markdown:pub:agg:{receiverId}（field=发布者:文章，value=文章名|昵称）
//    刷新桶 TTL=25 分钟（> 2×窗口，防聚合前过期丢事件）；接收者加入索引 Set
//    → 等待后台聚合器（每 10 分钟）统一合成为一条通知
// 3. 降级路径（Redis 不可用）：立即逐条发送（保证不丢，不聚合）
```

聚合器（`Message.Web.API/Background/MarkdownPublishAggregationService.cs`，每 10 分钟）：

```csharp
// 1. 扫描索引 Set markdown:pub:agg:receivers → 每个接收者桶
// 2. 单桶加 Redis 处理锁（SETNX 30s）防多实例并发
// 3. 桶数据组装文案（BuildAggregatedCopy 纯函数）：
//     1 篇  → "好友/关注者发布了新文章" / "{昵称} 发布了新文章《{名}》"
//     N 篇  → "好友/关注者动态" / "您的 N 位好友/关注者发布了新文章：{昵称1、昵称2…等}"
// 4. 落库 TweetNotification(MarkdownPublished) + 实时推送
// 5. 先删桶再摘索引（幂等；崩溃窗口极小最多重复一条）
```

要点：
- 好友/关注者关系均不含作者本人 → 天然免自通知；
- 同一用户既是好友又是关注者 → 并集去重只收一条；
- 聚合 = 同一接收者 10 分钟窗口内多条发布合并为一条提醒，避免轰炸；
- Redis 不可用时自动降级为逐条直发（保可用、保不丢，只是不聚合）。

### 5.2 Message 服务改动

#### 5.2.1 新集成事件消费者

仿照 [RegisterByUserIntegrationEventHandler](file:///f:/NotBlog/Message.Web.API/Application/IntegrationEvents/EventHandlers/RegisterByUserIntegrationEventHandler.cs) 的 `JsonIntegrationEventHandler` 模式，新增：

```csharp
// Message.Web.API/Application/IntegrationEvents/EventHandlers/MarkdownInteractionIntegrationEventHandler.cs
[EventBusName("MarkdownInteraction")]   // 与发布侧 routing key 对齐
public class MarkdownInteractionIntegrationEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUserInfoRepository userInfoRepository,
    NotificationSignalRService notificationPush,   // 见 5.2.3
    ILogger<...> logger)
    : JsonIntegrationEventHandler<MarkdownInteractionMessageIntegrationEvent>
{
    public override async Task Handler(MarkdownInteractionMessageIntegrationEvent @event)
    {
        // 1. 自互动过滤：操作者 == 目标作者时不通知
        if (@event.ActorUserId == @event.TargetUserId) return;

        // 2. 组装通知文案（枚举 → Title/Content），见 5.2.4
        var (title, content) = NotificationCopyBuilder.Build(@event);   // 昵称经 userInfoRepository 查询

        // 3. 落库（TweetNotification 通用实体）
        await notificationRepository.AddAsync(TweetNotification.Create(
            @event.TargetUserId,
            MapType(@event.InteractionType),
            title, content,
            RefType: @event.ReviewGuid is null ? "Markdown" : "MarkReview",
            RefGuid: @event.ReviewGuid ?? @event.MarkDownGuid));

        await notificationRepository.UnitOfWork.SaveEntitiesAsync();

        // 4. 实时推送（在线即达；离线用户由前端轮询/重连补拉）
        await notificationPush.PushAsync(@event.TargetUserId, savedNotify);
    }
}
```

> 事件数据副本 `MarkdownInteractionMessageIntegrationEvent` 在 **Message 程序集内独立定义**（跨服务不共享程序集，与 `RegisterByUserMessageIntegrationEvent` 一致），字段与发布侧一一对应。

**评论发布消费者（新增）**：责任与交互消费者对称，单独一个 Handler：

```csharp
// Message.Web.API/Application/IntegrationEvents/EventHandlers/MarkdownCommentPublishedIntegrationEventHandler.cs
[EventBusName("MarkdownCommentPublished")]   // 与发布侧 routing key 对齐
public class MarkdownCommentPublishedIntegrationEventHandler(
    ITweetNotificationRepository notificationRepository,
    IUserInfoRepository userInfoRepository,
    NotificationSignalRService notificationPush,
    ILogger<...> logger)
    : JsonIntegrationEventHandler<MarkdownCommentPublishedMessageIntegrationEvent>
{
    public override async Task Handler(MarkdownCommentPublishedMessageIntegrationEvent @event)
    {
        // 1. 自发布过滤：评论者 == 被通知者时不通知
        if (@event.ActorUserId == @event.TargetUserId) return;

        // 2. 类型映射：ParentReviewGuid == null → MarkdownCommentAdded；否则 → MarkdownCommentReplied
        var type = @event.ParentReviewGuid is null
            ? NotificationType.MarkdownCommentAdded
            : NotificationType.MarkdownCommentReplied;

        // 3. 文案组装（昵称经 userInfoRepository 查询；内容预览来自 @event.CommentContent）
        var (title, content) = NotificationCopyBuilder.Build(@event);

        // 4. 落库 + 实时推送（与交互消费者一致，可抽公共基类复用）
        await notificationRepository.AddAsync(TweetNotification.Create(
            @event.TargetUserId, type, title, content,
            RefType: "MarkReview", RefGuid: @event.ReviewGuid));
        await notificationRepository.UnitOfWork.SaveEntitiesAsync();
        await notificationPush.PushAsync(@event.TargetUserId, savedNotify);
    }
}
```

> 两个消费者（交互 / 评论发布）的「自互动过滤 → 文案组装 → 落库 → 推送」流程一致，可抽取公共抽象基类 `MarkdownNotificationConsumerBase`（仅差异点为：类型映射与文案来源），避免重复代码。

#### 5.2.2 SignalR 契约扩展

`IMessageClient` 新增方法（MessageHub 为 `Hub<IMessageClient>`，类型化客户端契约即后端 Push 契约）：

```csharp
/// <summary>接收站内通知（点赞/投币/评论互动等触发的作者通知）</summary>
Task PushNotification(NotificationDto notification);
```

#### 5.2.3 推送服务（可复用已有基建）

`MessageDeliveryService` 已提供「按 userIds 并行取在线连接」与 `IHubContext` 推送能力，新增一个方法并在其上构建轻量服务：

```csharp
// MessageDeliveryService 增加
/// <summary>向指定用户的所有在线连接推送通知（离线静默，读侧补拉）</summary>
public async Task NotifyNotificationAsync(Guid userId, NotificationDto dto, CancellationToken ct = default)
{
    var connections = await GetOnlineConnectionsAsync([userId], ct);
    if (connections.Count == 0) return;
    await Task.WhenAll(connections.Select(c => _hubContext.Clients.Client(c).PushNotification(dto)));
}
```

> 通知推送服务 `NotificationSignalRService`（封装上述方法）注册为 **Singleton**，内部经 `IServiceScopeFactory` 解析 Scoped 依赖（参照 `CallSessionStore` 的解析模式，避免 Singleton 注入 Scoped 冲突）。Handler 在落库后调用。

#### 5.2.4 通知文案映射

| NotificationType | Title | Content 示例（昵称经 UserInfo 表查得） |
|---|---|---|
| `MarkdownDocumentLiked` | 文章被点赞 | 「{昵称} 赞了您的文章《{Name}》」 |
| `MarkdownDocumentCoined` | 收到打赏 | 「{昵称} 打赏了您的文章《{Name}》 {Amount} 枚硬币」 |
| `MarkdownReviewLiked` | 评论被点赞 | 「{昵称} 赞了您在《{Name}》下的评论」 |
| `MarkdownReviewDisliked` | 评论被点踩 | 「{昵称} 踩了您在《{Name}》下的评论」 |
| `MarkdownCommentAdded` | 文章收到新评论 | 「{昵称} 评论了您的文章《{Name}》」 |
| `MarkdownCommentReplied` | 评论被回复 | 「{昵称} 回复了您在《{Name}》下的评论」 |
| `MarkdownPublished` | 好友发布了新文章 | 「{昵称} 发布了新文章《{Name}》」 |

读侧 DTO 已通用（`NotificationDto.Type` 直接映射枚举名），无需改动 [NotificationMappingExtensions](file:///f:/NotBlog/Message.Web.API/Dto/Response/NotificationMappingExtensions.cs) 以外的新类型适配（若需要图标/语义化可在此扩展）。

### 5.3 前端改动（notblog-ui）

1. **订阅实时通知**：在现有 `/MessageHub` 连接（[signalr.ts](file:///f:/NotBlog/notblog-ui/src/socket/signalr.ts)）上注册 `PushNotification` 回调：

```ts
// stores/notification.ts（新增轻量 store）或 chat.ts 中注册
conn.on('PushNotification', (notification) => {
  // 1) 未读数 +1（本地乐观更新，后台下次轮询校准）
  // 2) 通知列表头部插入（可选）
  // 3) 轻提示 Toast（例如「XX 赞了您的文章」）
});
```

2. **通知入口**：沿用现有 `/api/notifications`（未读数徽标 + 列表 + 全部已读），无后端改动。

### 5.4 数据库迁移（Markdown.Infrastructure）

新增迁移：`AddMarkCoinUniqueConstraint`

```csharp
migrationBuilder.CreateIndex(
    name: "IX_MarkCoin_MarkDownGuid_UserId",
    table: "MarkCoin",
    columns: new[] { "MarkDownGuid", "UserId" },
    unique: true);
```

Message 侧无需迁移（`TweetNotification` 与 `NotificationType` 为枚举字符串列，无需改表）。

### 5.5 配置

- AppHost / 独立运行的 EventBus 连接沿用现状，无新配置项。
- 网关 YARP 对 `/MessageHub` 的配置不变。

## 6. 幂等、一致性与可靠性

| 风险点 | 策略 |
|--------|------|
| 投币并发重复 | 唯一约束 + `IsUniqueViolation(23505)` 捕获，整批回滚并回滚内存计数（`RemoveCoin`），幂等返回现总额 |
| 重复点赞误发事件 | 端点仅 `IsFirst==true` 分支发布；重复交互幂等分支不发 |
| 总线故障丢事件 | 沿用 `PublishSafelyAsync`（仅记日志）；业务已成功落库；如需严格投递升级 Outbox（§9） |
| 消费者异常 | Handler 整体 try/catch，失败仅记日志（与 `RegisterByUser...` 一致），不重投 |
| 存量重复投币 | 迁移前清理（§6.2）；首次发布 v1 后可接受「存量不追发通知」 |
| 跨实例推送重复 | 复用 Redis 连接管理器去重（`GetOnlineConnectionsAsync` 已 `Distinct`） |

### 6.1 自互动与越权

- 事件携带 `ActorUserId` / `TargetUserId`，Handler 首行过滤自互动；
- 互动端点原有的接口层越权校验不变，事件只在合规成功后发布。

### 6.2 存量数据治理（投币去重）

迁移脚本执行**前**对 `MarkCoin` 按 `(MarkDownGuid, UserId)` 分组清理（保留 `CreateAt` 最早的一条），并同步回退 `MarkQuote.CoinSome` 冗余累计：
停止服务 → 清理重复记录并重算 `CoinSome`（= 每组首条 Amount 之和）→ 应用唯一索引迁移 → 恢复服务。需 DBA 确认数据量并安排在低峰窗口。

## 7. 测试计划

| 层级 | 用例 |
|------|------|
| 领域单测（Markdown.Tests） | ① `MarkCoin` 唯一约束冲突回滚后 `CoinSome` 不变；② 重复投币幂等返回现总额；③ `RemoveCoin` 下限钳制 0 |
| 仓储集成（Markdown.Tests/Status） | ① 同用户重复投币两次 → 记录仅 1 条、总额=首投金额；② 取消点赞后再次点赞 → 新记录、`IsFirst=true`（点赞通知恢复语义） |
| API 级（MessageHubTests 同目录增设） | ① 首次投币 201 + 返回金额；② 重复投币 200 + 返回现总额 + **不发布事件**（用 RabbitMQ 测试容器或 mock `IEventBus` 断言发布次数） |
| 评论发布通知（Markdown.Tests / Message.Tests） | ① 顶级评论落库后发布事件（`ParentReviewGuid=null`、Target=博客作者）；② 子评论发布事件（Target=父评论作者）；③ 评论者即作者/父评论作者时不发布事件；④ 消费者完成类型映射（Added/Replied）与落库+推送 |
| 文章发布好友通知（Message.Tests） | ① 发布后按好友 ID 列表逐人生成 `MarkdownPublished` 通知；② 无好友时跳过；③ 昵称缺失回退占位；④ 离线好友仅落库不推送 |
| 消费者（Message.Tests） | ① Handler 落库 + 仅在线连接收到 `PushNotification`；② 自互动（Actor==Target）不落库不推送；③ 总线并发消息顺序无关性 |
| 前端 | ① SignalR 收到 `PushNotification` 后未读角标 +1；② 通知列表/已读流程回归；③ 离线用户重连后轮询拉到通知 |

## 8. 验收标准

1. 同一用户对同一文档**第二次投币**不可再增加 `CoinSome`，接口幂等返回现总额。
2. 赞 / 投币 / 评论赞 / 评论踩的发生**首次**均会给目标作者生成 `/api/notifications` 通知；重复互动与取消类操作不产生通知。
3. 作者在线时，`/MessageHub` 实时收到 `PushNotification`；离线后重连/轮询可补拉。
4. 操作者点赞/投币自己的文章或评论，**不**收到通知。
5. **发布评论**（顶级）后博客作者收到「新评论」通知；**回复子评论**后被回复的评论作者收到「回复」通知；本人发帖/回复自己不通知（D-5）。
6. **创建文章**后，作者的全部**好友与关注者**收到「新文章」通知（并集去重一条）；同一接收者 10 分钟窗口内多条发布会**聚合为一条提醒**；作者本人不收到（D-6）。
7. 总线不可用时互动主流程不受影响，仅通知丢失且有错误日志。

## 9. 风险与后续建议

- **严格投递**：当前通知为「尽力而为」。如需不丢通知，升级 EventBus Outbox 模式（项目已有 [Outbox](file:///f:/NotBlog/Eventbus/Outbox) 基础设施可复用）。
- **通知收敛**：后续可增加「抖动态/通知聚合」（如同一作者短时间内多次点赞只发一条聚合通知），降低骚扰。
- **点赞恢复语义**：本次取消点赞不发通知、再次点赞视为新互动发通知，已默认合理；如需「取消也通知」可另行迭代。

## 10. 实施任务清单

```text
[ ] 1.  Markdown.Domain：MarkQuote.RemoveCoin
[ ] 2.  Markdown.Infrastructure：MarkCoin 唯一索引 + 迁移 + 存量清理脚本
[ ] 3.  Markdown.Infrastructure：交互仓储返回 InteractionResult（Coin/Like/ReviewLike/ReviewDislike）
[ ] 4.  Markdown.Web.API：MarkdownInteractionIntegrationEvent + InteractionType 枚举
[ ] 5.  Markdown.Web.API：四端点首次分支发布交互事件（Coin/Like/ReviewLike/ReviewDislike）
[ ] 5.5 Markdown.Web.API：MarkdownCommentPublishedIntegrationEvent + Create/AddChildReviewCommandHandler 发布点（§5.1.3）
[ ] 5.6 Message.Web.API：MarkdownCreated 消费者（文章发布→好友通知，Markdown 侧零改动，§5.1.4）
[ ] 5.7 Message.Web.API：好友∪关注者 + Redis 桶聚合 + 降级直发（§5.1.4）+ MarkdownPublishAggregationService（10 分钟窗，§5.1.4）+ IUserFollowRepository.GetFollowerIdsAsync
[ ] 6.  Markdown.Tests：幂等/回滚/事件发布次数 + 评论发布事件用例
[ ] 7.  Message.Domain：NotificationType 追加 6 枚举值（含 MarkdownCommentAdded/Replied）
[ ] 8.  Message.Web.API：IMessageClient.PushNotification + MessageDeliveryService.NotifyNotificationAsync
[ ] 9.  Message.Web.API：MarkdownInteraction 消费者 Handler
[ ] 9.5 Message.Web.API：MarkdownCommentPublished 消费者 Handler（可与 9 抽取公共基类）
[ ] 10. Message.Tests：两类消费者用例（含评论发布映射）
[ ] 11. 前端：signalr.ts 订阅 PushNotification + 未读数/Toast store
[ ] 12. 端到端验证 + 验收清单逐项核对
```