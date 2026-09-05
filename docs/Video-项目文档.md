# Video 视频服务 · 项目文档

> 项目概述、技术栈、目录结构、运行/配置方式、数据模型与外部依赖。面向运维、部署与接手项目的人员。
> 配套文档：`Video-开发文档.md`（开发视角）、`Video-交接文档.md`（近期变更与遗留事项）。

---

## 1. 项目简介

**Video 视频服务** 是 NotBlog 平台的视频内容服务，承载视频的上传、播放、互动与社区化能力：

- **视频发布**：HTTP 上传（格式白名单 + 500MB 上限），文件与封面经 **gRPC 存储到 FileDev 服务**，数据库只存元数据与文件 URI。
- **视频播放**：`/api/videostream` 代理 FileDev 流量（HTTP Range 透传支持拖动），公开/私密/定时保护三级权限。
- **互动体系**：点赞 / 点踩 / 投币 / 分享 / 收藏（Stars）/ 观看数六维计数（`VideoQuote`）；评论四型内容（文本/图片/视频/富文本）＋ 赞踩（`ReviewQuote`）；弹幕（文本/图片）。
- **观看统计**：`VideoHistory` 聚合根追踪 开始→进度→完成 全生命周期，支持断点续播（LastPositionSeconds）。
- **跨服务作者通知**：点赞/投币（仅首次）、评论/回复评论 → 集成事件 → **Message 服务**站内通知 + SignalR 实时推送（v1.0）。
- **对外接口**：HTTP REST（MiniAPI，7 组端点组）+ RabbitMQ 集成事件 + gRPC 客户端（FileDev）。
- **存储**：PostgreSQL（EF Core，元数据单库）＋ Redis（缓存/去重）。
- **运行模式**：单机独立运行（本地 PostgreSQL/Redis/RabbitMQ），或在 Aspire AppHost 编排下作为微服务运行。

## 2. 技术栈

| 类别 | 选型 | 版本 |
| --- | --- | --- |
| 运行时 / 语言 | .NET | net10.0 / C# 12 |
| Web 框架 | ASP.NET Core Minimal API | 10.x |
| ORM | EF Core + Npgsql (PostgreSQL) | 10.0.x |
| 缓存 / 去重 | StackExchange.Redis（`CacheMemory` 封装 `IRedisCacheService`） | — |
| 消息总线 | RabbitMQ（`EventBus` 封装，routing key=$EventBusName） | — |
| 应用架构 | NotMediator（MediatR 变体） | 2.0.1 |
| 文件存储（下游） | gRPC 客户端（FileDev.Web.API）+ HTTP 流代理 | Grpc.Net 2.x |
| 认证 | JWT（`JWToken` 封装，HS384） | — |
| API 文档 UI | Scalar + OpenAPI | — |
| 容器化 | Docker（multi-stage build） | — |

> 依赖仓库内共享基础库：`Commons`、`CacheMemory`、`EventBus`、`JWToken`、`NotMediator`、`NotBlog.ServiceDefaults`（Aspire）。

## 3. 解决方案结构

```
NotBlog/
├─ Video.Domain         # 领域层（实体、值对象、枚举、领域事件、仓储/服务/缓存接口、选项）
├─ Video.Infrastructure # 基础设施层（VideoDbContext、仓储、Redis 缓存服务、DbConfig、Migrations）
├─ Video.Web.API        # 应用/表现层（MiniAPI 端点、CQRS 命令、领域/集成事件、DTO、DI）
├─ Video.Tests          # 单元测试（NUnit + Moq：值对象与命令处理器）
└─ docs/                # Video 服务文档集
```

### 3.1 Video.Domain 目录

```
Entities/           # 聚合根与实体
  Videos.cs         # 视频聚合根（封面/文件URI/标签/作者集合/软删除 + VideoQuote/VideoControl）
  VideoReview.cs    # 评论（四型内容 ReviewContent + ReviewQuote 赞踩）
  VideoBarrage.cs   # 弹幕
  VideoCollection.cs# 收藏夹聚合根（VideoNvid）
  VideoHistory.cs   # 观看历史聚合根（开始→进度→完成）
  AuthorVideo.cs    # 权限枚举（Public/Private/Protected）
  VideoProtectedTime.cs / BarrageControl / AffiliatedAuthorize
ValueObjects/       # VideoQuote / ReviewQuote / VideoControl / ReviewContent /
                    # VideoImage / TimeSpace / VideoQuote等
Cache/              # IVideoCacheService + VideoCacheKeys（缓存键约定）
IRepository/        # IVideoRepository / IVideoCollectionRepository / IVideoHistoryRepository
IServices/          # ICurrentUserService
Server/             # IVideoService（读查询契约）
Options/            # DbContextOption / ReviewContentOptions
```

### 3.2 Video.Infrastructure 目录

```
EntityFramework/    # VideoDbContext（IUnitOfWork：SaveEntitiesAsync 先派发领域事件）
Repository/         # VideoRepository / VideoCollectionRepository / VideoHistoryRepository
Cache/              # VideoCacheService（Redis 热层实现）
DbConfig/           # 各实体 IEntityTypeConfiguration（值对象 ToJson 映射）
Migrations/         # EF Core 迁移（含 v1.0：VideoInteractionNotifications）
```

### 3.3 Video.Web.API 目录

```
Apis/               # 端点组：AddVideo / Video / VideoStream / VideoWatchStats /
                    # VideoReview / VideoBarrage / VideoCollection / VideoServiceDI
Application/
  Commands/         # 9 个命令处理器（上传/点赞/删除/评论/引评/弹幕/收藏/观看）
  DomainEvents/     # VideoPublished 等领域事件处理器
  IntegrationEvents/# 发布侧：EventPublishing + Events/（VideoPublished/
                    # VideoInteraction/VideoCommentPublished）
Dto/                # Request/（AddVideo/AddReview/UpdateByVideo/…）Response/
Protos/             # FileDev filestorage.proto（gRPC 契约引用）
Program.cs          # 装配（Aspire 数据库/Redis/RabbitMQ + JWT + EventBus + 端点映射）
```

## 4. 运行与配置

### 4.1 单机独立运行

```bash
# 前置：本地 PostgreSQL / Redis / RabbitMQ（或 Docker Desktop）
dotnet run --project Video.Web.API/Video.Web.API.csproj
```

### 4.2 Aspire 编排运行

由 `NotBlog.AppHost` 编排；连接串/服务发现走 Aspire（`VideoPostgres` / `CacheMemory` / `EventBus`）。`Program.cs` 单服务模式桥接逻辑：无 `ConnectionStrings:VideoPostgres` 时回退读取 `DbContextOption:DbContextConnection`。

### 4.3 关键配置（appsettings.json / appsettings.Development.json）

| 配置节 | 说明 |
|---|---|
| `ConnectionStrings:VideoPostgres` | 主库连接串（Aspire 命名） |
| `DbContextOption:DbContextConnection` | 单服务模式数据库连接串（桥接） |
| `ReviewContent` | 评论内容校验参数（MaxTextLength/MaxImageCount/图片格式白名单，经 `ReviewContent.Options` 静态注入） |
| `FileDev:BaseUrl` | FileDev 服务地址（gRPC 与 HTTP 代理共用） |
| `GrpcClient:FileDevGrpcAddress` / `MaxMessageSizeMb` | gRPC 客户端地址与最大消息体（默认 512MB） |
| `JwtOptions` | JWT 认证配置（凭据外置，与 Identity 一致） |
| `EventBus` | RabbitMQ 连接（Debug 模式走本机 guest，Release 走 Aspire `AddRabbitMQClient`） |

### 4.4 数据库迁移

```bash
dotnet ef database update --project Video.Infrastructure --startup-project Video.Web.API --context VideoDbContext
```
> ⚠️ v1.0 新增迁移 `VideoInteractionNotifications`：`VideoReview.VideoQuote` JSON 列改名 `Quote` 并回填 `c_Upvote→c_Like / c_Down→c_Dislike`。**应用前备份。**

## 5. 数据模型

### 5.1 表清单（PostgreSQL）

| 表 | 实体 | 说明 |
|---|---|---|
| `Video` | `Videos` | 视频元数据；`VideoQuote`/`VideoControl`/`TimeSpace` JSON 列；`VideoGuid` 主键 |
| `VideoReview` | `VideoReview` | 评论；`Content`（四型）与 `Quote`（Like/Dislike）JSON 列；`RootReview` 自关联回复树 |
| `VideoBarrage` | `VideoBarrage` | 弹幕；软删除 `IsDelete` |
| `VideoCollection` | `VideoCollection` | 收藏夹（`VideoNvid`）；`VideoGuid` 为多值列 |
| `VideoHistory` | `VideoHistory` | 观看历史；每观看会话一行，含 Progress/Duration/IsCompleted |

### 5.2 关键索引（Migrations 内）

- `VideoReview`：`VideoReviewGuid`（PK）、`VideoGuid`、`UserGuid`、`RootReview`；
- 其余表各自主键索引；无社交关系透传查询（历史/集合按主键访问）。

### 5.3 计数模型

| 对象 | 计数 | 说明 |
|---|---|---|
| 视频 | Upvote / Stars / Watch / Down / Ballot / Share | `VideoQuote`；Stars 由收藏夹增减驱动（v1.0） |
| 评论 | Like / Dislike | `ReviewQuote`（v1.0） |
| 弹幕/收藏夹 | 携带 VideoQuote | 备用维度，语义收敛中 |

## 6. 外部依赖与事件契约

### 6.1 下游服务

| 依赖 | 协议 | 用途 |
|---|---|---|
| FileDev.Web.API | gRPC（上传/图片）+ HTTP（流代理） | 视频文件与封面存储、播放转发 |
| Message.Web.API | RabbitMQ 集成事件 | 站内通知原料（`VideoLiked/VideoCoined/VideoCommentAdded/VideoCommentReplied`） |

### 6.2 集成事件（routing key）

| 事件 | routing key | 发布时机 |
|---|---|---|
| `VideoPublishedIntegrationEvent` | VideoPublished | 视频发布 |
| `VideoInteractionIntegrationEvent` | VideoInteraction | 点赞/投币**仅首次** |
| `VideoCommentPublishedIntegrationEvent` | VideoCommentPublished | 顶级评论/回复评论 |

### 6.3 Redis 键空间（`VideoCacheKeys` 约定，前缀 `video:`）

| 键模式 | 类型 | TTL | 用途 |
|---|---|---|---|
| `video:meta:{guid}` | String JSON | 30min | 元数据缓存 |
| `video:list:*` | String JSON | 5/3min | 列表/搜索缓存 |
| `video:quote:{guid}` / `video:review-quote:{guid}` | Hash | 2h | 计数热层（评论赞踩增量写） |
| `video:watch-window:{guid}:{user}` | String SetNX | 5min | 观看去重（S-18） |
| `video:interact:once:{guid}:{user}:{field}` | String SetNX | 30 天 | 互动「首次」通知去重（v1.0） |

## 7. 运维事项

- **上游依赖顺序**：PostgreSQL → Redis → RabbitMQ → FileDev 可用后再启动本服务；Redis/RabbitMQ 故障时互动/观看主流程可用（缓存降级 DB、事件发布降级日志）。
- **启动自检**：单服务模式启动会打印「单个服务执行！」；Aspire 模式打印「Aspire服务执行！」。
- **日志约定**：统一 `ILogger` 结构化日志；命令/端点均记录 Guid 与结果；异常经全局脱敏中间件。
- **扩容说明**：互动去重/观看去重均依赖 Redis 分布式约束（SetNX），跨实例安全；防重入用 `Interlocked`（进程内，榜单类任务）或 Redis 锁（跨实例）。