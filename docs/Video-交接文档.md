# NotBlog Video 服务交接文档（2026-08-29）

> 覆盖 Video 服务本开发周期的全部变更：**互动计数补全与作者通知（v1.0 实施）**、评论赞踩模型
> （ReviewQuote）、领域构造缺陷修复，以及配套文档。供后续接手人快速了解现状、契约与遗留事项。
> 依据文档：`Video-互动计数与作者通知-实施文档.md`（v1.0）、`Video-热点-设计方案.md`（待实施）。

---

## 1. 总体概况

| 事项 | 状态 | 说明 |
|---|---|---|
| 互动计数补全（VideoQuote） | ✅ 完成 | 收藏 Stars 接入增/减；点赞/投币/分享/观看维持现状 |
| 视频点赞/投币 → 作者通知 | ✅ 完成 | `VideoInteraction` 集成事件，Redis SetNX「首次」去重，仅首次通知 |
| 评论/回复评论 → 作者通知 | ✅ 完成 | `VideoCommentPublished` 集成事件，顶级→作者、回复→被回复者，自互动消费端跳过 |
| 评论赞踩 ReviewQuote | ✅ 完成 | 新值对象（Like/Dislike）替换评论原 VideoQuote；Redis 哈希增量 |
| 安全发布统一 | ✅ 完成 | `EventPublishing.PublishSafelyAsync`（含既有 VideoPublished 切换） |
| 领域构造缺陷修复 | ✅ 完成 | `VideoProtectedTime.Create(null,null)`、`VideoReview` 私有构造空内容初始化（测试暴露） |
| 单元测试 | ✅ 完成 | 新增 `Video.Tests` 项目，27/27 通过；Solution 构建 0 错误；Message.Tests 213/213 |
| **数据库迁移** | ⚠️ **未应用** | `VideoInteractionNotifications` 已生成；PG 未运行，**应用前先备份** |
| 前端接线（notblog-ui） | ⏸ 未做 | 4 个新通知类型的展示与视频详情跳转（文档 §9，非阻塞） |
| 视频热点榜 | 📋 仅设计 | `Video-热点-设计方案.md` 已出（公式/榜单服务/定时重建），**未实施** |

---

## 2. 本次开发内容（v1.0 实施）

### 2.1 计数与模型

- **`VideoReview`**：`VideoQuote` 挂点 → **`ReviewQuote`**（新值对象 `Like/Dislike`，原子增减、下限 0）；
  新增带 `reviewGuid` 的公开构造（评论 Id 由调用方提供，支撑通知追踪与 RequestId 幂等复用）。
- **`Videos.AddByVideoReview(reviewGuid, …)`**：签名加入评论 Id 参数（唯一调用点 `AddVideoReviewCommandHandler` 已同步）。
- **收藏计数**：`AddVideoToCollectionCommandHandler` / `RemoveVideoFromCollectionCommandHandler`
  在收藏关系提交后 `UpStars()/DownStars()` + `UpdateByQuoteAsync`（计数失败仅日志，幂等分支不重复计数）。
- **评论赞踩**：`QuoteVideoReviewCommandHandler` 收敛为 `upvote→Like / down→Dislike`（移除 ballot/share）；
  落库 + `IncrementReviewQuoteFieldAsync(reviewGuid, "like"|"dislike", ±1)` 增量写 Redis。
- **DTO/端点**：`VideoReviewResponse` 与评论列表/详情/互动查询输出 `Like/Dislike`；like 端点描述同步。

### 2.2 作者通知链路（跨服务）

```
Video.Web.API 发布侧                        Message.Web.API 消费侧
LikeVideoCommandHandler                    VideoInteractionIntegrationEventHandler
  1. 计数落库                              1. ActorUserId == TargetUserId → 跳过
  2. SetNX(video:interact:once:V:U:field)  2. VideoNotificationCopyBuilder 文案
     （仅 upvote/ballot，30 天，取消不删）    3. TweetNotification 落库（refType=Video）
  3. TargetUserId = Affiliated 首个非操作者  4. SignalR 实时推送（离线 /api/notifications 补拉）
  4. PublishSafelyAsync
AddVideoReviewCommandHandler                VideoCommentPublishedIntegrationEventHandler
  1. 顶级评论 → TargetUserId=作者             同上（refType=VideoReview，refGuid=ReviewGuid）
  2. 回复 → TargetUserId=被回复评论作者         文案区分「视频新评论 / 评论被回复」
  3. 预览截 50 字（CommentPreview）
```

- **event 契约**：`VideoInteractionIntegrationEvent`（InteractionType: VideoLiked/VideoCoined）、
  `VideoCommentPublishedIntegrationEvent`（RootReviewGuid=null 判顶级）；两者均带 `TargetUserId`，消费端不反查视频域。
- **注册**：`[EventBusName]` 特性对齐，Message 侧 `AddEventBus(Assembly)` 自动订阅，无手动注册。
- **新增文件**：Video 侧 `EventPublishing / ReviewQuote / 2 个集成事件`；
  Message 侧 `VideoInteractionIntegrationEventHandler / VideoCommentPublishedIntegrationEventHandler /
  VideoNotificationCopyBuilder`；`NotificationType` 追加 4 类（VideoLiked/VideoCoined/VideoCommentAdded/VideoCommentReplied）。

### 2.3 领域缺陷修复（测试暴露，生产同路径会命中）

| 文件 | 缺陷 | 修复 |
|---|---|---|
| `VideoProtectedTime.cs` | `Create(null,null)` 对 null 入参调 `.Value` 抛「Nullable must have a value」 | null 视为未设置（MinValue），兼容 `VideoControl.VideoControlBuilder()` |
| `VideoReview.cs` | 私有构造 `CreateText("")` 必抛（CreateText/CreateDefault 拒绝空内容）→ 实体无法构造 | 私有构造占位 `new ReviewContent()`，真实内容由公开构造 `CreateDefault` 覆盖 |

### 2.4 测试与验证

- 新增 **`Video.Tests`**（NUnit + Moq，已加入 Solution）：值对象（ReviewQuote/VideoQuote 原子增减与下限）、
  命令处理器（Like/AddReview/QuoteReview：事件发布、去重、归属、预览截断、缓存增量断言），共用 `Commands/VideoFixtures.cs` 基建；
- 结果：Video.Tests 27/27；Solution 构建 0 错误；Message.Tests 213/213；
- 经验已沉淀至项目记忆（领域构造缺陷暴露方式、Redis 去重升级路径）。

---

## 3. 遗留事项

| 事项 | 优先级 | 说明 |
|---|---|---|
| **应用数据库迁移** `VideoInteractionNotifications` | 高 | `dotnet ef database update`；存量评论 `c_Upvote→c_Like / c_Down→c_Dislike` 回填在迁移内完成；**先备份** |
| **前端通知接线** | 中 | notblog-ui 对 4 个新 `NotificationType` 的展示与跳转（视频详情需 VideoGuid 路由参数），以及评论赞踩字段（Like/Dislike）展示适配 |
| **热点榜实施** | 中 | 设计已定稿（`Video-热点-设计方案.md`）：`VideoHeatFormula` + ZSet 榜单 + 10 分钟重建；需先完成收藏 Stars 存量确认 |
| **互动去重依赖 Redis** | 低 | `video:interact:once` 30 天 SetNX 清库会重复通知；升级路径为「用户-视频-互动」明细表唯一索引 |
| **多作者通知主作者** | 低 | `Affiliated` 集合仅通知首个非操作者；后续可扩展逐作者（事件带 `List<Guid>`） |
| **评论赞踩通知** | 低 | 用户需求限定为数据；如对齐 Markdown，可加 `VideoReviewLiked/Disliked` 事件（字段已具备） |

---

## 4. 相关系联

- **文档集**：`Video-项目文档.md`（部署）、`Video-开发文档.md`（开发）、`Video-互动计数与作者通知-实施文档.md`（本次实现依据）、`Video-热点-设计方案.md`（待实施）；
- **跨服务**：Message 服务 `Application/IntegrationEvents/EventHandlers/` 下的 Video 消费者与本服务一一对应；
- **改动未提交**：本周期后端改动（见 `git status`：13 改 + 8 新 + 迁移 + 测试）尚未 commit/push，前端 notblog-ui 关联改动未做。