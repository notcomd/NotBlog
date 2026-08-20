# NotBlog 交接文档（2026-08-21）

> 覆盖本次开发周期全部工作：Markdown 文件化存储重构（三阶段）、封面属性、Markdown 详情页、
> 评论回复逻辑修复、语音/视频通话前端接入。供后续接手人快速了解现状、契约与遗留事项。

---

## 1. 总体概况

| 领域 | 状态 | 说明 |
|---|---|---|
| Markdown 服务文件化重构 | ✅ 代码完成 | 正文改文件存储（FileDev gRPC），实体只存元数据 |
| Redis 热点榜 | ✅ 代码完成 | 互动50% / 浏览30% / 时间衰减20%，PG 唯一事实源 |
| 封面属性 CoverUrl | ✅ 代码完成 | 实体/命令/DTO/迁移全链路 |
| Markdown 详情页 | ✅ 完成 | 前端 `/markdown/:guid`（后端已提交） |
| 评论回复逻辑修复 | ✅ 完成 | 前端 CommentSection/CommentItem |
| 语音/视频通话前端 | ✅ 完成 | WebRTC over SignalR（CallHub），前后端联调待验 |
| **数据库迁移** | ⚠️ **未应用** | PG 未运行；启动后端自动应用，**应用前先备份** |
| 端到端验证 | ⚠️ 未做 | 需 Docker Desktop + 整栈启动 |

### 提交记录（后端 F:\NotBlog，本地 master，未推送）

```
3959b5c6 feat(markdown): 详情响应补充 MarkUserGuid（作者标识，详情页展示用）
974198ed feat(markdown): MarkDown 实体添加封面属性（CoverUrl）+ 标签集合改 List
38e4bfb2 fix(tests): Message.Tests 缓存 mock 适配 IRedisCacheService（存量漂移）
9b497cd1 feat(markdown): Redis 热点榜（互动50%/浏览30%/时间衰减20% + ZSet + 定时重建）
47d0e7d8 chore(apphost): Markdown 接入 FileDev 服务发现 + RabbitMQ 端口调整
e17e87fa feat(markdown): FileDev gRPC 正文存储（服务级 JWT + 配置切换）
176493d5 feat(markdown): 文档/评论交互端点（浏览/点赞/分享/硬币/踩）
8ff29204 test(markdown): 文件化重构测试适配（40 用例全绿）+ 设计文档
e547242c feat(markdown): 文件化存取链路（本地磁盘存储 + /content 端点 + 收藏计数钩子）
bd570e17 feat(markdown): 基础设施与迁移（文件列/计数列/新表）
b7e40e14 refactor(markdown): 文档文件化领域模型重构（MarkQuote 拆文档级/评论级）
```

### 提交记录（前端 F:\NotBlog\notblog-ui，分支 fix/api-contract-alignment，未推送）

```
b3582a6 fix(comment) + feat(call): 评论回复逻辑修复 + 语音/视频通话接入（CallHub）
bfa201f feat(markdown): Markdown 详情页（/markdown/:guid）
514ba4c feat(markdown): 发布页改走 Markdown 服务（标题输入 + 封面 + 文件上传）
```

> ⚠️ 前端分支上有用户大量进行中改动（40+ 文件，CircleEditor/ExploreView 删除等）未提交，
> 已按文件粒度隔离，仅上述 3 个 commit 属于本次工作。

---

## 2. Markdown 文件化重构（阶段 1）

**设计文档**：`F:\NotBlog\Markdown-FileStorage-Redesign.md`（v0.1，含决策 D1-D6）

### 核心变更

- **`MarkDown` 实体**：删除 `MarkDownContent`（正文列），新增文件元数据：
  `FileId` / `FileUri` / `FileSize` / `FileExt` / `MarkDownHash` + `CoverUrl`（封面）
- **计数拆分**：`MarkQuote`（文档级：Love/View/Favorite/Share/Coin/HeatScore）
  + `ReviewQuote`（评论级：Love/View/Review/Dislike）——EF `OwnsOne` 同类型只能映射一次，故拆两类
- **新实体**：`MarkDocumentLike`、`MarkCoin`、`MarkReviewDislike`
  （(文档GUID, 用户GUID) 唯一约束防重，点赞/打赏/踩计数与记录同事务）
- **`MarkDownTagboard`：`HashSet<string>` → `List<string>`**（用户进行中改动，
  Builder 与 EntityConfig JSON 转换器已同步）
- **正文存储抽象**：`IMarkdownContentStore`（Domain 接口）：
  - `LocalMarkdownContentStore`（本地磁盘 `markdown-files/`，开发/单机）
  - `FileDevMarkdownContentStore`（gRPC，生产，默认）
  - 切换开关：`appsettings` → `MarkdownContent:Provider`（`Local` / 缺省=FileDev）

### 测试

- `Markdown.Tests`：**49/49 全绿**
  （MarkQuoteTests 重写、ReviewQuoteTests 新建、MarkDownTests 文件化适配、
  MarkdownHeatFormulaTests 公式 7 例）
- 全 sln build：**0 错误 0 警告**（此前 Message.Tests 长期编译失败，已修复）

### 迁移（关键 ⚠️）

两个待应用迁移（PG 未运行，启动后端自动应用）：
1. `20260820201833_MarkdownFileStorage`：
   - MarkDown 删 `MarkDownContent` 列（**先备份存量数据！**）
   - MarkReview 删 `CommentCount`/`ShareCount`，加 `DislikeCount`（删+加，非重命名——EF 自动重命名会污染历史数据，已手工修正）
   - 3 张新表（MarkDocumentLike/MarkCoin/MarkReviewDislike）+ 计数列
2. `20260820225319_AddMarkDownCoverUrl`：MarkDown 加 `CoverUrl varchar(2048)` 可空

---

## 3. Redis 热点榜（阶段 3）

- **公式**（用户定稿）：
  `HeatScore = 0.5×log10(1+Love+2·Fav+3·Share+5·Coin) + 0.3×log10(1+View) + 0.2×exp(-ageDays/7)`
- **领域方法**：`MarkDown.RecalculateHotScore(now)` → 写回 `MarkQuote.HeatScore` 列
- **`MarkdownHotBoardService`**（Web.API）：
  - 读榜：ZSet `markdown:hot:all` 直读（ZREVRANGE）
  - 空榜**单飞重建**（SETNX `markdown:hot:rebuild:lock` 30s 防击穿）
  - Redis 故障自动**降级 DB 实时计算**（PG 是唯一事实源，Redis 只是可重建投影）
  - 写侧钩子 `UpdateScoreAsync`：端点/命令计数变更后重算（DB 列 + ZADD 同步）
- **定时重建** `MarkdownHeatRebuildBackgroundService`：每 10 分钟全量重算兜底收敛（防重入）
- **浏览防刷**：`POST /{guid}/view` 登录用户 24h Redis Set 去重（`markdown:viewed:{guid:N}`）
- **端点**：`GET /api/markdown/hot?take=N`
- CacheMemory 接入：非 Local 模式 `builder.AddCacheMemory("Redis")`（AppHost 已 `WithReference(redis)`）

---

## 4. FileDev gRPC 正文存储（阶段 2 关键设计）

**两个真实阻塞点及解法**：

1. **匿名读取公开文档**：FileDev gRPC 强制 JWT。→ **服务级 JWT**：
   固定服务账号 `11111111-1111-1111-1111-111111111111`（`FileDevMarkdownContentStore.ServiceAccountId`），
   用共享 `JWT_PRIVATE_KEY`（系统环境变量）签发，FileDev 同 JwtOptions 校验；
   token 缓存至过期前 1 分钟自动刷新。
2. **文件权限模型**：FileDev 下载校验 = FILE_PRIVATE 仅所有者 → 文档文件以 **FILE_PUBLIC**
   上传（任何已认证调用者可下载），内容权限完全由 Markdown 服务层把关
   （HasPermission + 审核门控，与详情一致）。私有文档的"私有"由 Markdown 层保证，
   文件 GUID 不可枚举。

配套：
- 更新/还原命令成功后**清理旧正文文件**（历史快照已存 DB 全文，防 FileDev 文件堆积）
- 服务发现：`filedev-web-api`（AppHost `WithReference(filedev)` 已加）
- 单机调试（无 FileDev）：`MarkdownContent:Provider=Local`

---

## 5. Markdown 文档交互端点（阶段 2）

| 端点 | 认证 | 说明 |
|---|---|---|
| `POST /{guid}/view` | 匿名可 | 浏览 +1（登录用户 24h 防刷） |
| `POST /{guid}/like` / `unlike` | 需要 | 点赞/取消（唯一约束防重） |
| `POST /{guid}/share` | 需要 | 分享 +1 |
| `POST /{guid}/coin` | 需要 | 打赏，body `{amount: 1~100}` |
| `POST /reviews/{reviewGuid}/dislike` / `undislike` | 需要 | 评论踩 |
| `GET /{guid}/content` | 匿名可（权限校验内） | 正文（文件流） |
| `GET /hot?take=N` | 匿名可 | 热点榜 |

全部返回 `ApiResponse<long>`（最新计数）。热点写侧钩子已挂 view/like/unlike/share/coin + 收藏命令。

---

## 6. 前端 Markdown 发布与详情页

### 发布（`src/components/publish/MarkdownEditorPage.vue`）

- **标题输入框**（必填，maxlength 200）——替代原从内容首行提取
- **封面**（必填，3:4 竖版，≤10MB 经 `/api/files/upload-image` 上传 FileDev）
- 发布 → `createMarkdownDoc`：`POST /api/markdown`
  `{ name, content, coverUrl, auth }`（auth: `public`/`private`）
- 移除「发布到社区」下拉（Markdown 服务无圈子概念）
- 发布成功 → `/markdown/:guid`

### 详情页（`src/views/MarkdownDetailView.vue`，路由 `/markdown/:guid`）

- 标题/作者（GUID 前 8 位）/时间/标签/私密徽标 + 封面 + 正文渲染
  （`renderMarkdown`，`.markdown-body`）
- 操作区：点赞（like/unlike）、收藏（favorite/unfavorite，初始状态从
  `GET /api/favorites` 比对——详情接口无 isLiked/isFavorited 字段）、浏览计数
- 进入页面自动 `POST /view`

### 详情接口契约（后端 `GET /api/markdown/{guid}`）

`MarkdownResponse`：`markDownGuid / markUserGuid / name / hash / fileId / fileSize / fileExt /
coverUrl / tags[] / auth / createAt / updateAt / quote{loveCount, favoriteCount, shareCount,
coinCount, viewCount, heatScore, totalInteractions}`

---

## 7. 评论回复逻辑修复（详情页评论）

**问题清单与修复**（`src/components/post/CommentSection.vue` + `CommentItem.vue`）：

| 问题 | 修复 |
|---|---|
| `isMine` 写死 `userGuid === 'me'` → 删除按钮永不显示 | 比对 auth store 登录用户 id |
| 回复子评论提交 `parentGuid=子评论` → 后端 400「嵌套层级不能超过2层」 | 折叠到顶层：`parentGuid=顶层`、`replyToGuid=被回复对象`；CommentItem 递归传 `rootComment`，事件携带 `{root, target}` |
| 发布接口不返回评论实体 → 乐观插入永不生效 | 发布成功 `loadMore(true)` 重新加载 |
| 删除子评论只过滤顶层 items → 界面不消失 | 删除后重新加载列表 |
| 总数读 `data.total`（后端是 `TotalCount`）→ 恒显示当前页长度 | `data.totalCount ?? data.total` |

**后端评论模型**（Message 服务，未改动）：
- `GET /api/comments/tweet/{guid}` 只返回**顶层评论**（ParentGuid==null）
- `GET /api/comments/{guid}/replies` 懒加载子回复
- 嵌套限制 2 层：parentGuid 的 parentGuid 不能再有 parentGuid
- `user.userName` 后端返回空（无联查）→ 前端显示「用户」

---

## 8. 语音/视频通话前端（CallHub）

**后端已实现**（Message 服务，本次仅前端跟进）：
- `CallHub`（`/CallHub`，JWT 认证）：StartCall / AcceptCall / RejectCall / JoinCall / HangUp /
  CancelCall / GetCall / SendSignal
- 事件：`IncomingCall` / `CallStarted` / `CallEnded(reason)` / `MemberJoined` / `MemberLeft` /
  `MemberRejected` / `Signal`
- 状态存 Redis（`CallSessionStore`）：忙线判定、30s 响铃超时兜底、断线自动离开
- 拓扑：Mesh 全网状——**新加入成员向每个既有成员发 offer**，服务端只转发信令

**前端实现**：
| 文件 | 职责 |
|---|---|
| `src/socket/callSignalR.js` | CallHub 独立连接（与 MessageHub 分离，JWT + 自动重连） |
| `src/stores/call.js` | 状态机 idle/ringing/incoming/active/ended + WebRTC（RTCPeerConnection 按远端用户管理 + ICE 转发 + 媒体获取/清理）+ 服务端事件绑定 |
| `src/components/chat/CallPanel.vue` | 全局通话面板（AppLayout 挂载）：来电（接听/拒绝）、呼叫中（取消）、通话中（视频网格 + 本地小窗 + 静音/摄像头/挂断 + 计时）、结束原因展示 |
| `ChatConversation.vue` | 会话头部语音/视频按钮（通知会话除外，`canCall`） |

**联调注意**：
- 通话发起需 `getUserMedia` 权限（localhost 是 secure context ✓，部署需 HTTPS）
- STUN：`stun:stun.l.google.com:19302`（公网部署建议换自建 TURN，否则 NAT 穿透受限）
- 刷新页面即退出通话（无恢复逻辑，服务端 OnDisconnected 自动移出成员）
- 群组通话：新成员加入后向既有成员逐个发 offer（既有成员等 offer 即可，无需主动建连）

---

## 9. 遗留事项 / 后续建议

1. **数据库迁移未应用**（PG 未运行）：
   - 启动后端（Aspire AppHost）自动应用 2 个迁移
   - **`MarkdownFileStorage` 会删 `MarkDownContent` 列——应用前若本地库有要保留的测试文档先导出**
2. **端到端验证未做**：Docker Desktop 未运行。验证路径：
   启动 Docker → 启动 AppHost（JWT_PRIVATE_KEY 在系统环境变量，`reg query HKCU\Environment` 可读，
   勿写盘）→ 冒烟：创建文档 → `/content` → 点赞/浏览 → `/hot` → 通话（两个浏览器互呼）
3. **Markdown 文档列表页**未做（用户确认后续需求）——发布后跳详情页，信息流/列表暂不展示文档
4. **Markdown 详情页评论**未做（现只有展示 + 点赞/收藏）
5. 前端仓库 `fix/api-contract-alignment` 分支有用户 40+ 文件进行中改动未提交
6. 通话 WebRTC 群组 Mesh 与 TURN 配置需真实环境验证（本地 P2P 直连通常可用）
7. `MarkdownSearchQueryHandler` 仅匹配名称（正文已文件化，全文搜索需后续接入 FileDev/向量检索）

---

## 10. 运行环境备忘

- 后端整栈：`dotnet run --project F:\NotBlog\NotBlog.AppHost`（容器模式需 Docker Desktop；
  需注入 `JWT_PRIVATE_KEY` 环境变量）
- 单机 Markdown 调试：`MarkdownContent:Provider=Local`（本地磁盘存储，无需 FileDev）
- 前端：`npm run serve`（代理 `/api` 与 `/MessageHub` 到 `https://localhost:5000`；
  **CallHub 已随 `/MessageHub` 同代理规则**——vue.config.js 用 ws:true 通配，无需改动）
- 测试：`dotnet test Markdown.Tests`（49 例）、`dotnet test Message.Tests`（211 例）、
  `npm run lint` / `npm run build`（前端 0 错误）
