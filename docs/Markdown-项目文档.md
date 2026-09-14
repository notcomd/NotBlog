# Markdown 博客服务 · 项目文档

> 项目概述、技术栈、目录结构、运行/配置方式、数据模型与外部依赖。面向运维、部署与接手项目的人员。

---

## 1. 项目简介

**Markdown 博客服务** 是 NotBlog 平台的博客内容服务，提供 Markdown 博客文章的完整生命周期管理：

- **文章管理**：创建 / 更新 / 软删除 / 历史版本快照与还原，审核状态机（草稿 → 待审核 → 通过 / 驳回）。
- **文件化正文存储**：DB 只存文件元数据与 SHA-256 哈希，正文经 `IMarkdownContentStore` 读写，**默认走 FileDev gRPC 文件服务**（开发与生产一致，2026-09-13 起开发环境亦默认 FileDev）；仅显式配置 `MarkdownContent:Provider=Local` 时回退本地磁盘。
- **交互体系**：浏览 / 点赞 / 收藏 / 分享 / 投币（硬币），文档级与评论级交互计数 + 唯一约束幂等。
- **评论体系**：顶级评论 / 回复（子评论）/ 评论点赞与踩 / 评论配图。
- **热点榜**：互动 50% + 浏览 30% + 时间衰减 20% 热度算法；Redis ZSet 直读 + 定时重建 + DB 降级。
- **收藏体系**：收藏 + 标签分类 + 标签库复用建议。
- **跨服务通知**：通过 RabbitMQ 集成事件（文章发布 / 点赞 / 投币 / 评论发布 / 评论点赞 / 评论踩）通知 Message 服务站内推送。

- **对外接口**：HTTP REST（`/api/markdown/*` + `/api/favorites/*`，共 40 个端点）。
- **运行模式**：单机独立运行（依赖本地 PostgreSQL / Redis / RabbitMQ，正文存储缺省经 FileDev gRPC；无 FileDev 时可设 `MarkdownContent:Provider=Local` 回退本地磁盘），或在 Aspire AppHost 编排下作为微服务运行。

---

## 2. 技术栈

| 类别 | 选型 | 版本 |
| --- | --- | --- |
| 运行时 / 语言 | .NET | net10.0 / C# 12 |
| Web 框架 | ASP.NET Core Minimal API | 10.x |
| ORM | EF Core + Npgsql (PostgreSQL) | 10.0.x |
| 消息总线 | RabbitMQ（`EventBus` 封装） | — |
| 应用架构 | NotMediator（MediatR 变体） | 2.0.1 |
| 缓存 / 热点榜 | StackExchange.Redis（`CacheMemory` 封装） | — |
| 文件存储（下游） | gRPC 客户端（调用 FileDev.Web.API） | Grpc.Net 2.83.0 |
| 认证 | JWT（`JWToken` 封装，HS384） | — |
| API 文档 UI | Scalar + OpenAPI | 2.16.x |
| 容器化 | Docker（multi-stage build） | — |

> 依赖仓库内共享基础库：`Commons`、`CacheMemory`、`EventBus`、`JWToken`、`NotBlog.ServiceDefaults`（Aspire 服务默认）、`FileDev.Web.API`（gRPC 服务，通过 `Protos/filestorage.proto` 客户端）。

---

## 3. 解决方案结构（相关项目）

```
NotBlog/
├─ Markdown.Domain         # 领域层（实体、值对象、枚举、领域事件、仓储/服务接口、热度公式）
├─ Markdown.Infrastructure # 基础设施层（EF DbContext、仓储实现、幂等管理、实体配置、迁移）
├─ Markdown.Web.API        # 表现/应用层（REST 端点、命令/查询、集成事件、文件存储实现、热点榜、后台任务）
└─ docs/                   # 本文档集（Markdown 服务文档）
```

### 3.1 Markdown.Domain 目录

```
Entities/                    # 实体（一个文件一个类）
  MarkDown.cs                # 文档聚合根（文件元数据 + 交互计数 + 审核状态机 + 评论/历史聚合入口 + Builder）
  MarkFavorite.cs            # 收藏聚合根（标签 JSON 列 + Tags 内存集合 + 创建工厂）
  MarkFavoriteTag.cs         # 收藏标签库（UseCount / LastUsedAt 排序建议）
  MarkReview.cs              # 评论实体（自引用子评论 + 配图 + ReviewQuote + 软删除）
  OldMarkDown.cs             # 历史版本快照（全文 + 哈希）
  MarkCoin.cs                # 投币流水（用户-文档维度）
  MarkDocumentLike.cs        # 文档点赞记录
  MarkReviewLike.cs          # 评论点赞记录
  MarkReviewDislike.cs       # 评论踩记录
Enum/
  MarkDownAuth.cs            # 文档权限（Public/Private/Protected/Admin/Root）
  MarkReviewAuth.cs          # 评论权限（Public/Private/Protected）
  MarkStatus.cs              # 审核状态机（Draft=15/PendingReview=16/Approved=17/Rejected=18）
Events/
  MarkDownCreateDomainEvent.cs # 文档创建领域事件
Heat/
  MarkdownHeatFormula.cs     # 热点分公式（互动50%/浏览30%/时间衰减20%）
IRepository/
  IMarkdownRepository.cs     # 文档聚合根仓储接口（唯一暴露的文档仓储）
  IMarkFavoriteRepository.cs # 收藏聚合根仓储接口
  InteractionResult.cs       # 交互结果（Count + 是否首次 IsFirst）
IServices/
  ICurrentUserService.cs     # 当前用户服务接口（从 JWT Claims 提取）
  IMarkdownContentStore.cs   # 正文存储抽象（Save/Read/Delete）
Options/
  DbContextOption.cs         # 数据库连接配置项
```

### 3.2 Markdown.Infrastructure 目录

```
EntityFramework/
  MarkDownDbContext.cs       # EF DbContext（实现 IUnitOfWork，SaveChanges 前分发领域事件）
Configuration/
  MarkDownEntityConfiguration.cs        # 文档表配置（owned MarkQuote 6 列、文件元数据、审核默认草稿、标签 JSON 列）
  MarkReviewEntityConfiguration.cs      # 评论表配置（自引用、owned ReviewQuote、配图 JSONB ToJson、索引）
  MarkFavoriteEntityConfiguration.cs    # 收藏表配置（标签 JSON 列、(UserGuid,MarkDownGuid) 唯一索引）
  MarkFavoriteTagEntityConfiguration.cs # 标签库表配置（(UserGuid,Tag) 唯一索引）
Idempotent/
  ClientRequest.cs           # 幂等记录实体（ClientRequestId 主键 + ResponseJson）
  IRequestManagement.cs      # 幂等执行接口
  RequestManagement.cs       # 幂等实现（原子占位 → 赢家执行写响应 → 输家轮询返回）
Migrations/                  # EF Core 迁移（6 个 + ModelSnapshot）
Repository/
  MarkDownRepository.cs      # 文档聚合根仓储实现（交互计数幂等、ExecuteUpdate 浏览增量）
  MarkFavoriteRepository.cs  # 收藏仓储实现（标签合并、标签库 upsert）
ServiceCollectionExtensions.cs  # 基础设施 DI 注册（仓储 + 幂等管理）
```

### 3.3 Markdown.Web.API 目录

```
Apis/
  MarkdownApi.cs             # 文章主资源端点（CRUD + 列表 + 搜索 + 热点 + 交互）
  MarkdownApiHelpers.cs      # 共享辅助（管理员判定、幂等 key、权限解析、评论可见性、配图/标签校验）
  MarkdownAuditApi.cs        # 审核端点（submit/approve/reject）
  MarkdownReviewApi.cs       # 评论端点（创建/列表/详情/子评论/更新/删除/点赞/踩）
  MarkdownHistoryApi.cs      # 历史版本端点（列表/详情/删除/还原）
  MarkFavoriteApi.cs         # 收藏端点（添加/取消/改标签/标签库/列表）
Application/
  Behaviors/                 # 管道行为（LoggerBehavior / TransactionBehavior）
  Commands/                  # 命令 + Handler（CQRS 写侧，见开发文档 §4.1）
  Queries/                   # 查询 + Handler（CQRS 读侧，见开发文档 §4.2）
  IntegrationEvents/         # 集成事件定义 + EventPublishing.PublishSafelyAsync
    IntegrationEventHandlers/ # 集成事件处理器（占位/兼容）
Background/
  MarkdownHeatRebuildBackgroundService.cs  # 热点榜定时重建（每 10 分钟）
Dto/
  Request/                   # 请求 DTO（含 DataAnnotations 校验）
  Response/                  # 响应 DTO + Mapper（ApiResponse / MarkdownResponse 等）
Extensions/
  MarkdownApiExceptionHandler.cs  # 业务异常 → HTTP 状态码统一映射
  MigrateDbContextExtensions.cs   # 启动自动迁移
Protos/
  filestorage.proto          # FileDev gRPC 客户端协议
Services/
  CurrentUserService.cs      # ICurrentUserService 实现（JWT Claims）
  IMarkdownContentStore.cs / LocalMarkdownContentStore.cs / FileDevMarkdownContentStore.cs  # 正文存储实现
  MarkdownHotBoardService.cs / IMarkdownHotBoardService.cs  # 热点榜服务（Redis + DB 双路径）
  FileStorageGrpcOptions.cs  # FileDev gRPC 配置
  ClientRequestCleanupService.cs  # 幂等记录清理（每 24h，保留 7 天）
Resources/                   # 启动横幅与资源
Program.cs                   # 应用入口与全线配置
appsettings.json / appsettings.Development.json
Dockerfile                   # 容器化
```

---

## 4. 数据模型（PostgreSQL `markdownpostgres` 库）

### 4.1 表与核心索引

| 表 | 关键列 | 索引 / 约束 |
| --- | --- | --- |
| `MarkDown` | MarkDownGuid, MarkUserGuid, MarkDownName, FileId, FileUri, FileSize, FileExt, MarkDownHash, CoverUrl, Status, IsDelete, Auth, CreateAt, UpdateAt + MarkQuote 6 列 | Id（HiLo）、MarkDownGuid 唯一、Status 默认 15(草稿) |
| `MarkReview` | MarkReviewGuid, MarkDownGuid, UserId, MarkAggregateRootGuid(父评论), MarkReviewContent, ReviewAuth, IsDelete, ReviewTime + ReviewQuote 4 列 + ReviewImages(JSONB) | MarkDownGuid / UserId / MarkAggregateRootGuid 索引；FK 级联删除 |
| `OldMarkDown` | OldMarkDownGuid, MarkDownGuid, UserGuid, AuthType, OldMarkDownContent, OldMarkDownHash, IsDelete | MarkDownGuid FK |
| `MarkFavorite` | MarkFavoriteGuid, UserGuid, MarkDownGuid, TagsJson, CreateAt | **(UserGuid, MarkDownGuid) 唯一**、UserGuid / MarkDownGuid 索引 |
| `MarkFavoriteTag` | MarkFavoriteTagGuid, UserGuid, Tag, UseCount, LastUsedAt | **(UserGuid, Tag) 唯一**、UserGuid 索引 |
| `MarkCoin` | Id, MarkCoinGuid, MarkDownGuid, UserId, Amount, CreateAt | **(MarkDownGuid, UserId) 唯一**（一用户一篇仅投币一次）、MarkDownGuid 索引 |
| `MarkDocumentLike` | Id, MarkDownGuid, UserId, CreateAt | **(MarkDownGuid, UserId) 唯一**（点赞去重） |
| `MarkReviewLike` | Id, MarkReviewGuid, UserId, CreateAt | **(MarkReviewGuid, UserId) 唯一** |
| `MarkReviewDislike` | Id, MarkReviewGuid, UserId, CreateAt | **(MarkReviewGuid, UserId) 唯一**（踩去重） |
| `ClientRequest` | ClientRequestId, ClientRequestName, Created, ResponseJson | ClientRequestId 主键（幂等占位唯一约束） |

> 主键策略：`MarkDown` / `MarkFavorite` / `MarkDocumentLike` / `MarkReviewLike` / `MarkReviewDislike` 使用 HiLo 序列（`MarkDownGuid` 等多个 sequence）；`MarkReview` / `MarkFavoriteTag` 以领域 Guid 为主键；`MarkCoin` 使用自增 Id。

### 4.2 迁移历史

| 迁移 | 说明 |
| --- | --- |
| `20260817210450_MarkDownDb` | 初始建库（MarkDown / MarkReview / MarkFavorite / ClientRequest / 点赞记录等） |
| `20260820201833_MarkdownFileStorage` | 文件化重构：删除旧正文列，新增 FileId / FileUri / FileSize / FileExt / Hash 列 |
| `20260820225319_AddMarkDownCoverUrl` | 新增封面列 CoverUrl |
| `20260826054452_ReviewImagesAsOwnedJsonb` | 评论配图改为 owned JSONB 存储 |
| `20260826125151_PendingModelSync` | 模型同步调整 |
| `20260829074243_AddMarkCoinUniqueConstraint` | 新增 `MarkCoin(MarkDownGuid, UserId)` 唯一索引（投币一次化） |

---

## 5. 运行与部署

### 5.1 本地独立运行（开发）

1. 启动依赖：PostgreSQL（库 `markdownpostgres`）、Redis、RabbitMQ。
2. 配置 `appsettings.json`（默认即可）：`DbContextOption.DbContextConnection`（本地连接串）、`EventBus`（RabbitMQ 连接）、`JwtOptions`。
3. 正文存储默认走 FileDev gRPC（`FileStorageGrpc:Address` 为脱离 AppHost 时的兜底地址）；无 FileDev 可用时，设 `MarkdownContent:Provider=Local` 回退本地磁盘 `markdown-files/`。
4. 运行：`dotnet run --project Markdown.Web.API`。
5. 启动时自动执行 EF 迁移（`AddMigration<MarkDownDbContext>`）。

### 5.2 Aspire 编排运行

- 通过 `NotBlog.sln` 的 AppHost 项目统一编排，连接串（`MarkDownPostgres`）由 Aspire 注入。
- `MarkdownContent:Provider` 不配置 → 走 FileDev gRPC（服务发现解析 `filedev-web-api`），同时注册 Redis 热点缓存。
- RabbitMQ 通过 `EventBus` 连接串注入（非 DEBUG 分支 `AddRabbitMQClient`）。

### 5.3 容器化

- 根 `Dockerfile`（multi-stage build）可独立部署；生产环境需保证 FileDev 可达并配置 `FileStorageGrpc:Address` 兜底。

---

## 6. 配置清单

| 配置节 | 键 | 说明 |
| --- | --- | --- |
| `ConnectionStrings` | `MarkDownPostgres` | PostgreSQL 连接串（Aspire 注入；单机分支回退 `DbContextOption`） |
| `DbContextOption` | `DbContextConnection` | 单机模式数据库连接串 |
| `JwtOptions` | Issuer / Audiences / PrivateKey(外部) / 校验项 | JWT 认证（Identity 签发对齐，HS384） |
| `EventBus` | SubscriptionClientName / HostName / ExchangeName / ExchangeType / UserName / Password | RabbitMQ 事件总线（`markdown_queue` / `notcomd_event_bus`） |
| `MarkdownContent` | `Provider`（缺省 = FileDev；`Local` = 本地磁盘回退） | 正文存储实现切换；默认 FileDev，本地磁盘仅作无 FileDev 时的回退 |
| `FileStorageGrpc` | `Address` | FileDev gRPC 地址（脱离 AppHost 时兜底） |
| `MongoDb` | 无（非本服务使用） | — |

---

## 7. 外部依赖

| 依赖 | 用途 | 不可用时表现 |
| --- | --- | --- |
| PostgreSQL | 主数据存储（EF Core） | 服务不可用 |
| RabbitMQ | 集成事件总线（通知 Message 服务） | 业务可继续落库，事件丢失仅记日志（尽力而为） |
| Redis | 热点榜 ZSet + 浏览防刷 + 重建互斥锁 | 热点榜降级 DB 实时计算；浏览防刷跳过 |
| FileDev（gRPC） | 正文文件存储（默认，含开发环境） | 正文读写失败；`MarkdownContent:Provider=Local` 可回退本地磁盘 |
| Message 服务（消费端） | 站内通知推送 | 通知不达，文章/交互功能本身不受影响 |

---

## 8. 运维要点

- **热点榜**：Redis 故障自动降级 DB；定时重建每 10 分钟兜底收敛 HeatScore 一致性。
- **幂等表**：`ClientRequest` 记录由 `ClientRequestCleanupService` 每 24h 清理（保留 7 天），防止膨胀。
- **日志**：结构化工场日志（ILogger），交互端点记录关键计数变更；事件发布失败/总线故障有告警日志。
- **监控关注点**：RabbitMQ 队列堆积（通知消费延迟）、FileDev gRPC 延迟与重试、热点榜 Redis miss 率。