# Message 服务功能完善度审查与修复方案

> 日期：2026-08-09 ｜ 范围：`Message.Domain` / `Message.Infrastructure` / `Message.Web.API` ｜ 基线：`a8b4857c`（feat/markdown-favorites）
> 方法：端点全表核对 + 符号引用追踪 + 设计文档对照 + 单项目构建验证
> 结论：代码编译通过（0 个 CS 错误）；构建失败仅因 `Message.Web.API.exe` 被运行中的进程占用（文件锁 MSB3021），服务当前在跑

---

## 1. 现状诊断

### 1.1 模块总览

| 层 | 内容 |
|---|---|
| Domain | 26 实体（IM/群组/好友/Tweet 系列/社区系列）、35 枚举、54 域事件、16 仓储接口、8 领域服务接口 |
| Infrastructure | 17 EntityConfig、15 仓储、Redis/EventBus/邮件等 10 服务、`Sql/` 仅 1 个脚本（CommunitySchema.sql，5 张社区表） |
| Web.API | 12 个 API 文件（约 130 端点）、118 命令/查询文件、28 个域事件处理器、2 个 Hub（MessageHub/CommunityHub） |

### 1.2 端点全表（现状）

| API 组 | 端点 |
|---|---|
| `/api/audit` | GET /tweets/pending ｜ POST /tweets/{id}/approve ｜ POST /tweets/{id}/reject ｜ GET /reports/pending ｜ POST /reports/{id}/resolve |
| `/api/circles` | POST /join ｜ GET /invitations/my ｜ POST /invitations/{id}/accept ｜ POST /invitations/{id}/reject ｜ GET /my ｜ POST / ｜ GET /{id} ｜ PUT /{id} ｜ DELETE /{id} ｜ POST /{id}/invitations ｜ GET /{id}/invitations ｜ DELETE /{id}/invitations/{iid} ｜ GET /{id}/members ｜ POST /{id}/members/{uid}/role ｜ DELETE /{id}/members/{uid} ｜ POST /{id}/transfer ｜ GET /{id}/posts |
| `/api/comments` | POST / ｜ GET /tweet/{tid} ｜ GET /{cid}/replies ｜ DELETE /{cid} |
| `/api/files` | POST /upload ｜ POST /upload-image ｜ POST /chunk/{init,upload,status,merge,cancel,resume} |
| `/api/follows` | POST /{uid} ｜ DELETE /{uid} ｜ GET /following ｜ GET /followers ｜ GET /feed |
| `/api/friends` | POST /request ｜ PUT /request/{fid} ｜ GET / ｜ GET /requests ｜ GET /sent-requests ｜ DELETE /{fid} ｜ PUT /{fid}/{block,remark,star,mute} ｜ GET /{blocked,starred,count,search} |
| `/api/groups` | POST / ｜ GET / ｜ GET /{id} ｜ PUT /{id}/info ｜ DELETE /{id} ｜ GET/POST /{id}/members ｜ DELETE /{id}/members/{uid} ｜ PUT /{id}/admins ｜ PUT /{id}/transfer ｜ PUT/DELETE /{id}/members/{uid}/{mute,ban} ｜ GET /public ｜ GET /search ｜ GET /{id}/member-count ｜ GET /{id}/is-member/{uid} |
| `/api/messages` | POST / ｜ GET /{id} ｜ DELETE /{id}（撤回） ｜ POST /{id}/forward ｜ PUT /{id}/read ｜ GET /search ｜ GET /unread ｜ GET /sessions/{sid}/messages ｜ 附件 6 端点 |
| `/api/reports` | POST / ｜ GET /my |
| `/api/sessions` | POST / ｜ GET / ｜ GET /{id} ｜ PUT /{id}/{pin,mute} ｜ DELETE /{id} ｜ GET/POST /{id}/participants ｜ DELETE /{id}/participants/{uid} ｜ GET /pinned ｜ GET /unread-count |
| `/api/topics` | POST / ｜ GET / ｜ GET /{id}/posts |
| `/api/tweets` | POST / ｜ POST /circle ｜ POST /draft ｜ GET /{id} ｜ GET /user/{uid} ｜ GET /timeline ｜ GET /trending ｜ PUT /{id}（改草稿） ｜ DELETE /{id} ｜ POST/DELETE /{id}/{pin,like,favorite} ｜ POST /{id}/{share,coin,view} |

### 1.3 问题总表

| 编号 | 级别 | 问题 | 一句话结论 |
|---|---|---|---|
| R-01 | 🔴 P1 | REST 消息链路（发送/撤回/已读）无实时推送 | 对端收不到任何通知，SignalR 与 REST 行为不一致 |
| R-02 | 🔴 P1 | 关注 Feed 泄露 Private 推文 | `/follows/feed` 未做可见性过滤 |
| R-03 | 🔴 P1 | `Visibility.Followers` 可见性未接入关注关系 | 策略注释已过时，关注者/非关注者都看不到 |
| R-04 | 🔴 P1 | 站内通知只有写没有读 | 0 字节空命令 + 无 API + 无 DbSet + 无建表 SQL |
| R-05 | 🟠 P2 | `TweetCreatedEventHandler` 审核链路空转 | 把空字符串传给审核服务；写侧已有真实敏感词过滤 |
| R-06 | 🟠 P2 | 浏览量可刷 + 详情页叠加计数 | 无去重；GET 详情每次 +1 且每次重算热度 |
| R-07 | 🟠 P2 | 三处查询 TotalCount 与过滤不一致 | 分页计数虚高 |
| R-08 | 🟠 P2 | 草稿无独立列表入口 | 只能混在用户主页 |
| R-09 | 🟠 P2 | 在线/离线状态推送未接线 | `NotifyUserStatusChangedAsync` 零调用 |
| R-10 | 🟠 P2 | Tweet 系列表无建表脚本 | DDL 只存在于设计文档 Markdown |
| R-11 | 🟡 P3 | 8+ 事件处理器空壳、10 个事件无处理器 | 群解散/移出成员等无任何通知 |
| R-12 | 🟡 P3 | 圈子发现/话题管理缺失 | 无公开圈子列表；话题无法停用 |
| R-13 | 🟡 P3 | list.md 声称已完成的实体实际不存在 | 已读回执/离线消息/加密 全仓零引用 |
| R-14 | 🟡 P3 | `RecallConfig` 死代码 | 撤回时限硬编码 2 分钟 |
| R-15 | 🟡 P3 | `MessagePush`/`MessageOther` 发送即抛异常 | 需明确产品意图 |
| R-16 | 🟡 P3 | 邮件/本地化渠道为占位实现 | 已文档化，待接入 NotEmail |
| R-17 | 🟡 P3 | `EncryptionAlgorithm` 死枚举 | 零使用 |

---

## 2. 设计原则

1. **加性修改优先**：不破坏现有端点契约，全部修复为新增端点/新增过滤/增强推送。
2. **推送单一出口**：实时推送统一走 `MessageDeliveryService`，禁止在命令/处理器里直接操作 `IHubContext`。
3. **可见性判定收敛**：所有可见性判断收敛到 `TweetVisibilityPolicy` 一个点，查询层只负责把"查看者的关注集合"喂给它，避免散落判断。
4. **过滤下沉到仓库层**：可见性/状态过滤必须在 SQL 层完成（先过滤后分页），TotalCount 与 items 同条件，杜绝内存过滤导致的分页不一致。
5. **建表脚本唯一事实源**：Message 无 EF Migrations，`Message.Infrastructure/Sql/` 下的脚本必须与 EntityConfig 完全对照，新表必须同步出脚本。
6. **事件处理器是业务出口**：域事件处理器负责通知/推送等副作用；Hub 内联逻辑保持不变（迁移风险），新增链路（REST）补上等价副作用。

---

## 3. 问题详情与修复方案

### 🔴 R-01 REST 消息链路无实时推送

**位置**：`Application/Commands/Messages/SendMessageCommandHandler.cs:31`、`RecallMessageCommandHandler.cs:23`、`MarkMessageAsReadCommandHandler.cs:18`；空壳 `DomainEventHandlers/MessageSentEventHandler.cs:27`、`MessageRecalledEventHandler.cs`、`MessageReadEventHandler.cs`

**现状**：REST `POST /api/messages` 只落库 + 失效缓存；`DELETE /{id}`（撤回）与 `PUT /{id}/read` 同样不推送。`MessageSentEvent` 等 3 个域事件处理器是空壳（`await Task.CompletedTask`）。完整推送逻辑只存在于 `MessageHub.SendMessage/MarkAsRead/RecallMessage`（内联 `DeliverMessageAsync`/`NotifyMessageRecalledAsync`/`NotifyMessageReadAsync`）。

**影响**：前端任何走 REST 的消息操作，对端零实时感知（无新消息、无撤回、无已读回执），只能靠轮询 `GET /sessions/{sid}/messages`。与 SignalR 链路行为不一致。

**方案（REST 路径补齐推送，与 Hub 对齐）**：
1. `SendMessageCommandHandler`：注入 `MessageDeliveryService`，`Handler` 末尾在保存成功后调用 `delivery.DeliverMessageAsync(sessionId, message.MapToDto(), session.Participants, ct)`（session 已在 `ValidateSessionAndSenderAsync` 取得）。媒体消息分支返回前同样处理。
2. `RecallMessageCommandHandler`：取 message → 取 session → `delivery.NotifyMessageRecalledAsync(message.SessionId, messageId, session.Participants, ct)`。
3. `MarkMessageAsReadCommandHandler`：取 message → 取 session → `delivery.NotifyMessageReadAsync(message.SessionId, messageId, userId, session.Participants, ct)`。
4. REST 路径不补 `Clients.Group` 广播（那是 Hub 按连接订阅的专属通道，参与者连接推送已覆盖）。

**涉及文件**：3 个命令 handler + 注入构造函数（⚠️ 同步改 `Message.Tests` 中 Moq 构造）。空壳事件处理器暂不动（见 R-11）。

**工作量**：S ｜ **验证**：启动服务，REST 发消息 → 对端 SignalR 客户端收到 `ReceiveMessage`；REST 撤回 → 对端收到 `MessageRecalled`。

---

### 🔴 R-02 关注 Feed 泄露 Private 推文

**位置**：`Application/Queries/Community/GetCommunityFeedQueryHandler.cs:20` → `Infrastructure/Repository/TweetRepository.cs` `GetCommunityFeedAsync`（只过滤 `Approved && CircleGuid==null && authorIds.Contains`）

**现状**：`/follows/feed` 返回关注列表中所有用户的推文，**未做可见性过滤**。对照 `GetTimelineQueryHandler`/`GetTrendingQueryHandler` 都有过滤，唯独 feed 漏了。

**影响**：关注对象发 `Private`（仅自己可见）推文会出现在你的 Feed 中，隐私泄露。

**方案**：仓库方法加 `Guid viewerId` 参数，SQL 层过滤 `Visibility != Private || AuthorGuid == viewerId`（Followers 语义在 R-03 后统一由策略处理，feed 场景查看者已关注作者，天然满足 Followers 条件）：

```csharp
// TweetRepository.GetCommunityFeedAsync(authorGuids, viewerId, page, pageSize)
.Where(t => t.TweetStatus == TweetStatus.Approved
            && t.CircleGuid == null
            && ids.Contains(t.AuthorGuid)
            && (t.Visibility != Visibility.Private || t.AuthorGuid == viewerId))
```

`GetCommunityFeedCountAsync` 同步加同条件过滤。handler 注入 `ICurrentUserService` 取 viewerId（未认证传 `Guid.Empty`）。

**涉及文件**：TweetRepository（2 方法）+ GetCommunityFeedQueryHandler ｜ **工作量**：S ｜ **验证**：A 用户 Private 推文不出现在 B 的关注 Feed；Public 正常。

---

### 🔴 R-03 `Visibility.Followers` 未接入关注关系

**位置**：`Services/TweetVisibilityPolicy.cs:10`（注释"当前 Message 模块未实现关注关系"——已过时）；使用处：`GetTimelineQueryHandler`、`GetTweetDetailQueryHandler`、`GetUserTweetsQueryHandler`

**现状**：关注关系 2026-08 已实现（`UserFollow` 实体 + `IUserFollowRepository.ExistsAsync/GetFollowingIdsAsync`），但策略仍把 Followers 退化为"仅作者可见"：**关注者看不到，非关注者也看不到**，该可见级别完全失效。

**方案**：
1. 改造策略为携带查看者关注集合：

```csharp
public static bool IsVisibleTo(Tweet tweet, Guid viewerId, IReadOnlySet<Guid>? followingIds = null)
{
    if (tweet.AuthorGuid == viewerId) return true;
    if (tweet.Visibility == Visibility.Public) return true;
    if (tweet.Visibility == Visibility.Followers)
        return followingIds?.Contains(tweet.AuthorGuid) == true;
    return false; // Private
}
```

2. 三个查询 handler 注入 `IUserFollowRepository`：viewerId 非空时 `GetFollowingIdsAsync(viewerId)` 一次取全集，避免逐条 `ExistsAsync` 的 N+1。
3. `GetTrendingQueryHandler` 保持 Public-only（热榜语义不变）；`GetCommunityFeedQueryHandler` 在 R-02 修复后同步走策略。
4. 更新策略文件注释，删除"未实现关注关系"的过时说明。

**涉及文件**：TweetVisibilityPolicy.cs + 4 个查询 handler ｜ **工作量**：S ｜ **验证**：关注者能看到关注对象的 Followers 推文；取消关注后不可见；Private 仍仅作者可见。

---

### 🔴 R-04 站内通知只有写没有读

**位置**：空文件 `Application/Commands/CreateTwewtNotificationCommand.cs`（0 字节）；`MessageDbContext.cs` 无 `DbSet<TweetNotification>`；`TweetNotificationRepository` 用 `context.Set<>()` 绕过；6 个事件处理器在写通知（TweetApproved/TweetRejected/CommentAdded/CircleInvitationCreated/UserFollowed/ReportResolved）

**现状**：通知持续落库（若表存在），但**没有任何读取端点**——用户永远看不到通知。`GetByUserAsync/GetUnreadCountAsync/MarkAllAsReadAsync` 全部闲置。且仓库无该表建表脚本（见 R-10），本地库若无手工建表，6 个处理器写库即抛 "relation does not exist"。

**方案（补齐读侧 + 基础设施）**：
1. **删除 0 字节空文件** `CreateTwewtNotificationCommand.cs`（通知创建由事件处理器直写，命令无存在意义）。
2. `MessageDbContext` 增加 `DbSet<TweetNotification> TweetNotifications`。
3. 新增 `Apis/NotificationsApi.cs`（`MapNotificationsApi` 挂入 Program.cs，全部 `RequireAuthorization`）：

| 方法 | 路由 | 查询/命令 | 说明 |
|---|---|---|---|
| GET | `/api/notifications` | `GetMyNotificationsQuery(UserId, Page, PageSize, UnreadOnly)` | 我的通知列表（按 CreateTime 倒序） |
| GET | `/api/notifications/unread-count` | `GetUnreadNotificationCountQuery(UserId)` | 未读数 |
| PUT | `/api/notifications/read-all` | `MarkAllNotificationsReadCommand(UserId)` | 全部已读 |
| PUT | `/api/notifications/{notifyGuid}/read` | `MarkNotificationReadCommand(NotifyGuid, UserId)` | 单条已读（校验归属，非本人 404） |

4. 新文件按规范：`Dto/Request`（无）、`Dto/Response/NotificationDto.cs`、`Application/Queries/Notifications/` + `Application/Commands/Notifications/`，命令/查询 record 与 Handler 同文件（对照 `GetMyReportsQuery` 风格），DTO 映射方法放实体扩展或 Mapper。
5. 仓储已具备全部所需方法（`GetByUserAsync/GetUnreadCountAsync/MarkAllAsReadAsync/UpdateAsync`），无需改动；单条已读 = 取实体 → `MarkAsRead()` → `UpdateAsync`。

**涉及文件**：新 8-10 个文件 + MessageDbContext + Program.cs ｜ **工作量**：M ｜ **验证**：触发一次评论/关注 → 通知落库 → GET 列表可见 → 已读后 unread-count 下降。

---

### 🟠 R-05 `TweetCreatedEventHandler` 审核链路空转

**位置**：`DomainEventHandlers/TweetCreatedEventHandler.cs:26-27`（`var content = string.Empty;` / `var mediaUrls = Array.Empty<string>();`）

**现状**：handler 把**空内容/空媒体**传给 `ISensitiveWordFilter` 与 `IImageModerationService`，连占位日志打的都是空内容——链路完全空转。**修正结论**：写侧其实已有真实过滤——`CreateTweetCommandHandler`/`SaveDraftCommandHandler`/`UpdateDraftCommandHandler` 均调用 `SensitiveWordFilter.ContainsSensitive`（`Services/SensitiveWordFilter.cs`，内置 15 词拒绝策略），圈子帖与消息同样覆盖。因此 Tweet 的文本安全已有兜底，缺口仅剩：图片审核未接入 + handler 冗余空转。

**方案**：
1. handler 注入 `ITweetRepository`，`GetByIdAsync` 取真实推文，把 `Content` 与媒体 URI 传给审核服务（服务仍是占位放行实现，但链路真实，后续替换实现即生效）。
2. 或直接删除该 handler 的占位调用（写侧已过滤）——**推荐 1**，为图片审核预留真实入口。
3. 图片审核服务替换为真实实现（第三方 API）时，命中策略由占位放行改为自动 Reject（状态 + `TweetNotification`），列为后续增强。

**涉及文件**：TweetCreatedEventHandler ｜ **工作量**：S ｜ **验证**：创建含敏感词推文被写侧拒绝（现状已生效）；无回归。

---

### 🟠 R-06 浏览量无去重 + 详情页叠加计数

**位置**：`Commands/Tweets/RecordTweetViewCommandHandler.cs:25`（无条件 +1）；`Queries/Tweets/GetTweetDetailQueryHandler.cs:28-32`（GET 详情也 +1 且每次 `RecalculateHotScore`）

**现状**：`POST /{tweetGuid}/view` 每次 +1，无去重（设计文档 2.2.5 的 `TweetViewLogs` 表不存在）；GET 详情页每次访问再 +1 且无 `%10` 节流（view 命令有）。双路径叠加，热度分数失真，详情查询高频写库。

**方案（Redis 轻量去重，不建表）**：
1. 计数唯一入口收敛为 `POST /{tweetGuid}/view`：**移除** `GetTweetDetailQueryHandler` 中的 `IncrementViewCount + RecalculateHotScore`（保留可见性/圈子检查）。前端在详情页加载时显式调一次 view 端点。
2. `RecordTweetViewCommandHandler` 去重：`IDistributedCache`（Program 已注册 `AddRedisDistributedCache`）按用户+推文写键 `tweet:viewed:{userId}:{tweetGuid}`，TTL 24h，`SetString` 后首次才 `IncrementViewCount`；`%10` 节流的 `RecalculateHotScore` 保留。
3. 24h 内重复请求返回成功但不计数（幂等语义，前端无需改动）。

**涉及文件**：RecordTweetViewCommandHandler、GetTweetDetailQueryHandler ｜ **工作量**：S ｜ **验证**：同一用户 10 次 view 只 +1；不同用户各 +1。

---

### 🟠 R-07 分页 TotalCount 与过滤不一致

**位置**：`GetUserTweetsQueryHandler.cs:22`、`GetTrendingQueryHandler.cs:19`、`GetCommunityFeedQueryHandler.cs:21`（R-02 修复后）

**现状**：items 在内存中按可见性/状态过滤，但 `TotalCount` 来自未过滤计数 → 计数虚高、过滤后可能出现空页。

**方案**：三处过滤全部下沉仓库层（R-02/R-03 已含），Count 与列表方法用同一 `Where` 条件；handler 不再做内存过滤（`GetUserTweetsQueryHandler` 的草稿/圈子判断改为仓库参数：`GetByAuthorAsync(authorGuid, viewerId, page, pageSize)` + 草稿仅本人、圈子帖过滤）。

**涉及文件**：TweetRepository（GetByAuthorAsync/GetTimelineAsync/GetTrendingAsync/GetCommunityFeedAsync + 对应 Count）及 3 个 handler ｜ **工作量**：M ｜ **验证**：他人视角 TotalCount 不含 Private；本人视角含草稿。

---

### 🟠 R-08 草稿无独立列表入口

**位置**：`APIs/TweetsApi.cs`（POST /draft、PUT /{tweetGuid} 均只操作单条）；`Queries/Tweets/` 无草稿查询

**现状**：能存、能改、能删，但"我的草稿列表"只能靠 `GET /user/{userGuid}` 中"本人+Draft"的特殊分支翻找，前端做不了草稿箱。

**方案**：新增 `GET /api/tweets/drafts`（字面量路由，注册在 `/{tweetGuid}` 之前）→ `GetMyDraftsQuery(UserId, Page, PageSize)` → 仓库新增 `GetByAuthorAndStatusAsync(authorGuid, TweetStatus.Draft, page, pageSize)` + `CountByAuthorAndStatusAsync`。

**涉及文件**：TweetsApi + 新查询 2 文件 + TweetRepository 2 方法 ｜ **工作量**：S ｜ **验证**：存 2 条草稿 → GET /drafts 返回 2 条；发布后只剩 1 条。

---

### 🟠 R-09 在线/离线状态推送未接线

**位置**：`MessageDeliveryService.NotifyUserStatusChangedAsync`（零调用）；`UserOnlineEventHandler.cs`/`UserOfflineEventHandler.cs`（空壳）；`MessageHub.OnConnectedAsync/OnDisconnectedAsync`

**现状**：`IMessageClient` 有 `UserOnline/UserOffline` 契约，`UserStatusCacheService` 正确维护 Redis 在线状态，但**没有任何代码推送状态变更**给好友——好友在线状态只能靠前端轮询。

**方案（Hub 内联推送，与现有模式一致）**：
1. `MessageHub.OnConnectedAsync` 末尾：`await _deliveryService.NotifyUserStatusChangedAsync(userId, online: true, ct)`。
2. `OnDisconnectedAsync` 末尾：`NotifyUserStatusChangedAsync(userId, online: false, ct)`（注意仅在最后一条连接断开时推 offline，`_connectionCommandService.GetConnectionCountAsync` 判断）。
3. 两个域事件处理器保留空壳并加注释说明推送已在 Hub 内联（避免未来误改双推）。

**涉及文件**：MessageHub（2 处）+ 2 个事件处理器注释 ｜ **工作量**：S ｜ **验证**：A/B 在线互相关注，B 断线 → A 收到 `UserOffline(B)`。

---

### 🟠 R-10 Tweet 系列表无建表脚本

**位置**：`Message.Infrastructure/Sql/`（仅 CommunitySchema.sql，5 张社区表）；DDL 只存在于 `Message.Domain/Tweet功能集成开发文档.md` 第 2.2 节

**现状**：Message 无 EF Migrations、无 EnsureCreated，新环境建表只能手工从文档复制；且文档表结构与 EntityConfig 已漂移（如 `Tweets.MediaUrls` JSON 列、`TopicGuids` text 列、枚举字符串列）。

**方案**：
1. **新建 `Message.Infrastructure/Sql/TweetSchema.sql`**：`Tweets`/`Comments`/`TweetInteractions`/`TweetAuditLogs`/`TweetReports`/`TweetNotifications` 六张表，字段/索引**严格对照 EntityConfig**（关键点：`Tweets.MediaUrls` text(JSON)、`Hashtags` text(逗号串)、`TopicGuids` text、`TweetStatus`/`Visibility` text、`CreateTime` 倒序索引、`(TweetStatus, CreateTime)` 复合索引、`HotScore` 倒序索引；`TweetNotifications` 的 `(UserGuid, IsRead, CreateTime)` 复合索引）。
2. **全量兜底**：建议从运行中的库导出权威 schema（`pg_dump --schema-only`）生成 `MessageSchema.sql`（含消息/会话/群组/好友/附件 + Tweet 系列 + 社区），作为部署唯一入口；CommunitySchema.sql 保留或并入。
3. 文档 2.2 节标注"以 `Sql/` 脚本为准"。

**涉及文件**：Sql/ 新增 1-2 脚本 ｜ **工作量**：M ｜ **验证**：空库执行脚本 → 服务启动 → 各模块 CRUD 冒烟通过。

---

### 🟡 R-11 空壳事件处理器与无处理器事件

**位置**：`DomainEventHandlers/` 全部 28 个文件

**现状**：8+ 处理器空壳（MessageSent/MessageRead/MessageRecalled/MessageForwarded/GroupCreated/GroupMemberJoined/UserOnline/UserOffline，只记日志）；10 个事件**无处理器**：`GroupDissolved`、`GroupMemberRemoved`、`GroupOwnershipTransferred`、`FriendshipCreated/Accepted/Rejected`、`FileDownloaded`、`TweetPublished`、`TweetViewCountUpdated`、`MessageReceived`。

**影响**：群解散/被移出/群主变更时成员零通知；好友请求接受/拒绝无通知；推文发布无事件出口（未来做发布通知/动态需要）。

**方案（按需分两批）**：
1. 第一批（低成本高价值）：`GroupDissolvedEventHandler` + `GroupMemberRemovedEventHandler` —— 写 `TweetNotification`（站内通知）+ 经 `MessageDeliveryService` 推送 `SystemNotice`（若无契约则仅落通知，前端列表轮询）。
2. 第二批：补 `MessageReceivedEvent` 语义说明（发送成功即已落库，接收方推送由 R-01 的 DeliverMessageAsync 承担，该事件仅作审计用途，保持空壳）；其余事件标注"审计用途，无需处理"。
3. 空壳处理器统一加注释说明其职责边界，防后续误改。

**工作量**：S-M ｜ **验证**：解散群 → 成员通知列表出现记录。

---

### 🟡 R-12 圈子发现与话题管理缺失

**位置**：`APIs/CirclesApi.cs`（无 GET /）；`APIs/TopicsApi.cs`（仅创建/列表/帖子流）

**现状**：新用户只能靠邀请码/链接/直邀进圈，无公开圈子列表/搜索；话题只能创建，错别字话题无法停用（`Topic.Deactivate()` 已实现但无调用方）。

**方案（需产品确认后实施）**：
1. `GET /api/circles?keyword=&page=&pageSize=`：`Status == Active` 的圈子列表 + 名称模糊搜索（`EF.Functions.Like`），返回 `CircleDto` 精简版（不含成员明细）。注：Circle 无"允许被发现"开关字段，默认所有 Active 圈子可被发现；如需控制，先加 `IsDiscoverable` 字段（涉及实体+配置+建表脚本，工作量 +S）。
2. `PUT /api/topics/{topicGuid}`（改名/简介）+ `DELETE /api/topics/{topicGuid}`（停用）：仅创建者本人或 Admin 可操作；停用后 `GET /api/topics` 与帖子流过滤 `IsActive`。

**工作量**：M ｜ **验证**：新用户可搜到圈子并凭 ID 申请加入（或直接展示详情）。

---

### 🟡 R-13 list.md 文档漂移

**位置**：`Message.Domain/list.md` T005/T006/T007

**现状**：任务清单声称 2026-03-09 已完成"已读回执（MessageReadReceipt/GroupReadReceipt）""离线消息（OfflineMessage/OfflineMessageConfig/UserOfflineStorage）""消息加密（MessageEncryption/EncryptionKey/SessionKey）"，但**这些实体全仓不存在**（重构时被移除或从未提交），`EncryptionAlgorithm` 枚举是唯一残留（零使用）。

**方案**：更新 list.md——T005/T006/T007 状态改为"❌ 已移除（2026-08 重构）"，标注替代方案：已读 = 消息级 `MarkAsRead` + 会话未读数；离线 = `OnConnectedAsync` 未读补推；加密 = 未实现（按需立项）。**不建议补实现**（IM 加密端到端需求未明确）。

**工作量**：S ｜ **验证**：文档与代码事实一致。

---

### 🟡 R-14 `RecallConfig` 死代码

**位置**：`Entities/Recall/RecallConfig.cs`（63 行，全仓零引用）；`Message.Recall` 硬编码 `MessageRecall.CanRecall(SentTime)`（2 分钟）

**方案（二选一，建议 A）**：
- **A（推荐）删除**：撤回时限 2 分钟固定值符合常见 IM 产品，无配置诉求；删除 `RecallConfig` 实体（含 MessageRecall 中引用检查）。
- **B 接线**：撤回时限改为 `RecallConfig` 静态默认 + 管理端点（Admin 可配私聊/群聊分钟数），实体方法 `Recall` 增加 config 参数。仅当产品明确需要差异化时限时选 B。

**工作量**：S ｜ **验证**：删除后全 sln build 0 警告。

---

### 🟡 R-15 `MessagePush`/`MessageOther` 类型

**位置**：`Enums/MessageType.cs`；`SendMessageCommandHandler.cs:31`

**现状**：两种类型发送即 `NotSupportedException`（映射 400）。`MessagePush`（系统推送消息）若未来做"系统公告进会话"会用上；`MessageOther` 无场景。

**方案**：维持现状，文档标注"MessagePush 预留（系统消息场景），接入时在 handler 补创建分支 + DTO 展示适配"。`MessageStatus.Pending/Delivered/Failed` 同理标注（发送链路直接 Sent，Delivered 可由 SignalR 送达回执未来接入）。

**工作量**：S（文档）｜ **验证**：无代码变更。

---

### 🟡 R-16 邮件/本地化占位

**位置**：`Services/DefaultEmailSender.cs`、`TweetApprovedEventHandler.cs:33`（邮件占位日志）；`ILocalizationService.cs`（预留）

**现状**：审核通过/驳回无真实邮件；通知文案中文硬编码。已文档化（Tweet 文档 6.3/10）。

**方案**：维持占位；接入 `NotEmail`（SMTP/IMAP 已就绪）时替换 `DefaultEmailSender` 注册即可，handler 无需改动（事件处理器已调 `IEmailSender` 接口）。`ILocalizationService` 在通知模板化时再启用。

**工作量**：- ｜ **验证**：无。

---

### 🟡 R-17 `EncryptionAlgorithm` 死枚举

**位置**：`Enums/EncryptionAlgorithm.cs`

**方案**：随 R-13 一并删除（或保留作加密模块立项占位——**建议删除**，避免死代码蔓延，git 历史可回溯）。

**工作量**：S ｜ **验证**：全 sln build 0 警告 0 错误。

---

## 4. 新增端点全表（实施后增量）

| API 组 | 新增端点 | 归属修复 | 说明 |
|---|---|---|---|
| `/api/notifications` | GET / ｜ GET /unread-count ｜ PUT /read-all ｜ PUT /{notifyGuid}/read | R-04 | 全部 RequireAuthorization，仅本人数据 |
| `/api/tweets` | GET /drafts | R-08 | 字面量路由，先于 /{tweetGuid} 注册 |
| `/api/circles` | GET /（列表+搜索） | R-12 | 仅 Active 圈子 |
| `/api/topics` | PUT /{topicGuid} ｜ DELETE /{topicGuid} | R-12 | 创建者/Admin |

## 5. 数据模型

### 5.1 TweetNotifications（R-04 依赖，对照 TweetNotificationConfiguration.cs）

| 列 | 类型 | 约束 |
|---|---|---|
| Id | uuid | PK，Guid.CreateVersion7 |
| UserGuid | uuid | NOT NULL，通知接收者 |
| Type | text | NOT NULL（NotificationType 字符串） |
| Title | varchar(255) | NOT NULL |
| Content | varchar(2000) | NOT NULL |
| RefType | varchar(20) | 可空（"Tweet"/"Comment"/...） |
| RefGuid | uuid | 可空 |
| IsRead | boolean | NOT NULL 默认 false |
| CreateTime | timestamptz | NOT NULL |

索引：`(UserGuid, IsRead, CreateTime DESC)`、`(Type)`

### 5.2 Tweets 列名要点（R-10 建表脚本对照）

`TweetGuid`(PK) ｜ `AuthorGuid` ｜ `Content` varchar(2000) ｜ `MediaUrls` text(JSON) ｜ `LinkMetadata` text(JSON) ｜ `Hashtags` text(逗号串) ｜ `TopicGuids` text ｜ `TweetStatus` text ｜ `Visibility` text ｜ `IsPinned` bool ｜ 计数列 `ViewCount/LikeCount/CommentCount/ShareCount/CoinCount/FavoriteCount` bigint/int ｜ `HotScore` bigint ｜ `CircleGuid` uuid 可空 ｜ `AuditReason` varchar(500) ｜ `PublishTime` ｜ `CreateTime` ｜ `UpdateTime`
索引：`AuthorGuid`、`(TweetStatus, CreateTime DESC)`、`HotScore DESC`、`CreateTime DESC`、`Hashtags DESC`、`CircleGuid`

### 5.3 建表脚本清单

| 脚本 | 表 | 状态 |
|---|---|---|
| `Sql/CommunitySchema.sql` | Circles/CircleMembers/CircleInvitations/Topics/UserFollows | ✅ 已有 |
| `Sql/TweetSchema.sql`（新增） | Tweets/Comments/TweetInteractions/TweetAuditLogs/TweetReports/TweetNotifications | R-10 |
| `Sql/MessageSchema.sql`（新增，pg_dump 生成） | 消息/会话/群组/好友/附件 全量兜底 | R-10 |

## 6. 安全影响分析

| 修复 | 安全收益 |
|---|---|
| R-02/R-03/R-07 | 修复 Private 泄露（feed）、Followers 语义生效；分页不再暴露不可见内容计数 |
| R-04 | 通知端点强制 UserGuid=当前用户（防越权读他人通知）；单条已读校验归属 |
| R-06 | 浏览计数防刷（Redis 去重），降低热度分数字面操纵 |
| R-12 | 圈子列表仅 Active；话题停用仅创建者/Admin（权限校验复用 `currentUser.IsAdmin()` + 创建者比对） |
| 无新增 | 不引入任何绕过鉴权/公开数据面（新端点全部 `RequireAuthorization`） |

## 7. 分阶段实施 checklist

### 阶段 0：快速止血（1 个 PR，全部 S 级改动）
- [ ] R-01 REST 三链路推送（同步改 Message.Tests Moq 构造）
- [ ] R-02 Feed 可见性过滤（仓库层 + Count 同步）
- [ ] R-03 Followers 接入关注关系（策略 + 4 查询）
- [ ] R-05 TweetCreatedEventHandler 接真实内容
- [ ] R-06 浏览量去重 + 移除详情页自动计数
- [ ] R-07 三处查询过滤下沉 + TotalCount 对齐
- [ ] R-17 删除 EncryptionAlgorithm 死枚举
- **验证**：`dotnet build NotBlog.sln`（0 警告 0 错误，先停运行中的 Message 进程）→ `dotnet test Message.Tests` 全绿 → REST/SignalR 手动冒烟

### 阶段 1：核心缺口补齐（1-2 个 PR）
- [ ] R-04 通知读侧（4 端点 + DbSet + 删空文件）
- [ ] R-08 草稿列表端点
- [ ] R-09 在线状态推送
- [ ] R-10 建表脚本（TweetSchema.sql 手写 + MessageSchema.sql pg_dump）
- **验证**：同上 + 空库部署演练（执行脚本 → 启动 → 全模块冒烟）

### 阶段 2：增强项（按产品优先级）
- [ ] R-11 群解散/移出成员通知（第一批）
- [ ] R-12 圈子发现 + 话题管理（需产品确认 IsDiscoverable）

### 阶段 3：决策与清理
- [ ] R-13 list.md 更新 ｜ R-14 RecallConfig 删除（方案 A） ｜ R-15 文档标注 ｜ R-16 接入 NotEmail 时替换

## 8. 兼容与废弃时间表

| 变更 | 兼容性 |
|---|---|
| 全部修复 | **加性**：新端点、新过滤、新推送，不修改现有端点契约 |
| `DELETE /{id}`（消息） | 保持"撤回"语义不变，仅增强推送 |
| `GET /tweets/user/{uid}` | 行为不变（R-07 只修计数准确性，不改变返回内容） |
| `GET /tweets/{id}` 浏览量 | **行为变化**：详情 GET 不再自动 +1，前端需显式调 `POST /view`（一次性改动，旧前端计数停更但功能不受损） |
| `TweetNotifications` 表 | 建表用 `CREATE TABLE IF NOT EXISTS`，已手工建表的环境无影响 |
| 删除 `RecallConfig`/`EncryptionAlgorithm` | 纯内部符号，无外部契约，git 可回溯 |

## 9. 验证方案汇总

```bash
# 1. 构建（先停运行中的服务，避免 MSB3021 文件锁）
taskkill /F /IM Message.Web.API.exe 2>nul   # 或停 Aspire
dotnet build NotBlog.sln                      # 目标：0 警告 0 错误

# 2. 单元测试（NUnit，改 handler 构造函数后必须跑）
dotnet test Message.Tests

# 3. 手动冒烟（服务 + Postgres/Redis/RabbitMQ 起）
#    R-01: REST 发消息 → 对端 SignalR 收到 ReceiveMessage；REST 撤回 → MessageRecalled
#    R-02: 关注用户发 Private 推文 → 不出现在 /follows/feed
#    R-03: 关注者可见 Followers 推文；取关后不可见
#    R-04: 评论触发通知 → GET /api/notifications 可见 → 已读后 unread-count=0
#    R-06: 同用户 10 次 POST /view → ViewCount 只 +1
#    R-08: 存草稿 → GET /api/tweets/drafts 返回
#    R-09: B 断线 → A 收到 UserOffline(B)

# 4. EventBus 冒烟（涉及 R-04 通知事件时）
#    RabbitMQ 15672 管理 API 建队列绑定 → 发布 → GET 核对
```
