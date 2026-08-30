# Markdown 博客服务 · 开发文档

> 面向开发者的服务设计文档：架构分层、领域模型、命令/查询/事件链路、关键实现要点与扩展指南。

---

## 1. 服务定位

**Markdown 博客服务** 是 NotBlog 平台的博客内容服务，负责 Markdown 博客文章的完整生命周期：

- **文章管理**：创建 / 更新 / 软删除 / 历史版本快照与还原 / 审核状态机（草稿 → 待审核 → 通过 / 驳回）。
- **文件化正文存储**：数据库仅存文件元数据（FileId / FileUri / FileSize / FileExt / SHA-256），正文通过 `IMarkdownContentStore` 读写（生产 FileDev gRPC / 开发本地磁盘）。
- **交互体系**：浏览 / 点赞 / 取消点赞 / 收藏 / 分享 / 投币（硬币），文档级与评论级交互计数，均带唯一约束防重复（幂等）。
- **评论体系**：顶级评论 / 子评论（回复）/ 评论点赞 / 评论踩 / 评论配图（JSONB）。
- **热点榜**：互动 50% + 浏览 30% + 时间衰减 20% 的热度公式，Redis ZSet 直读 + 定时全量重建 + DB 降级。
- **收藏体系**：用户收藏聚合根 + 标签分类管理 + 标签库复用建议。
- **跨服务通知**：通过 RabbitMQ 集成事件（点赞 / 投币 / 评论点赞 / 评论踩 / 评论发布 / 文章发布）通知 Message 服务站内推送。

---

## 2. 架构分层

```
┌─────────────────────────────────────────────────────────┐
│ Markdown.Web.API（表现/应用层）                            │
│  Minimal API 端点 → 命令/查询(NotMediator) → 仓储 → 事件发布 │
├─────────────────────────────────────────────────────────┤
│ Markdown.Infrastructure（基础设施层）                      │
│  EF Core DbContext / 仓储实现 / 幂等管理 / 迁移            │
├─────────────────────────────────────────────────────────┤
│ Markdown.Domain（领域层）                                  │
│  实体 / 值对象 / 枚举 / 领域事件 / 仓储与服务接口 / 热度公式  │
└─────────────────────────────────────────────────────────┘
```

### 2.1 关键分层规则

- **聚合根唯一暴露**：`MarkDown` 是文档聚合根，`MarkReview`（评论）、`OldMarkDown`（历史版本）均为聚合内实体，不暴露独立仓储；`MarkFavorite` 是收藏聚合根，单独暴露 `IMarkFavoriteRepository`。所有聚合内实体的操作均通过聚合根方法完成（如 `SoftDeleteReview` 递归软删整棵评论子树）。
- **正文文件化**：`MarkDown` 实体不含 `Content` 字段，只保存文件元数据。正文通过 `IMarkdownContentStore` 保存/读取/删除，数据库列 `FileId` 为文件存储后端标识。
- **值对象内聚**：`MarkQuote`（文档交互统计 6 字段）与 `ReviewQuote`（评论交互统计 4 字段）均使用 `Interlocked`/`Volatile` 线程安全计数，作为 owned 实体映射为独立列；`ReviewImage` 评论配图作为 owned 集合整体序列化为 JSONB 列。
- **错误信息防泄漏**：统一业务异常映射 `MarkdownApiExceptionHandler`（KeyNotFoundException→404、UnauthorizedAccessException→403、InvalidOperationException/ArgumentException→400），其余交由全局脱敏中间件返回 500。

---

## 3. 领域模型

### 3.1 实体总览

| 实体 | 类型 | 说明 | 唯一约束 |
| --- | --- | --- | --- |
| `MarkDown` | 聚合根 | 文档（仅存文件元数据 + 交互统计） | — |
| `MarkReview` | 聚合内实体 | 评论（含自引用父子关系、配图、ReviewQuote） | — |
| `OldMarkDown` | 聚合内实体 | 历史版本快照（全文存 DB，含哈希） | — |
| `MarkFavorite` | 聚合根 | 用户收藏（标签 JSON 列） | `(UserGuid, MarkDownGuid)` |
| `MarkFavoriteTag` | 实体 | 收藏标签库（标签复用） | `(UserGuid, Tag)` |
| `MarkCoin` | 实体 | 投币流水（审计 + 计数来源） | `(MarkDownGuid, UserId)` |
| `MarkDocumentLike` | 实体 | 文档点赞记录 | `(MarkDownGuid, UserId)` |
| `MarkReviewLike` | 实体 | 评论点赞记录 | `(MarkReviewGuid, UserId)` |
| `MarkReviewDislike` | 实体 | 评论踩记录 | `(MarkReviewGuid, UserId)` |

### 3.2 值对象

| 值对象 | 字段 | 说明 |
| --- | --- | --- |
| `MarkQuote` | Love / Favorite / Share / Coin / View / HeatScore | 文档交互统计，线程安全计数，热度分由公式计算写回 |
| `ReviewQuote` | Love / View / Reply / Dislike | 评论交互统计 |
| `ReviewImage` | ImageUrl / ImageName | 评论配图引用，无独立身份，JSONB 存储 |

### 3.3 枚举

| 枚举 | 值 | 说明 |
| --- | --- | --- |
| `MarkDownAuth` | Public / Private / Protected / Admin / Root | 文档权限类型（`HasPermission` 判定：仅公开或作者可见） |
| `MarkReviewAuth` | Public / Private / Protected | 评论权限（非公开仅所有者可见） |
| `MarkStatus` | MarkDraft=15 / MarkPendingReview=16 / MarkApproved=17 / MarkRejected=18 | 审核状态机（显式数值保持与历史数据兼容） |
| `MarkdownInteractionType` | DocumentLiked / DocumentCoined / ReviewLiked / ReviewDisliked | 集成事件交互类型 |

### 3.4 聚合关系

```
MarkDown (聚合根)
 ├─ MarkReviews  (1..N)            顶级评论（MarkAggregateRootGuid = null）
 │    └─ MarkReviews (1..N)        子评论（自引用，MarkAggregateRootGuid = 父评论Guid）
 ├─ OldMarkDowns (1..N)            历史版本快照
 └─ MarkQuote (owned)              文档交互统计

MarkFavorite (聚合根)
 ├─ Tags（内存集合 ⇄ TagsJson 文本列）
 └─ MarkFavoriteTag（标签库，独立聚合）
```

### 3.5 关键领域行为

- `MarkDown.CreateHistorySnapshot(content)`：更新/还原前保存旧正文快照；内容哈希与现有历史相同时跳过（防历史版本无限膨胀，P1-8）。
- `MarkDown.SoftDeleteReview(reviewGuid)`：BFS 递归收集后代评论，整棵子树软删除（保留树结构供审计）。
- `MarkDown.AddChildReview(parentReviewGuid, childReview)`：聚合根统一入口，内部设置父评论 Guid、更新父评论回复计数。
- `MarkDown.SubmitForReview/Approve/Reject`：审核状态机流转，非法状态变更抛 `InvalidOperationException`（→ 400）。
- `MarkDown.HasPermission(userGuid)`：垂直权限控制，与 API 层的"审核门控"配合实现双层防护。
- `MarkdownHeatFormula.Calculate(quote, createdAt, now)`：`0.5×log10(1+Love+2×Favorite+3×Share+5×Coin) + 0.3×log10(1+View) + 0.2×exp(-ageDays/7)`。

---

## 4. 应用层（CQRS）

### 4.1 命令（写侧）

| 命令 | Handler 要点 |
| --- | --- |
| `CreateMarkdownCommand` | 计算 SHA-256 → 保存正文到文件存储 → Builder 创建实体（仅元数据） → 仓储落库 → 发布 `MarkdownCreatedEventData` |
| `UpdateMarkdownCommand` | 所有权校验 → 旧正文快照 → 保存新正文 → 更新元数据/标签/封面 → 清理旧文件（失败仅告警） |
| `DeleteMarkdownCommand` | 幂等（IdempotencyKey）→ 所有权校验 → 软删除 |
| `RestoreMarkdownCommand` | 作者校验 → 当前内容快照 → 历史内容重新保存为文件 → 更新元数据引用 |
| `SubmitMarkdownCommand` | 作者校验 → 草稿/驳回 → 待审核 |
| `ApproveMarkdownCommand` / `RejectMarkdownCommand` | 作者或管理员校验 → 状态流转 |
| `CreateMarkReviewCommand` | 幂等 → 聚合根添加评论 → 发布 `MarkdownCommentPublished`（顶级评论通知作者）+ `MarkReviewCreated` |
| `AddChildReviewCommand` | 幂等 → 聚合根添加子评论 → 发布 `MarkdownCommentPublished`（子评论通知父评论作者）+ `ChildReviewAdded` |
| `UpdateMarkReviewCommand` / `DeleteMarkReviewCommand` | 通过聚合根更新内容 / 软删除 |
| `AddFavoriteCommand` | 幂等 → 已收藏合并标签 / 未收藏创建并收藏计数 +1（同事务，唯一约束兜底并发） → 记录标签库使用 → 热度刷新 |
| `RemoveFavoriteCommand` | 幂等 → 取消收藏并计数 -1 |
| `UpdateFavoriteTagsCommand` | 幂等 → 覆盖式更新标签 + 记录标签库使用 |

### 4.2 查询（读侧）

| 查询 | 说明 |
| --- | --- |
| `MarkdownListQuery` | 分页 / 按标签 / 按用户；仅 `MarkApproved` 且（公开 或 作者本人），正文文件化故列表仅轻量投影；带标签过滤时先取候选行投影过滤再回查完整实体 |
| `MarkdownSearchQuery` | 名称模糊搜索（正文已文件化不参与搜索；如需全文检索后续引入 PG tsvector） |
| `MarkFavoriteListQuery` | 按用户 + tag（`EF.Functions.Like` 精确匹配 JSON 数组元素）分页；批量联查文章名，过滤已删除文章 |
| `MarkFavoriteTagsQuery` | 标签库：按 UseCount 降序、LastUsedAt 次之，支持关键字过滤 |

### 4.3 行为（管道）

- `LoggerBehavior`：日志记录命令/查询执行。
- `TransactionBehavior`：事务包裹（已验证存在，用于跨实体一致性提交）。

---

## 5. 幂等性设计

基于 `ClientRequest` 表 + `ClientRequestId` 主键唯一约束（`RequestManagement` 实现）：

1. **原子占位**：直接 `INSERT` ClientRequest 记录，依赖主键唯一约束做并发去重（TOCTOU 安全）。
2. **赢家**：占位成功 → 执行业务 → 将响应 JSON 写入 `ResponseJson`。
3. **输家**：捕获唯一键冲突 → 轮询（10 次 × 100ms）读取赢家响应并直接返回，保证重试拿到与首次一致的结果。

- 幂等 Key 来源：请求头 `Idempotency-Key`（缺失时回退新生成的 Guid，向后兼容）。
- `ClientRequestCleanupService`：每 24h 清理超过 7 天的幂等记录（`ExecuteDelete`，不加载实体）。
- 写侧命令（创建评论 / 子评论 / 收藏 / 取消收藏 / 改标签 / 删除文章等）均包裹于 `ExecuteIdempotentAsync`。

---

## 6. 交互幂等（一次性约束）

所有交互计数均采用 **"记录表 + 唯一约束 + 计数同事务提交"** 模式：

```
点赞/投币/踩
  ├─ 插入交互记录（MarkDocumentLike / MarkCoin / MarkReviewLike / MarkReviewDislike）
  ├─ 内存计数 +N（MarkQuote / ReviewQuote）
  ├─ SaveChanges 提交（记录与计数同一事务，原子一致）
  └─ 唯一约束冲突（23505）→ 整批回滚 + 内存计数回滚 → 幂等返回当前计数（IsFirst=false）
```

- `InteractionResult(Count, IsFirst)`：`IsFirst=true` 表示本次真正产生了新的交互记录，**仅在此分支发布作者通知事件**（防止重复通知轰炸）。
- 浏览量：`ExecuteUpdate` 直接生成 UPDATE SQL 原子递增（不经过 EF 追踪，避免 GET 写放大）；已登录用户 24h Redis Set 防刷（匿名不防刷，Redis 不可用跳过）。

---

## 7. 跨服务集成事件

所有事件发布均通过 `EventPublishing.PublishSafelyAsync`：**总线故障仅记录日志，不拖垮已成功的业务**（尽力而为投递，如需严格保证可升级 Outbox）。

| 事件 | 触发点 | 消费端用途 |
| --- | --- | --- |
| `MarkdownCreatedEventData` | 创建文章成功 | Message 服务 → 好友/关注者聚合提醒 |
| `MarkdownInteractionIntegrationEvent`（`DocumentLiked` / `DocumentCoined` / `ReviewLiked` / `ReviewDisliked`） | 首次点赞/投币/评论点赞/评论踩 | Message 服务 → 作者站内通知 |
| `MarkdownCommentPublishedIntegrationEvent` | 发布评论（顶级→通知作者；子评论→通知父评论作者） | Message 服务 → 作者站内通知 |
| `MarkReviewCreatedIntegrationEvent` / `MarkReviewDeletedIntegrationEvent` / `MarkReviewLikedIntegrationEvent` / `ChildReviewAddedIntegrationEvent` | 评论创建/删除/点赞/子评论 | 兼容保留（通知统一走 `MarkdownInteraction`） |

> 消费端均在 Message 服务侧实现（`Message.Web.API` 下的事件处理器），Markdown.Web.API 内的同名处理器（`MarkdownCreatedEventHandler` 等）为占位/兼容实现。

---

## 8. 文件存储抽象

`IMarkdownContentStore`（接口：`SaveAsync / ReadAsync / DeleteAsync`）有两种实现，按配置 `MarkdownContent:Provider` 切换 DI 注册：

| 实现 | 配置 | 说明 |
| --- | --- | --- |
| `FileDevMarkdownContentStore` | 默认（未配置 Local） | gRPC 调用 FileDev 服务；`FILE_PUBLIC` 上传；携带**服务级 JWT**（固定服务账号 `11111111-...`，共享 `JWT_PRIVATE_KEY` 签发），不依赖用户 token；token 缓存至过期前 1 分钟，信号量串行刷新；gRPC 瞬时故障指数退避重试（3 次）；`NotFound` 视为确定性失败不重试 |
| `LocalMarkdownContentStore` | `MarkdownContent:Provider=Local` | 开发/单机：文件存 `ContentRoot/markdown-files/{guid:N}.md`；读路径仅取文件名防目录穿越 |

> 正文权限：文件以 `FILE_PUBLIC` 上传（FileDev 非私有可下载），内容权限由 **Markdown 服务层**把关（`HasPermission` + 审核门控）。

---

## 9. 热点榜

- **数据流**：PG 是唯一事实源（`MarkQuote.HeatScore` 列），Redis ZSet（`markdown:hot:all`）是可降级投影。
- **实时刷新**：交互端点写后调用 `UpdateScoreAsync` → 重算单文档热度 → 写 DB + 同步 ZSet。
- **定时重建**：`MarkdownHeatRebuildBackgroundService` 每 10 分钟全量重算兜底收敛（启动延迟 1 分钟），防重入（`Interlocked`）。
- **单飞保护**：空榜命中时通过 Redis `SETNX` 互斥锁（30s TTL）保证多实例只有一个执行重建，其余短暂等待后重读。
- **读路径**：Redis ZSet 直读 → miss 单飞重建 → Redis 故障降级 DB 实时计算（`ComputeFromDbAsync`，仅取已审核通过）。

---

## 10. 权限与可见性

| 场景 | 规则 |
| --- | --- |
| 读侧门控 | 文档必须：未删除 + `HasPermission(viewer)` +（`MarkApproved` 或 viewer 为作者）；不可见一律 404（不泄露存在性） |
| 写侧门控 | 命令/端点内校验所有权（`MarkUserGuid == userId`），越权抛 `UnauthorizedAccessException` → 403 |
| 评论可见性 | 公开评论所有人可见；私有/受保护评论仅所有者可见；已删除评论全量过滤 |
| 历史版本 | 权限校验 + 审核门控与文档详情一致；删除/还原仅作者 |

---

## 11. 后端基础设施（Program.cs 注册链）

```
builder.AddServiceDefaults()
→ AddJwtAuthentication(JwtOptions)          # JWT Bounder 认证（ValidateAudience/IssuerSigningKey）
→ AddNpgsqlDbContext<MarkDownDbContext>(MarkDownPostgres)
→ AddMarkdownInfrastructure()                # 仓储 + 幂等管理注册
→ AddNotMediator(Assembly)                   # 命令/查询/领域事件
→ AddMigration<MarkDownDbContext>()          # 启动自动迁移
→ EventBus 注册（DEBUG 手动 ConnectionFactory / Release Aspire） + AddEventBus
→ AddOpenApi / AddScalarApiReference          # API 文档 UI
→ AddExceptionHandler<MarkdownApiExceptionHandler>()
→ AddScoped<ICurrentUserService, CurrentUserService>()
→ IMarkdownContentStore 注册（Provider=Local → Local；否则 FileDev gRPC + Redis 热点缓存）
→ AddMarkdownHotBoardService + MarkdownHeatRebuildBackgroundService
→ AddHostedService<ClientRequestCleanupService>()
→ MapMarkdownApis() / MapMarkFavoriteApi()
```

---

## 12. 扩展指南（常见开发任务）

### 12.1 新增一个交互端点（如"点踩文档"）

1. 领域层：若需新记录表，在 `MarkDownDbContext.OnModelCreating` 配置唯一约束表 + 索引；在 `IMarkdownRepository`/`MarkDownRepository` 增加交互方法（参照 `LikeDocumentAsync` 的"记录 + 计数同事务 + 唯一约束幂等"模式），返回 `InteractionResult`。
2. Web.API：在 `MarkdownApi` 增加 `MapPost("/{guid}/xxx")` + Handler，参照 `LikeDocumentAsync`：加载实体 → 越权门控 → 调仓储 → `IsFirst` 才发 `MarkdownInteractionIntegrationEvent` → 热度刷新。
3. 若需热度参与：在 `MarkdownHeatFormula` 增加子项权重。

### 12.2 新增一个通知类型

1. `Markdown.Web.API/Application/IntegrationEvents/` 新增事件 record，标注 `[EventBusName("...")]`。
2. 在触发点通过 `EventPublishing.PublishSafelyAsync` 发布（注意幂等分支不发）。
3. Message 服务侧新增对应消费者 Handler（通知文案、落库、SignalR 推送）。

### 12.3 切换正文存储后端

保持 `IMarkdownContentStore` 接口不变，新增实现类并在 `Program.cs` 按配置条件注册即可，领域层与 API 层无需改动。

### 12.4 审核流程调整

状态机流转集中在 `MarkDown.SubmitForReview/Approve/Reject`，修改此处即全局生效；注意 `MarkStatus` 显式数值（15-18）与既有数据库兼容，不要重排。

---

## 13. 常见坑与既定决策

| 问题 | 决策 |
| --- | --- |
| 重复点赞/投币重复通知 | 仅 `IsFirst=true` 分支发布事件；唯一约束 + 事务回滚保证计数与记录原子一致 |
| 历史版本无限膨胀 | 快照前按哈希去重（`CreateHistorySnapshot` 返回 null 表示已存在相同内容） |
| 列表页全量加载 1MB 正文 | 正文已文件化，列表/搜索只查询元数据；带标签过滤时先轻量投影再回查 |
| GET 请求写放大 | 浏览量用 `ExecuteUpdate` 原子 SQL，不经过 EF 追踪 |
| 枚举默认值"审核通过"风险 | `Status` 列 DB 默认值 = 草稿，防 SQL 直插绕过审核 |
| 评论配图独立表外键类型问题 | `ReviewImage` 改为 owned JSONB（`ToJson()`），无独立表无外键 |
| 事件丢失 | 尽力而为投递（`PublishSafelyAsync`），后续可升级 Outbox 严格保证 |