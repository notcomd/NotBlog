# NotBlog 推文（Tweet）功能集成开发文档

> **版本**: v1.1  
> **日期**: 2026-07-07  
> **项目**: NotBlog Message 服务  
> **目标**: 在现有即时通讯服务基础上，集成类似微博/Twitter 的推文发布与互动功能  
> **状态**: 已评审（10 项决策已确认）

---

## 目录

1. [需求分析与功能拆解](#1-需求分析与功能拆解)
2. [数据库设计](#2-数据库设计)
3. [API 接口设计规范](#3-api-接口设计规范)
4. [前端交互流程与状态管理](#4-前端交互流程与状态管理)
5. [后端业务逻辑实现](#5-后端业务逻辑实现)
6. [异常处理与日志记录策略](#6-异常处理与日志记录策略)
7. [测试计划](#7-测试计划)
8. [部署与发布流程](#8-部署与发布流程)
9. [项目进度与里程碑规划](#9-项目进度与里程碑规划)
10. [决策记录](#10-决策记录)

---

## 1. 需求分析与功能拆解

### 1.1 核心功能矩阵

| 模块 | 功能 | 优先级 | 说明 |
|------|------|--------|------|
| **推文发布** | 纯文本推文 | P0 | 最长 2000 字符（复用 `TextContent` 模式） |
| | 文本+图片推文 | P0 | 支持最多 9 张图片，通过 FileDev.Web.API 存储 |
| | 文本+视频推文 | P0 | 支持单个视频，上传后自动压缩 |
| | 文本+链接推文 | P0 | 自动解析链接元数据 |
| | 草稿保存 | P0 | 仅创建者可见（`TweetStatus = Draft`） |
| **审核流程** | 敏感词过滤 | P0 | 已接入真实实现（S-17）：命中敏感词的 Tweet 被拒绝或进入审核 |
| | 图片内容审核 | P1 | 已接入 IImageModerationService，审核失败策略明确（S-17） |
| | 人工审核 | P1 | 管理员审核接口 |
| | 审核状态跟踪 | P0 | Pending → Approved/Rejected |
| **互动功能** | 评论（含回复） | P0 | 支持嵌套回复（最大 2 层） |
| | 点赞 | P0 | 可取消，记录时间 |
| | 收藏 | P0 | 可取消，记录时间 |
| | 分享 | P0 | 站内转发 + 站外分享链接 |
| | 硬币 | P1 | 用户投币给推文 |
| | 查看量统计 | P0 | 去重统计（用户+IP） |
| | 热度计算 | P0 | 公式：点赞×20% + 收藏×20% + 硬币×20% + 分享×20% + 查看量×20% |
| **举报机制** | 举报推文 | P0 | 含举报原因分类 |
| | 举报评论 | P0 | 含举报原因分类 |
| | 举报处理 | P1 | 管理员审核 → 下架/驳回 |
| | 处理结果通知 | P0 | 通知被举报方和举报方 |
| **审核管理** | 审核队列 | P0 | 待审核列表，支持批量 |
| | 审核操作记录 | P1 | 审核人、时间、原因 |
| | 审核统计 | P2 | 审核通过率、处理时效 |

### 1.2 核心实体关系图 (ERD)

```
┌─────────────────────────────────────────────────────────────────────┐
│                          推文实体关系图                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────┐       ┌──────────────┐       ┌──────────────────┐    │
│  │   User   │       │    Tweet     │       │  TweetAuditLog   │    │
│  │ (Identity)│      │              │       │                  │    │
│  │──────────│ 1:N   │──────────────│ 1:N   │──────────────────│    │
│  │ UserGuid │──────▶│ TweetGuid    │──────▶│ AuditGuid        │    │
│  │ UserName │       │ AuthorGuid   │       │ TweetGuid(FK)    │    │
│  │ Avatar   │       │ Content      │       │ AuditorGuid      │    │
│  └──────────┘       │ MediaUrls    │       │ AuditStatus      │    │
│                     │ LinkMetadata │       │ AuditReason      │    │
│  ┌──────────┐       │ TweetStatus  │       │ AuditTime        │    │
│  │   Tag    │       │ Visibility   │       └──────────────────┘    │
│  │──────────│  N:M  │ IsPinned     │                                │
│  │ TagGuid  │◀─────▶│ PublishTime  │       ┌──────────────────┐    │
│  │ TagName  │       │ ViewCount    │       │   TweetReport    │    │
│  └──────────┘       └──────┬───────┘       │──────────────────│    │
│                            │               │ ReportGuid       │    │
│         ┌──────────────────┼───────────────│ ReporterGuid     │    │
│         │                  │               │ TargetGuid       │    │
│         ▼                  ▼               │ TargetType       │    │
│  ┌──────────────┐  ┌──────────────┐       │ ReportReason     │    │
│  │   Comment    │  │  Interaction │       │ EvidenceUrls     │    │
│  │──────────────│  │──────────────│       │ ReportStatus     │    │
│  │ CommentGuid  │  │ InteractGuid │       │ ReportTime       │    │
│  │ TweetGuid(FK)│  │ TweetGuid(FK)│       │ ResolutionNote   │    │
│  │ UserGuid     │  │ UserGuid     │                                │
│  │ Content      │  │ Type         │       ┌──────────────────┐    │
│  │ ParentGuid   │  │ CreatedTime  │       │   Notification   │    │
│  │ ReplyCount   │  └──────────────┘       │──────────────────│    │
│  │ IsDeleted    │                          │ NotifyGuid       │    │
│  └──────────────┘                          │ UserGuid         │    │
│         │                                  │ Type             │    │
│         │ 1:N                              │ Content          │    │
│         ▼                                  │ IsRead           │    │
│  ┌──────────────┐                          └──────────────────┘    │
│  │CommentReport │                                                    │
│  │──────────────│                                                    │
│  │ CommentGuid   │                                                    │
│  └──────────────┘                                                    │
│                                                                     │
│  推文状态流转:                                                       │
│  ┌──────┐   发布   ┌──────────┐   审核   ┌──────────┐              │
│  │ Draft │───────▶│ Pending  │───────▶│ Approved │              │
│  └──────┘         └────┬─────┘         └──────────┘              │
│                        │ 审核失败       ┌──────────┐              │
│                        └──────────────▶│ Rejected │              │
│                                        └──────────┘              │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.3 模块架构分层

参照现有 DDD 分层架构，新增模块遵循相同分层：

```
Message.Domain/
  Entities/
    Tweet/Tweet.cs              — 推文聚合根
    Tweet/TweetInteraction.cs   — 互动实体（点赞/收藏/分享/硬币）
    Tweet/Comment.cs            — 评论实体
    Tweet/TweetAuditLog.cs      — 审核日志实体
    Tweet/TweetReport.cs        — 举报实体
    Tweet/TweetTag.cs           — 标签实体
    Tweet/TweetNotification.cs  — 通知实体
  Enums/
    TweetEnums.cs               — 推文相关枚举
  Events/
    TweetCreatedEvent.cs
    TweetPublishedEvent.cs
    TweetApprovedEvent.cs / TweetRejectedEvent.cs
    CommentAddedEvent.cs
    TweetLikedEvent.cs / TweetFavoritedEvent.cs / TweetSharedEvent.cs / TweetCoinedEvent.cs
    TweetReportedEvent.cs / CommentReportedEvent.cs
    ReportResolvedEvent.cs
  IRepository/
    ITweetRepository.cs
    ICommentRepository.cs
    ITweetInteractionRepository.cs
    ITweetAuditRepository.cs
    ITweetReportRepository.cs
  IProvider/
    ITweetProvider.cs

Message.Infrastructure/
  EntityConfig/
    TweetConfiguration.cs
    CommentConfiguration.cs
    TweetInteractionConfiguration.cs
    TweetAuditLogConfiguration.cs
    TweetReportConfiguration.cs
    TweetTagConfiguration.cs
    TweetNotificationConfiguration.cs

Message.Web.API/
  Application/DomainEventHandlers/
    TweetCreatedEventHandler.cs     — 触发自动审核
    TweetApprovedEventHandler.cs    — 更新状态、通知作者
    TweetRejectedEventHandler.cs    — 通知作者失败原因
    CommentAddedEventHandler.cs     — 更新评论计数
    TweetInteractionEventHandler.cs — 更新热度
    TweetReportedEventHandler.cs    — 记录举报、通知管理员
    ReportResolvedEventHandler.cs   — 通知举报双方
  Controllers/
    TweetsController.cs
    CommentsController.cs
  Dto/
    TweetDto.cs                     — Request/Response DTO
```

---

## 2. 数据库设计

### 2.1 数据库引擎

沿用现有 PostgreSQL（通过 Npgsql）。

### 2.2 表结构设计

#### 2.2.1 Tweets（推文表）

```sql
CREATE TABLE Tweets (
    TweetGuid        UUID PRIMARY KEY,           -- 推文唯一标识 (Guid.CreateVersion7)
    AuthorGuid       UUID NOT NULL,              -- 作者
    Content          TEXT NOT NULL,              -- 推文文本内容（最长 2000 字符）
    MediaUrls        JSONB,                      -- 媒体文件 URL 列表 ["url1","url2"]
    LinkMetadata     JSONB,                      -- 链接元数据 {"url":"...","title":"...","description":"...","image":"..."}
    Hashtags         TEXT[],                     -- 话题标签数组
    TweetStatus      VARCHAR(20) NOT NULL,       -- Draft | Pending | Approved | Rejected
    Visibility       VARCHAR(20) NOT NULL,       -- Public | Followers | Private
    IsPinned         BOOLEAN DEFAULT FALSE,      -- 是否置顶（仅个人主页）
    ViewCount        BIGINT DEFAULT 0,           -- 查看量
    LikeCount        INT DEFAULT 0,              -- 点赞数（冗余缓存）
    CommentCount     INT DEFAULT 0,              -- 评论数（冗余缓存）
    ShareCount       INT DEFAULT 0,              -- 分享数（冗余缓存）
    CoinCount        INT DEFAULT 0,              -- 硬币数（冗余缓存）
    FavoriteCount    INT DEFAULT 0,              -- 收藏数（冗余缓存）
    HotScore         DOUBLE PRECISION DEFAULT 0, -- 热度分数
    AuditReason      TEXT,                       -- 最近审核原因
    PublishTime      TIMESTAMPTZ,                -- 发布时间
    CreateTime       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdateTime       TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- 索引
    INDEX idx_tweets_author (AuthorGuid),
    INDEX idx_tweets_status_time (TweetStatus, CreateTime DESC),
    INDEX idx_tweets_hotscore (HotScore DESC),
    INDEX idx_tweets_hashtags USING GIN (Hashtags),
    INDEX idx_tweets_create_time (CreateTime DESC)
);
```

#### 2.2.2 TweetTags（标签表，多对多关联表）

```sql
CREATE TABLE TweetTags (
    TweetGuid UUID NOT NULL REFERENCES Tweets(TweetGuid) ON DELETE CASCADE,
    TagName   VARCHAR(100) NOT NULL COLLATE "C",  -- 规范化标签名（小写），排序规则 C 提高查询性能
    PRIMARY KEY (TweetGuid, TagName),
    INDEX idx_tweettags_tag (TagName)
);
```

#### 2.2.3 TweetMedia（推文媒体附件表）

```sql
CREATE TABLE TweetMedia (
    MediaGuid    UUID PRIMARY KEY,
    TweetGuid    UUID NOT NULL REFERENCES Tweets(TweetGuid) ON DELETE CASCADE,
    MediaType    VARCHAR(10) NOT NULL,           -- Image | Video
    MediaUrl     TEXT NOT NULL,
    ThumbnailUrl TEXT,
    FileSize     BIGINT,                          -- 字节
    Width        INT,
    Height       INT,
    Duration     INT,                             -- 视频时长（秒）
    SortOrder    INT DEFAULT 0,                   -- 排序序号
    CreateTime   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tweetmedia_tweet ON TweetMedia(TweetGuid, SortOrder);
```

#### 2.2.4 TweetInteractions（推文互动表）

```sql
CREATE TABLE TweetInteractions (
    InteractGuid UUID PRIMARY KEY,
    TweetGuid    UUID NOT NULL REFERENCES Tweets(TweetGuid) ON DELETE CASCADE,
    UserGuid     UUID NOT NULL,
    Type         VARCHAR(10) NOT NULL,            -- Like | Favorite | Share | Coin
    CreateTime   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE INDEX uq_interaction (TweetGuid, UserGuid, Type),
    INDEX idx_interactions_tweet (TweetGuid),
    INDEX idx_interactions_user (UserGuid)
);
```

#### 2.2.5 TweetViewLogs（推文查看日志，用于去重统计）

```sql
CREATE TABLE TweetViewLogs (
    ViewGuid  UUID PRIMARY KEY,
    TweetGuid UUID NOT NULL REFERENCES Tweets(TweetGuid) ON DELETE CASCADE,
    UserGuid  UUID,                              -- 可为空（匿名访问）
    ViewerIP  VARCHAR(45),                       -- 查看者 IP
    ViewTime  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    INDEX idx_viewlogs_tweet_time (TweetGuid, ViewTime DESC),
    INDEX idx_viewlogs_user_tweet (UserGuid, TweetGuid, ViewTime DESC)
);
```

#### 2.2.6 Comments（评论表）

```sql
CREATE TABLE Comments (
    CommentGuid  UUID PRIMARY KEY,
    TweetGuid    UUID NOT NULL REFERENCES Tweets(TweetGuid) ON DELETE CASCADE,
    UserGuid     UUID NOT NULL,
    ParentGuid   UUID,                            -- NULL=顶级评论，非 NULL=回复某评论
    ReplyToGuid  UUID,                            -- 被回复的用户 GUID
    Content      TEXT NOT NULL,                   -- 评论内容
    LikeCount    INT DEFAULT 0,                   -- 点赞数
    ReplyCount   INT DEFAULT 0,                   -- 回复数
    IsDeleted    BOOLEAN DEFAULT FALSE,           -- 软删除
    CreateTime   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    INDEX idx_comments_tweet_time (TweetGuid, CreateTime),
    INDEX idx_comments_parent (ParentGuid),
    INDEX idx_comments_user (UserGuid)
);
```

#### 2.2.7 TweetAuditLogs（审核日志表）

```sql
CREATE TABLE TweetAuditLogs (
    AuditGuid    UUID PRIMARY KEY,
    TweetGuid    UUID NOT NULL REFERENCES Tweets(TweetGuid),
    AuditorGuid  UUID NOT NULL,                    -- 审核员 GUID（管理员或特殊账号）
    Action       VARCHAR(10) NOT NULL,             -- Approve | Reject
    Reason       TEXT NOT NULL,                    -- 审核理由
    AuditTime    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    INDEX idx_audit_tweet (TweetGuid, AuditTime DESC),
    INDEX idx_audit_auditor (AuditorGuid, AuditTime DESC)
);
```

#### 2.2.8 TweetReports（举报表）

```sql
CREATE TABLE TweetReports (
    ReportGuid     UUID PRIMARY KEY,
    ReporterGuid   UUID NOT NULL,                  -- 举报者
    TargetType     VARCHAR(10) NOT NULL,           -- Tweet | Comment
    TargetGuid     UUID NOT NULL,                  -- 被举报的 TweetGuid 或 CommentGuid
    ReportReason   TEXT NOT NULL,                  -- 举报文字原因
    ReportCategory VARCHAR(50) NOT NULL,           -- Spam | Harassment | Violence | Porn | 其他
    EvidenceUrls   JSONB,                          -- 举报证据媒体文件 ["url1","url2","url3"] 支持图片/视频/文档
    Status         VARCHAR(20) NOT NULL,           -- Pending | Reviewing | Resolved_Removed | Resolved_Rejected
    ReviewerGuid   UUID,                           -- 管理员
    ReviewNote     TEXT,                           -- 审核备注
    ReviewTime     TIMESTAMPTZ,                    -- 审核时间
    CreateTime     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    INDEX idx_reports_target (TargetType, TargetGuid),
    INDEX idx_reports_status (Status, CreateTime),
    INDEX idx_reports_reporter (ReporterGuid)
);
```

> **设计说明**: `EvidenceUrls` 为 JSONB 数组，存储举报用户提交的截图、录屏视频或相关文档的 URL。文件通过 `FileDev.Web.API` 上传后获取 URL，再随举报请求一并提交。每个举报最多附带 5 个证据文件，单文件 ≤ 10MB。

#### 2.2.9 TweetNotifications（通知表）

```sql
CREATE TABLE TweetNotifications (
    NotifyGuid    UUID PRIMARY KEY,
    UserGuid      UUID NOT NULL,                   -- 通知接收者
    Type          VARCHAR(30) NOT NULL,            -- 通知类型
    Title         VARCHAR(255) NOT NULL,
    Content       TEXT NOT NULL,
    RefType       VARCHAR(20),                     -- Tweet | Comment | Report | Audit
    RefGuid       UUID,                            -- 关联实体 GUID
    IsRead        BOOLEAN DEFAULT FALSE,
    CreateTime    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    INDEX idx_notify_user_read (UserGuid, IsRead, CreateTime DESC),
    INDEX idx_notify_type (Type)
);
```

---

## 3. API 接口设计规范

### 3.1 通用规范

- **Base URL**: `/api/tweets`、`/api/comments`、`/api/reports`、`/api/audit`
- **认证**: 网关统一认证（API Gateway 完成 JWT 验证后，通过 Header `X-User-Id` 传递用户 GUID 至服务）。服务内不处理 Token 验证，仅做**资源所有权确认**（判断操作目标是否属于当前用户）。
- **响应格式**: 统一使用 `ApiResponse<T>` 格式
- **分页**: `page`(从1开始) + `pageSize`(1-100)，返回含 `total`, `items` 的对象
- **Content-Type**: `application/json`

### 3.2 推文接口 (`/api/tweets`)

| 方法 | 路径 | 描述 | 权限 |
|------|------|------|------|
| `POST` | `/api/tweets` | 创建/发布推文 | 已登录用户 |
| `POST` | `/api/tweets/draft` | 保存草稿 | 已登录用户 |
| `GET` | `/api/tweets/{tweetGuid}` | 获取推文详情 | 公开（Approved 状态） |
| `GET` | `/api/tweets/user/{userGuid}` | 获取用户推文列表 | 公开 |
| `GET` | `/api/tweets/timeline` | 获取时间线 | 已登录用户 |
| `GET` | `/api/tweets/trending` | 热门推文 | 公开 |
| `PUT` | `/api/tweets/{tweetGuid}` | 编辑草稿 | 作者本人 |
| `DELETE` | `/api/tweets/{tweetGuid}` | 删除推文 | 作者本人或管理员 |
| `POST` | `/api/tweets/{tweetGuid}/pin` | 置顶推文 | 作者本人 |
| `POST` | `/api/tweets/{tweetGuid}/unpin` | 取消置顶 | 作者本人 |

#### 创建推文请求示例

```json
POST /api/tweets
（网关已注入 Header: X-User-Id, X-User-Roles）
Content-Type: multipart/form-data

{
  "content": "这是一条推文 #HelloWorld",
  "mediaFiles": ["file1.jpg", "file2.jpg"],   // 多文件上传
  "linkUrl": "https://example.com",            // 可选
  "visibility": "Public"                       // Public | Followers | Private
}
```

#### 推文详情响应示例

```json
{
  "success": true,
  "data": {
    "tweetGuid": "018f...",
    "author": {
      "userGuid": "...",
      "userName": "Alice",
      "avatar": "https://..."
    },
    "content": "这是一条推文 #HelloWorld",
    "mediaUrls": ["https://cdn..."],
    "linkMetadata": {
      "url": "https://example.com",
      "title": "Example",
      "description": "...",
      "image": "https://..."
    },
    "hashtags": ["HelloWorld"],
    "tweetStatus": "Approved",
    "visibility": "Public",
    "viewCount": 1200,
    "likeCount": 45,
    "commentCount": 12,
    "shareCount": 8,
    "coinCount": 3,
    "favoriteCount": 15,
    "hotScore": 254.6,
    "publishTime": "2026-07-07T10:30:00Z",
    "createTime": "2026-07-07T10:25:00Z",
    "isLiked": true,
    "isFavorited": false,
    "isCoined": false
  }
}
```

### 3.3 评论接口 (`/api/comments`)

| 方法 | 路径 | 描述 | 权限 |
|------|------|------|------|
| `POST` | `/api/comments` | 发表评论 | 已登录用户 |
| `GET` | `/api/comments/tweet/{tweetGuid}` | 获取推文评论列表 | 公开 |
| `GET` | `/api/comments/{commentGuid}/replies` | 获取评论回复列表 | 公开 |
| `DELETE` | `/api/comments/{commentGuid}` | 删除评论 | 作者/管理员 |

#### 发表评论请求

```json
POST /api/comments
{
  "tweetGuid": "018f...",
  "content": "好文章！",
  "parentGuid": null,           // null=顶级评论，指定=回复某评论
  "replyToGuid": null           // 可选：@某人
}
```

### 3.4 互动接口 (`/api/tweets`)

| 方法 | 路径 | 描述 | 权限 |
|------|------|------|------|
| `POST` | `/api/tweets/{tweetGuid}/like` | 点赞 | 已登录用户 |
| `DELETE` | `/api/tweets/{tweetGuid}/like` | 取消点赞 | 已登录用户 |
| `POST` | `/api/tweets/{tweetGuid}/favorite` | 收藏 | 已登录用户 |
| `DELETE` | `/api/tweets/{tweetGuid}/favorite` | 取消收藏 | 已登录用户 |
| `POST` | `/api/tweets/{tweetGuid}/share` | 分享 | 已登录用户 |
| `POST` | `/api/tweets/{tweetGuid}/coin` | 投币 | 已登录用户 |
| `POST` | `/api/tweets/{tweetGuid}/view` | 记录查看 | 公开（前端自动触发） |

### 3.5 举报接口 (`/api/reports`)

| 方法 | 路径 | 描述 | 权限 |
|------|------|------|------|
| `POST` | `/api/reports` | 提交举报 | 已登录用户 |
| `GET` | `/api/reports/my` | 我的举报记录 | 已登录用户 |

#### 提交举报请求

> **前置步骤**: 若需要附带证据文件，用户先通过 `FileDev.Web.API` 上传截图/录屏/文档，获取文件 URL 后再提交举报。

```json
POST /api/reports
{
  "targetType": "Tweet",
  "targetGuid": "018f...",
  "reason": "包含不当言论",
  "category": "Harassment",   // Spam|Harassment|Violence|Porn|Other
  "evidenceUrls": [           // 可选：举报证据（图片/视频/文档）最多 5 个
    "https://filedev.notblog.com/files/evidence_001.jpg",
    "https://filedev.notblog.com/files/evidence_002.mp4"
  ]
}
```

### 3.6 审核接口 (`/api/audit` — 管理员专用，服务内校验 Admin 角色)

| 方法 | 路径 | 描述 | 权限 |
|------|------|------|------|
| `GET` | `/api/audit/tweets/pending` | 待审核推文列表 | Admin |
| `POST` | `/api/audit/tweets/{tweetGuid}/approve` | 通过审核 | Admin |
| `POST` | `/api/audit/tweets/{tweetGuid}/reject` | 拒绝审核 | Admin |
| `GET` | `/api/audit/reports/pending` | 待处理举报列表 | Admin |
| `POST` | `/api/audit/reports/{reportGuid}/resolve` | 处理举报 | Admin |

#### 审核操作请求

```json
POST /api/audit/tweets/{tweetGuid}/reject
{
  "reason": "包含违规内容，不符合社区规范"
}

POST /api/audit/reports/{reportGuid}/resolve
{
  "action": "Remove",          // Remove | Dismiss
  "note": "经核实，该推文确实包含违规内容，已下架"
}
```

---

## 4. 前端交互流程与状态管理

### 4.1 推文发布流程

```
┌─────────────────────────────────────────────────────────────────┐
│                      推文发布流程                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [用户输入内容] → [添加媒体] → [添加链接] → [选择可见性]          │
│       │                                                        │
│       ├─── 点击「发布」→ POST /api/tweets                        │
│       │         │                                               │
│       │         ▼                                               │
│       │   ┌───────────┐                                        │
│       │   │ 推文创建   │ TweetStatus = Pending                   │
│       │   └─────┬─────┘                                        │
│       │         │                                               │
│       │         ▼                                               │
│       │   ┌───────────┐                                        │
│       │   │ 自动审核   │ 敏感词过滤 + API 检查                    │
│       │   └─────┬─────┘                                        │
│       │         │                                               │
│       │    ┌────┴────┐                                         │
│       │    ▼         ▼                                          │
│       │ [通过]    [需人工审核]                                    │
│       │    │         │                                          │
│       │    ▼         ▼                                          │
│       │ Approved  进入审核队列                                    │
│       │    │         │                                          │
│       │    └────┬────┘                                          │
│       │         │                                               │
│       │         ▼                                               │
│       │   ┌──────────────────┐                                  │
│       │   │ 前端提示状态:      │                                  │
│       │   │ "审核中，仅自己可见"│                                  │
│       │   └──────────────────┘                                  │
│       │                                                        │
│       └─── 点击「保存草稿」→ POST /api/tweets/draft               │
│                   │                                             │
│                   ▼                                             │
│            TweetStatus = Draft                                  │
│            "草稿已保存，仅自己可见"                                │
│                                                                 │
│   [审核通过] → 前端推送通知 (SignalR/轮询)                        │
│   [审核拒绝] → 前端显示失败原因，允许修改后重新提交                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 前端口误群状态管理

使用 **React/Vue 状态管理**（以 React+Zustand 为例）：

```typescript
// Tweet Store
interface TweetState {
  tweets: Tweet[];              // 当前展示的推文列表
  currentTweet: Tweet | null;   // 当前查看的推文详情
  timeline: Tweet[];            // 时间线数据
  loading: boolean;
  hasMore: boolean;
  page: number;

  // Actions
  fetchTimeline: () => Promise<void>;
  loadMore: () => Promise<void>;
  createTweet: (data: CreateTweetDto) => Promise<Tweet>;
  deleteTweet: (guid: string) => Promise<void>;
  toggleLike: (guid: string) => Promise<void>;
  toggleFavorite: (guid: string) => Promise<void>;
  addComment: (tweetGuid: string, content: string) => Promise<void>;
}

// Interaction Store（乐观更新）
interface InteractionState {
  likedTweets: Set<string>;     // 已点赞推文集合
  favoritedTweets: Set<string>;
  coinedTweets: Set<string>;

  // 乐观更新模式
  optimisticLike: (tweetGuid: string) => void;
  optimisticFavorite: (tweetGuid: string) => void;
}
```

### 4.3 关键前端交互规则

| 场景 | 规则 |
|------|------|
| **推文列表加载** | 无限滚动 + 虚拟列表（每次 20 条），按时间/HotScore 排序 |
| **互动操作** | 乐观更新 UI，请求失败后回滚 |
| **查看量统计** | 推文进入视口 1 秒后自动发送 POST view 请求，同一用户同一天不重复计数 |
| **审核状态 UI** | Draft=显示编辑按钮；Pending=显示"审核中"状态条；Rejected=显示失败原因及重新提交入口 |
| **举报状态** | 举报后按钮变灰显示"已举报"；管理员处理完成后通知栏推送结果 |
| **实时通知** | 通过 SignalR Hub 推送评论/点赞/审核结果通知 |

---

## 5. 后端业务逻辑实现

### 5.1 实体设计（核心代码结构）

#### 5.1.1 Tweet 聚合根

```csharp
// Message.Domain/Entities/Tweet/Tweet.cs
namespace Message.Domain.Entities.Tweet;

public class Tweet : Entity, IAggregateRoot
{
    public Guid TweetGuid { get; init; }
    public Guid AuthorGuid { get; private set; }
    public TextContent Content { get; private set; }        // max 2000
    public IReadOnlyList<TweetMedia> Media => _media.AsReadOnly();
    public LinkMetadata? LinkMetadata { get; private set; }
    public IReadOnlySet<string> Hashtags => _hashtags;
    public TweetStatus TweetStatus { get; private set; }
    public Visibility Visibility { get; private set; }
    public bool IsPinned { get; private set; }
    public long ViewCount { get; private set; }
    public int LikeCount { get; private set; }
    public int CommentCount { get; private set; }
    public int ShareCount { get; private set; }
    public int CoinCount { get; private set; }
    public int FavoriteCount { get; private set; }
    public double HotScore { get; private set; }
    public string? AuditReason { get; private set; }
    public DateTimeOffset? PublishTime { get; private set; }

    // 私有构造函数 (EF Core)
    private Tweet() => TweetGuid = Guid.CreateVersion7();

    // 工厂方法
    public static Tweet Create(
        Guid authorGuid, TextContent content,
        IEnumerable<TweetMedia>? media = null,
        LinkMetadata? linkMetadata = null,
        ISet<string>? hashtags = null,
        Visibility visibility = Visibility.Public)
    {
        var tweet = new Tweet { ... };
        tweet.AddDomainEvent(new TweetCreatedEvent(tweet.TweetGuid, authorGuid));
        return tweet;
    }

    // 领域方法
    public void Publish() { ... AddDomainEvent(new TweetPublishedEvent(...)); }
    public void Approve(Guid auditorGuid, string? reason) { ... }
    public void Reject(Guid auditorGuid, string reason) { ... }
    public void IncrementViewCount() => ViewCount++;
    public void AddLike() => LikeCount++;
    public void RemoveLike() => LikeCount--;
    public void AddComment() => CommentCount++;
    public void RecalculateHotScore()
    {
        HotScore = LikeCount * 0.2 + FavoriteCount * 0.2 + CoinCount * 0.2
                 + ShareCount * 0.2 + ViewCount * 0.2;
    }
    public void Pin() => IsPinned = true;
    public void Unpin() => IsPinned = false;
}
```

#### 5.1.2 Comment 实体

```csharp
public class Comment : Entity
{
    public Guid CommentGuid { get; init; }
    public Guid TweetGuid { get; private set; }
    public Guid UserGuid { get; private set; }
    public Guid? ParentGuid { get; private set; }     // 父评论（null=顶级）
    public Guid? ReplyToGuid { get; private set; }    // 被@的用户
    public TextContent Content { get; private set; }
    public int LikeCount { get; private set; }
    public int ReplyCount { get; private set; }
    public bool IsDeleted { get; private set; }

    public static Comment Create(Guid tweetGuid, Guid userGuid,
        TextContent content, Guid? parentGuid, Guid? replyToGuid)
    {
        // ...
        comment.AddDomainEvent(new CommentAddedEvent(
            comment.CommentGuid, tweetGuid, userGuid, parentGuid));
        return comment;
    }

    public void SoftDelete() => IsDeleted = true;
    public void IncrementReplyCount() => ReplyCount++;
}
```

### 5.2 审核规则

#### 5.2.1 内容审核规则（TweetCreatedEventHandler 触发）

| 规则 | 检查逻辑 | 当前实现 | 后续升级 |
|------|---------|---------|---------|
| 敏感词过滤 | `ISensitiveWordFilter.FilterAsync(content)` | 接口已定义，当前仅记录日志 `[预留]` | 接入敏感词库后启用过滤 |
| 图片内容审核 | `IImageModerationService.ModerateAsync(mediaUrls)` | 接口已定义，当前仅记录日志 `[预留]` | 接入第三方审核 API |
| 频率限制 | 同一用户 1 分钟内最多发 5 条 | **已实现**，超限返回 429 | — |
| 新用户审核 | 注册不足 24 小时的用户发推→标记 Pending 进入人工队列 | **已实现** | — |

> **设计原则**: 敏感词过滤和图片审核均通过接口抽象（`ISensitiveWordFilter`、`IImageModerationService`），当前提供默认实现仅记录日志并直接放行。后续接入第三方服务时，仅需替换接口实现，无需修改领域逻辑。

#### 5.2.2 人工审核规则（Admin 角色）

| 场景 | 操作 | 通知 |
|------|------|------|
| 推文通过 | Tweet.Approve() → TweetStatus = Approved | 通知作者「推文已通过审核」 |
| 推文拒绝 | Tweet.Reject() → TweetStatus = Rejected | 通知作者拒绝原因 |
| 举报属实 | Report.Resolve(Removed) → 下架推文/隐藏评论 | 通知被举报方+举报方 |
| 举报不实 | Report.Resolve(Rejected) → 不处理 | 仅通知举报方 |

#### 5.2.3 审核权限定义

不新增专用角色，复用现有 Identity 的 Admin 角色。审核权限在服务内通过 `ICurrentUserService.IsAdmin()` 判断（角色信息由网关通过 `X-User-Roles` Header 注入）：

```
审核权限判断逻辑:
  Admin 角色 → 可执行所有审核操作（approve / reject / resolve report）
  非 Admin  → 拒绝审核操作（返回 403）
  
  ICurrentUserService 从网关 Header 获取:
    X-User-Id    → 当前用户 GUID
    X-User-Roles → 角色列表（如 "Admin,User"）
```

### 5.3 热度计算

#### 5.3.1 计算公式

```
HotScore = LikeCount × 0.20 + FavoriteCount × 0.20 + CoinCount × 0.20
         + ShareCount × 0.20 + ViewCount × 0.20
```

#### 5.3.2 更新策略

| 触发事件 | 更新方式 | 说明 |
|---------|---------|------|
| 点赞/取消点赞 | 实时更新 LikeCount + 重新计算 | 同步 |
| 收藏/取消收藏 | 实时更新 FavoriteCount + 重新计算 | 同步 |
| 分享 | 实时更新 ShareCount + 重新计算 | 同步 |
| 投币 | 实时更新 CoinCount + 重新计算 | 同步 |
| 查看量 | 批量更新（每 10 次查看触发一次计算） | 异步 |
| 定时任务 | 每 5 分钟全量重算热门推文 HotScore | 异步 |

#### 5.3.3 实现

```csharp
// TweetInteractionEventHandler.cs — 实时计算
public async Task Handler(InteractionEvent notification, CancellationToken ct)
{
    var tweet = await _tweetRepository.FindOneByTweetAsync(notification.TweetGuid);
    if (tweet is null) return;

    tweet.RecalculateHotScore();
    await _tweetRepository.UpdateByTweetAsync(tweet);
    await _tweetRepository.UnitOfWork.SavaChangesAsync(ct);
}

// HotScoreBackgroundService.cs — 定时全量重算
// 每 5 分钟查询近 7 天内有互动的推文，重新计算热度
```

### 5.4 所有权校验

由于认证由 API 网关统一处理，本服务仅验证**资源所有权**——即当前操作者是否为资源的合法拥有者。用户身份通过网关注入的 Header `X-User-Id` 获取。

```csharp
// Message.Web.API/Middleware/UserContextMiddleware.cs
// 从 Header 提取用户信息，设置 ICurrentUserService
public class UserContextMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        if (Guid.TryParse(userId, out var guid))
        {
            var currentUser = context.RequestServices.GetRequiredService<ICurrentUserService>();
            currentUser.SetUser(guid,
                context.Request.Headers["X-User-Roles"].ToString().Split(','));
        }
        await _next(context);
    }
}

// Controller 中的所有权校验模式
// — 不依赖 [Authorize] 策略，而是在 Provider 层校验所有权
public class TweetsController : ControllerBase
{
    [HttpDelete("{tweetGuid}")]
    public async Task<ApiResponse> Delete(Guid tweetGuid)
    {
        var currentUserGuid = _currentUserService.GetUserId();
        var tweet = await _tweetProvider.GetTweetAsync(tweetGuid);
        
        // 所有权确认：只有作者本人或 Admin 可以删除
        if (tweet.AuthorGuid != currentUserGuid && !_currentUserService.IsAdmin())
            throw new UnauthorizedAccessException("无权操作此推文");
        
        await _tweetProvider.DeleteTweetAsync(tweetGuid);
        return ApiResponse.Ok();
    }
}

// ITweetPermissionService 接口 — 封装所有权判断逻辑
public interface ITweetPermissionService
{
    bool IsOwner(Guid resourceOwnerGuid);              // 是否资源所有者
    bool CanModify(Guid resourceOwnerGuid);            // 所有者 或 Admin
    bool IsAdmin();                                     // 是否为管理员
    bool CanAudit();                                    // Admin
    bool CanResolveReport();                            // Admin
}
```

> **设计原则**: 网关统一认证 → 服务仅获取用户身份 → 业务层校验所有权。不做重复的 Token 解析或 Policy 验证。

### 5.5 媒体文件存储集成（FileDev.Web.API）

推文的媒体文件（图片/视频）存储统一通过 `FileDev.Web.API` 项目提供的接口完成。

#### 5.5.1 FileDev.Web.API 接口梳理

FileDev.Web.API 是一个独立的文件存储微服务，提供以下核心 API（路由前缀 `/api/filestorage`）：

| 端点 | 方法 | 说明 | 使用场景 |
|------|------|------|---------|
| `POST /api/filestorage/Notfile/uploadFile` | multipart/form-data | 小文件直传 | < 5MB 图片上传 |
| `POST /api/filestorage/chunk/init` | JSON | 初始化分片上传 | > 5MB 的视频/图片 |
| `POST /api/filestorage/chunk/upload` | multipart (`fileKey`, `chunkIndex`, `chunkContent`) | 上传分片 | 大文件分片 |
| `POST /api/filestorage/chunk/merge` | JSON (`fileKey`) | 合并分片 | 分片上传完成 |
| `GET /api/filestorage/chunk/status/{fileKey}` | query | 查询分片状态 | 断点续传 |
| `POST /api/filestorage/chunk/cancel/{fileKey}` | query | 取消分片上传 | 用户取消 |
| `POST /api/filestorage/stream/upload` | stream + headers | 流式上传 | 流式场景 |
| `POST /api/filestorage/dedup/check` | JSON (`fileMd5`, `fileSize`) | 重复文件检测 | 秒传检查 |
| `GET /api/filestorage/Notfile/findFile?fileId=` | query | 按 ID 查询文件 | 获取文件信息 |

**认证方式**: 网关统一注入 `X-User-Id` Header，FileDev.Web.API 同样通过网关认证，服务间调用透传用户身份。

**支持的文件类型** (`FileType` 枚举):

| 扩展名 | FileType | 说明 |
|--------|----------|------|
| `.jpg .jpeg .png .gif .bmp .webp .svg .ico` | `FileImage` | 图片 |
| `.mp4 .avi .mkv .mov .wmv .flv .webm` | `FileVideo` | 视频 |
| 其他 | `FileFile` | 通用文件 |

#### 5.5.2 Tweet 媒体上传流程

```
┌──────────────────────────────────────────────────────────────────┐
│                    推文媒体上传流程                                │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  前置条件: 网关已认证，Header 含 X-User-Id                             │
│                                                                  │
│  Step 1: 用户选择媒体文件                                         │
│     │                                                            │
│     ├── 图片 (< 5MB) ──→ 直接调 FileDev /Notfile/uploadFile      │
│     │                         │                                  │
│     │                         └──→ 返回 fileUri                  │
│     │                                                            │
│     └── 视频 / 大图片 (≥ 5MB) ──→ 分片上传流程:                    │
│              │                                                   │
│              ├── POST /chunk/init     → 获取 fileKey             │
│              ├── POST /chunk/upload   → 逐个上传分片              │
│              ├── GET  /chunk/status   → 确认完整性                │
│              └── POST /chunk/merge    → 合并返回 fileUri         │
│                                                                  │
│  Step 2: 创建推文                                                 │
│     POST /api/tweets                                             │
│     {                                                            │
│       "content": "...",                                          │
│       "mediaGuids": ["media-guid-1", "media-guid-2"]            │
│     }                                                            │
│                                                                  │
│  Step 3: 视频压缩（异步）                                          │
│     视频上传完成后，FileDev 后台自动压缩为 H.264 720p              │
│     TweetMedia 中保存原视频 URI + 缩略图 URI                      │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

#### 5.5.3 集成代码结构

```csharp
// Message.Domain/IServices/IFileStorageService.cs — 领域接口
public interface IFileStorageService
{
    /// <summary>小文件直传（< 5MB）</summary>
    Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName,
        string contentType, CancellationToken ct);

    /// <summary>初始化分片上传</summary>
    Task<ChunkInitResult> InitChunkUploadAsync(string fileName, long totalSize,
        string fileMd5, CancellationToken ct);

    /// <summary>合并分片</summary>
    Task<FileUploadResult> MergeChunksAsync(string fileKey, CancellationToken ct);
}

// Message.Infrastructure/Services/FileDevStorageService.cs — 基础设施实现
public class FileDevStorageService : IFileStorageService
{
    private readonly HttpClient _httpClient;

    // 调用 FileDev.Web.API 的 HTTP 接口实现文件存储
    // ...
}
```

#### 5.5.4 文件大小限制

| 类型 | 限制 | 说明 |
|------|------|------|
| 图片 | ≤ 10MB | 单张最大 10MB，单条推文最多 9 张 |
| 视频 | ≤ 100MB | 单个视频最大 100MB，上传后压缩为 H.264 720p |
| 分片大小 | 5MB（默认 5,242,880 字节） | FileDev 默认分片大小 |

---

## 6. 异常处理与日志记录策略

### 6.1 异常分层处理

```
┌──────────────────────────────────────────────────────┐
│                  ExceptionHandlingMiddleware          │
│  (全局异常捕获 → 统一 ApiResponse.Error 响应)          │
├──────────────────────────────────────────────────────┤
│  Controller / Provider 层                            │
│  - 参数校验 → 400 Bad Request                        │
│  - 所有权校验失败 → 403 Forbidden                     │
│  - 业务校验 → 自定义异常映射                          │
├──────────────────────────────────────────────────────┤
│  Domain 层                                           │
│  - 业务规则校验 → 抛出 DomainException               │
│  - 状态机校验 → 抛出 InvalidOperationException        │
├──────────────────────────────────────────────────────┤
│  Infrastructure 层                                   │
│  - 数据库异常 → DbUpdateException → 500              │
│  - 文件存储异常 → StorageException → 500             │
│  - 第三方API异常 → ExternalServiceException → 502    │
└──────────────────────────────────────────────────────┘
```

### 6.2 自定义异常

```csharp
public class DomainException : Exception
{
    public string ErrorCode { get; }
    public DomainException(string errorCode, string message) : base(message)
        => ErrorCode = errorCode;
}

public class TweetNotFoundException : DomainException
{
    public TweetNotFoundException(Guid tweetGuid)
        : base("TWEET_NOT_FOUND", $"推文 {tweetGuid} 不存在") { }
}

public class TweetStatusException : DomainException
{
    public TweetStatusException(string action, TweetStatus current)
        : base("TWEET_STATUS_ERROR", $"当前状态 {current} 不允许执行 {action} 操作") { }
}

public class DuplicateInteractionException : DomainException
{
    public DuplicateInteractionException(string type)
        : base("DUPLICATE_INTERACTION", $"不能重复{type}") { }
}
```

### 6.3 通知渠道与日志策略

**通知发送策略**: 审核结果和举报处理结果同时通过站内通知 + Email 双渠道发送。

| 通知场景 | 站内通知（TweetNotification） | Email | 说明 |
|---------|:---:|:---:|------|
| 推文审核通过 | ✅ | ✅ | 通知作者 |
| 推文审核拒绝 | ✅ | ✅ | 通知作者 + 拒绝原因 |
| 举报被处理（下架） | ✅ | ✅ | 通知被举报方 + 举报方 |
| 举报被驳回 | ✅ | ✅ | 仅通知举报方 |
| 评论被回复 | ✅ | ❌ | 仅站内通知 |
| 推文被点赞 | ✅ | ❌ | 仅站内通知 |

**通知频率限制**: 同一用户每分钟最多接收 10 条站内通知推送。

#### 日志级别与字段

| 级别 | 场景 | 关键字段 |
|------|------|---------|
| `Information` | 推文创建、发布、审核通过/拒绝 | TweetGuid, AuthorGuid, AuditStatus |
| `Information` | 互动操作（点赞/收藏/分享/硬币） | TweetGuid, UserGuid, InteractionType |
| `Warning` | 重复互动尝试 | TweetGuid, UserGuid, Type |
| `Warning` | 自动审核命中敏感词 | TweetGuid, MatchedWord |
| `Error` | 第三方审核API调用失败 | TweetGuid, ErrorMessage, RetryCount |
| `Error` | 文件上传失败 | FileName, FileSize, ErrorMessage |

**结构化日志格式**:

```csharp
_logger.LogInformation(
    "[{Time}] 推文审核通过: TweetGuid={TweetGuid}, Auditor={AuditorGuid}, PublishTime={PublishTime}",
    DateTimeOffset.UtcNow, tweetGuid, auditorGuid, publishTime);
```

---

## 7. 测试计划

### 7.1 单元测试

| 测试对象 | 测试范围 | 用例数（预估） |
|---------|---------|:---:|
| `Tweet` 实体 | 创建、状态流转、计数操作、HotScore 计算 | 15 |
| `Comment` 实体 | 创建、软删除、层级校验（最大 2 层回复） | 8 |
| `TweetAuditLog` 实体 | 审核记录创建 | 3 |
| `TweetReport` 实体 | 举报创建、状态流转 | 5 |
| `TweetProvider` | 推文 CRUD、状态切换、互动操作 | 20 |
| `CommentProvider` | 评论 CRUD、回复层级处理 | 10 |
| `AuditProvider` | 审核通过/拒绝、举报处理 | 12 |
| `HotScore` 计算 | 公式校验、批量更新 | 5 |
| **总计** | | **~78** |

**测试文件位置**: `Message.Domain.Tests/` 和 `Message.Web.API.Tests/`

### 7.2 集成测试

| 场景 | 描述 | 验证点 |
|------|------|--------|
| 推文完整发布流程 | 创建→审核→发布→查询 | 状态流转正确，Approved 后对他人可见 |
| 互动联动 | 点赞→取消→收藏→取消→分享→硬币 | 计数准确，HotScore 实时更新 |
| 举报闭环 | 举报→管理员处理→通知双方 | 通知正确送达，内容正确下架 |
| 评论嵌套 | 顶级→回复→回复的回复 | 层级正确，计数更新 |
| 并发点赞 | 10 用户同时点赞 | 不超卖，计数准确 |

### 7.3 用户验收测试

| 用例 | 步骤 | 预期结果 |
|------|------|---------|
| 发布纯文本推文 | 输入文本→发布→等待审核 | 审核通过后推文公开展示 |
| 发布带图推文 | 上传 3 张图→输入文字→发布 | 图片正常显示，顺序正确 |
| 审核失败修改重发 | 收到拒绝通知→修改内容→重新提交 | 新推文再次进入审核流程 |
| 举报推文 | 查看推文→点击举报→选择原因→提交 | 举报成功提交，等待处理 |
| 管理员处理举报 | 查看举报列表→核实内容→选择处理 | 处理后双方收到通知 |
| 查看热门推文 | 进入热门页→查看榜单 | 按 HotScore 排序 |

---

## 8. 部署与发布流程

### 8.1 环境规划

| 环境 | 用途 | 数据库 | 部署方式 |
|------|------|--------|---------|
| Development | 本地开发 | PostgreSQL (Docker) / InMemory | dotnet run |
| Staging | 预发布验证 | PostgreSQL (测试实例) | Docker Compose |
| Production | 正式环境 | PostgreSQL (主从) | Kubernetes / Docker Swarm |

### 8.2 性能指标

| 指标 | 目标值 | 说明 |
|------|--------|------|
| **推文时间线查询 QPS** | ≥ 200 QPS | 热门推文列表 + Redis 缓存加速 |
| **推文发布吞吐** | ≥ 50 TPS | 含媒体上传（异步处理） |
| **互动操作 QPS** | ≥ 500 QPS | 点赞/收藏等轻量操作 |
| **API 响应时间 P99** | ≤ 500ms | 时间线查询；互动操作 ≤ 100ms |
| **HotScore 重算延迟** | ≤ 5min | 定时全量重算间隔 |

### 8.3 数据备份策略

| 项目 | 策略 |
|------|------|
| 备份频率 | **每日全量备份**（凌晨 3:00 UTC 自动执行） |
| 备份类型 | PostgreSQL `pg_dump` 全量导出 + WAL 连续归档 |
| 保留期限 | 最近 30 天 |
| 存储位置 | 本地 + 异地（对象存储） |
| 恢复验证 | 每月一次恢复演练 |

### 8.4 数据库迁移

使用 EF Core Migrations 管理数据库变更：

```bash
# 创建迁移
dotnet ef migrations add AddTweetFeature -p Message.Infrastructure -s Message.Web.API

# 应用迁移
dotnet ef database update -p Message.Infrastructure -s Message.Web.API
```

### 8.5 部署检查清单

- [ ] 数据库迁移已执行并验证
- [ ] 网关已配置 `X-User-Id` / `X-User-Roles` Header 注入
- [ ] FileDev.Web.API 服务正常运行且网络可达
- [ ] Admin 角色已在 Identity 中配置
- [ ] SignalR 连接正常
- [ ] Redis 缓存配置正确
- [ ] API 限流配置已应用（200 QPS 目标）
- [ ] 每日全量备份任务已配置
- [ ] 监控告警已配置

### 8.6 回滚方案

| 问题 | 回滚方式 |
|------|---------|
| 数据库迁移失败 | 执行 `dotnet ef database update <PreviousMigration>` |
| 新功能严重 Bug | K8s 回滚到上一版本，暂不下线数据库表 |
| FileDev.Web.API 故障 | 媒体上传降级为本地暂存，待恢复后补传 |
| 审核逻辑异常 | 配置开关关闭人工审核，所有推文自动通过 |

---

## 9. 项目进度与里程碑规划

### 9.1 开发阶段

```
Phase 1: 基础推文发布 (Week 1-2)
├── Day 1-2: Domain 层实体 + 枚举 + 事件定义
├── Day 3-4: Infrastructure 层 EF 配置 + 仓储实现
├── Day 5-6: Web.API 层 Provider + Controller + DTO
├── Day 7-8: 推文发布 + 草稿 API 联调
├── Day 9:  单元测试
└── Day 10: 代码评审

Phase 2: 互动功能 (Week 3)
├── Day 1-2: 评论实体 + API
├── Day 3-4: 点赞/收藏/分享/硬币 + HotScore 计算
├── Day 5:   查看量统计 + 去重逻辑
└── Day 6-7: 单元测试 + 集成测试

Phase 3: 审核 + 举报 (Week 4)
├── Day 1-2: 审核实体 + 审核流程 API
├── Day 3-4: 举报实体 + 举报流程 API
├── Day 5:   通知系统集成（站内 + Email 双通道）
├── Day 6:   所有权校验中间件 + Admin 角色判断
└── Day 7-8: 单元测试 + 集成测试

Phase 4: 联调验收 (Week 5)
├── Day 1-2: 前端对接 + 联调
├── Day 3-4: 性能测试 + 优化
├── Day 5:   用户验收测试 (UAT)
└── Day 6-7: Bug 修复 + 文档完善
```

### 9.2 里程碑

| 里程碑 | 时间 | 交付物 | 验收标准 |
|--------|------|--------|---------|
| M1: 基础推文可用 | Week 2 | 推文发布 API | 可通过 API 发布纯文本+图片推文 |
| M2: 互动完整 | Week 3 | 互动 API + 热度 | 所有互动操作可用，HotScore 计算正确 |
| M3: 审核+举报上线 | Week 4 | 审核+举报 API | 完整审核链路+举报闭环 |
| M4: 验收完成 | Week 5 | 完整系统 | 所有 UAT 用例通过 |

---

## 10. 决策记录

以下事项已在评审阶段确认，作为开发的决策依据：

| # | 事项 | 决策 | 影响范围 |
|---|------|------|---------|
| 1 | **媒体文件存储方案** | 使用 `FileDev.Web.API` 项目接口（分片上传 + 流式上传） | `TweetProvider` 媒体上传流程 |
| 2 | **图片审核服务** | 定义 `IImageModerationService` 接口，当前默认实现仅记录日志放行，后续替换实现即可 | 审核流程可扩展性 |
| 3 | **审核角色定义** | 不新增 Moderator，复用 Admin 角色，服务内通过 `ICurrentUserService.IsAdmin()` 判断 | 权限校验 |
| 4 | **敏感词过滤** | 定义 `ISensitiveWordFilter` 接口，当前默认实现仅记录日志放行，后续接入词库 | 审核流程可扩展性 |
| 5 | **视频处理** | 视频上传后压缩为 H.264 720p（由 FileDev.Web.API 后台异步处理） | `TweetMedia` 处理 |
| 6 | **通知渠道** | 审核和举报结果同时发送站内通知 + Email 邮件 | `TweetNotification` + `IEmailSender` |
| 7 | **历史数据迁移** | 不需要 | 无 |
| 8 | **性能指标** | 推文时间线查询 ≥ 200 QPS（Redis 缓存加速） | 缓存策略、索引设计 |
| 9 | **多语言支持** | 预留 `ILocalizationService` 接口，当前使用中文硬编码，后续接入 | 通知模板 |
| 10 | **数据备份** | 每日全量备份（PostgreSQL pg_dump + WAL 归档），保留 30 天 | 运维部署 |

---

> **文档状态**: 已评审，可进入 Phase 1 开发  
> **下一步**: Phase 1 — Domain 层实体 + 枚举 + 事件定义
