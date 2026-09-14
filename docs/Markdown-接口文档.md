# Markdown 博客服务 · API 接口文档

> HTTP REST 接口说明。统一前缀 `/api`；认证端点需 `Authorization: Bearer <JWT>`。
> 所有响应统一包装：`{"success": bool, "message": string?, "data": T?}`（`ApiResponse<T>`）。

---

## 0. 通用约定

### 0.1 响应包装

```json
// 成功示例
{ "success": true, "message": "点赞成功", "data": 12 }
// 失败示例（404）
{ "success": false, "message": "文章不存在" }
```

### 0.2 认证

- 需要认证的端点：请求头携带 `Authorization: Bearer <token>`；未认证返回 `401 Unauthorized`。
- 越权（非所有者操作私有/他人资源）：业务层返回 `403 Forbidden` 或 `404`（读侧门控一律 404 防泄露）。
- 幂等：写操作写入端点接受请求头 `Idempotency-Key`（Guid），缺失时服务端自动生成（向后兼容）；相同 Key 的重复请求返回首次执行结果。

### 0.3 通用错误状态码

| 状态码 | 含义 |
| --- | --- |
| 200 / 201 | 成功（创建类 201） |
| 400 | 参数校验失败（标签超限、内容为空、金额越界、非法权限类型等） |
| 401 | 未认证 |
| 403 | 无权限（非作者越权操作） |
| 404 | 资源不存在 / 不可见（门控统一） |
| 500 | 服务器错误（未识别异常，脱敏） |

### 0.4 可见性门控（重要）

所有读与写操作都遵循双层防护：
1. **权限校验**：`HasPermission(userId)` —— 公开文档所有人可见；私有/受保护文档仅作者。
2. **审核门控**：未通过审核（非 `MarkApproved`）的文档仅作者可见；其余请求一律返回 404（不泄露文档存在性）。

---

## 1. 文章管理 `/api/markdown`

### 1.1 创建文章

`POST /api/markdown/`（需认证）

请求体 (`CreateMarkdownRequest`)：

```json
{
  "name": "我的第一篇博客",        // 必填, ≤200 字符
  "content": "# 标题\n正文...",   // 必填, ≤1,000,000 字符
  "tags": ["dotnet", "blog"],    // 可选, ≤20 个, 单个 ≤50 字符
  "auth": "public",              // 可选: public|private|protected|admin|root, 默认 public
  "coverUrl": "https://.../cover.png"  // 可选, ≤2048 字符
}
```

响应：`201 Created`，`data` = 新文章 `MarkdownResponse`（含 `MarkDownGuid`）。创建成功向 Message 服务发布文章创建事件（好友/关注者聚合提醒）。

### 1.2 获取公开文章列表（分页）

`GET /api/markdown/?skip=0&take=20`

- 无需认证；`skip` ≥ 0（负值钳 0），`take` 钳制 1~100。
- 仅返回非删除、`MarkApproved`、公开的文档；`data` = `List<MarkdownSummaryResponse>`。

### 1.3 获取文章详情

`GET /api/markdown/{markDownGuid}`

- 无需认证；返回元数据 + 交互统计（不含正文，正文见 1.4）。
- 门控：私有/未过审非作者 → 404。响应 `data` = `MarkdownResponse`。

### 1.4 获取文章正文

`GET /api/markdown/{markDownGuid}/content`

- 无需认证；从文件存储（FileDev/Local）读取正文，`data` = 字符串（Markdown 原文）。
- 门控与详情一致；正文文件缺失 → 404。

### 1.5 更新文章

`PUT /api/markdown/{markDownGuid}`（需认证，仅作者）

请求体：同 [1.1]（`UpdateMarkdownRequest`；`tags` 传 `null` 表示不修改，`coverUrl` 空串表示清除封面）。

- 自动对旧正文生成历史版本快照（哈希去重）；旧正文文件保存成功后被清理。
- 越权 → 403；不存在 → 404。响应 `data` = 更新后 `MarkdownResponse`。

### 1.6 删除文章

`DELETE /api/markdown/{markDownGuid}`（需认证，仅作者）

- 软删除（`IsDelete=true`）；支持 `Idempotency-Key`。成功 `data` = null。

### 1.7 文章列表（分页/标签/按用户）

`GET /api/markdown/list?skip=0&take=20&tag=dotnet&userGuid=<guid>`

- 无需认证；仅返回已审核通过且对查看者可见的文章。
- `userGuid` 过滤作者；`tag` 过滤标签（精确匹配）。
- 响应 `data` = `List<MarkdownSummaryResponse>`。

### 1.8 文章搜索

`GET /api/markdown/search?keyword=xx&skip=0&take=20`

- 无需认证；按名称模糊搜索（正文文件化，不参与搜索）。
- `keyword` 必填（空白 → 400）。响应 `data` = `List<MarkdownSummaryResponse>`。

### 1.9 热点榜

`GET /api/markdown/hot?take=20`

- 无需认证；`take` 钳制 1~100（默认 20）。
- 数据源：Redis ZSet 直读 → miss 单飞重建 → DB 计算降级。
- 热度公式：`0.5×log10(1+点赞+2×收藏+3×分享+5×硬币) + 0.3×log10(1+浏览) + 0.2×e^(-age天/7)`。
- 响应 `data` = `List<MarkdownHotResponse>`。

---

## 2. 文档交互 `/api/markdown/{markDownGuid}/...`

所有交互端点（除浏览）需认证；文档不可见（删除/私有/未过审）时一律 404。

| 方法 | 路径 | 说明 | 返回 data |
| --- | --- | --- | --- |
| POST | `/api/markdown/{markDownGuid}/view` | 浏览 +1（无需认证；已登录用户 24h Redis 防刷） | `long` 浏览数 |
| POST | `/api/markdown/{markDownGuid}/like` | 点赞 +1（一用户一次，唯一约束幂等；首次发布作者通知） | `long` 点赞数 |
| POST | `/api/markdown/{markDownGuid}/unlike` | 取消点赞 -1（未点赞幂等返回） | `long` 点赞数 |
| POST | `/api/markdown/{markDownGuid}/share` | 分享 +1 | `long` 分享数 |
| POST | `/api/markdown/{markDownGuid}/coin` | 投币（一用户一篇仅一次；首次发布作者通知） | `long` 硬币总数 |

投币请求体 (`CoinMarkdownRequest`)：

```json
{ "amount": 5 }   // 必填, 1~100（越界 → 400）
```

> 通知语义：点赞/投币仅在**首次**发生时向作者推送消息（`MarkdownInteractionIntegrationEvent`），重复交互幂等分支不重复通知。

---

## 3. 审核 `/api/markdown/{markDownGuid}/...`

全部需认证。

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/markdown/{markDownGuid}/submit` | 提交审核（仅作者；草稿/驳回 → 待审核；状态非法 → 400） |
| POST | `/api/markdown/{markDownGuid}/approve` | 审核通过（作者或管理员；待审核 → 通过） |
| POST | `/api/markdown/{markDownGuid}/reject` | 审核驳回（作者或管理员；待审核 → 驳回） |

---

## 4. 评论 `/api/markdown/{markDownGuid}/reviews/...`

创建/更新/删除/点赞/踩需认证；查询无需认证。门控规则同文章（评论所属文档不可读时一律 404）。

### 4.1 创建评论

`POST /api/markdown/{markDownGuid}/reviews/`（需认证）

请求体 (`CreateMarkReviewRequest`)：

```json
{
  "content": "写得很棒！",              // 必填, ≤2000 字符
  "reviewImages": ["https://.../a.png"], // 可选, ≤9 张（http/https 绝对地址）
  "auth": "public"                       // 可选: public|private|protected, 默认 public
}
```

响应：`201 Created`，`data` = 评论 Guid。顶级评论发布通知给博客作者（作者本人评论不通知）。

### 4.2 获取顶级评论列表

`GET /api/markdown/{markDownGuid}/reviews/`

响应 `data` = `List<MarkReviewResponse>`（按时间倒序；已删除/不可见评论过滤）。

### 4.3 获取评论详情

`GET /api/markdown/{markDownGuid}/reviews/detail/{reviewGuid}`

- 读取即浏览 +1。门控：评论及所属文档对当前用户可见；否则 404。

### 4.4 获取子评论

`GET /api/markdown/{markDownGuid}/reviews/{reviewGuid}/children`

响应 `data` = `List<MarkReviewResponse>`（按时间正序）。

### 4.5 更新评论

`PUT /api/markdown/{markDownGuid}/reviews/{reviewGuid}`（需认证，仅作者本人）

请求体 (`UpdateMarkReviewRequest`)：`{ "content": "修改后的内容" }`。

### 4.6 删除评论

`DELETE /api/markdown/{markDownGuid}/reviews/{reviewGuid}`（需认证，仅作者本人）

- 软删除**整棵评论子树**（含所有后代评论）；支持 `Idempotency-Key`。

### 4.7 添加子评论（回复）

`POST /api/markdown/{markDownGuid}/reviews/{reviewGuid}/children`（需认证）

请求体：同 [4.1] `CreateMarkReviewRequest`。

响应：`201 Created`，`data` = 子评论 Guid。回复通知父评论作者（本人回复自己不通知）。

### 4.8 评论点赞 / 取消点赞

| 方法 | 路径 | 说明 | 返回 data |
| --- | --- | --- | --- |
| POST | `.../reviews/{reviewGuid}/like` | 点赞 +1（一用户一次；首次通知评论作者） | `long` 点赞数 |
| POST | `.../reviews/{reviewGuid}/unlike` | 取消点赞 -1（未点赞幂等） | `long` 点赞数 |

### 4.9 评论踩 / 取消踩

| 方法 | 路径 | 说明 | 返回 data |
| --- | --- | --- | --- |
| POST | `.../reviews/{reviewGuid}/dislike` | 踩 +1（一用户一次；首次通知评论作者） | `long` 踩数 |
| POST | `.../reviews/{reviewGuid}/undislike` | 取消踩 -1（未踩幂等） | `long` 踩数 |

---

## 5. 历史版本 `/api/markdown/{markDownGuid}/history/...`

列表/详情无需认证；删除/还原需认证（仅作者）。

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/markdown/{markDownGuid}/history/` | 历史版本列表（按创建时间倒序），`data` = `List<OldMarkDownResponse>` |
| GET | `/api/markdown/{markDownGuid}/history/{oldMarkDownGuid}` | 单个历史版本详情，`data` = `OldMarkDownResponse` |
| DELETE | `/api/markdown/{markDownGuid}/history/{oldMarkDownGuid}` | 软删除历史版本（仅作者；越权 → 403） |
| POST | `/api/markdown/{markDownGuid}/history/{oldMarkDownGuid}/restore` | 还原历史版本（仅作者；还原前自动为当前内容建快照） |

---

## 6. 收藏 `/api/favorites/...`

全部需认证。

### 6.1 添加收藏

`POST /api/favorites/`（需认证）

请求体 (`AddFavoriteRequest`)：

```json
{ "markDownGuid": "<文章Guid>", "tags": ["笔记", "技术"] }
```

- 已收藏时自动**合并**新标签（幂等，不报错）；未收藏则创建并收藏计数 +1。
- 门控：文章不可见（私有/未过审/已删除）→ 404。
- 响应：`201 Created`，`data` = 收藏 Guid。

### 6.2 取消收藏

`DELETE /api/favorites/{markDownGuid}`（需认证）

- 幂等（未收藏也返回成功）；收藏计数 -1。

### 6.3 更新收藏标签（覆盖式）

`PUT /api/favorites/{markDownGuid}/tags`（需认证）

请求体 (`UpdateFavoriteTagsRequest`)：`{ "tags": ["新标签"] }`（空/null 表示清空）。

- 收藏不存在 → 404；标签数量/长度违规 → 400。

### 6.4 我的标签库

`GET /api/favorites/tags?keyword=xx&limit=50`

- 常用标签建议：按使用次数优先、最近使用次之；`limit` 钳制 1~100（默认 50）。
- 响应 `data` = `List<MarkFavoriteTagResponse>`。

### 6.5 我的收藏列表

`GET /api/favorites/?tag=笔记&skip=0&take=20`

- 按用户分页；`tag` 精确过滤（JSON 数组元素匹配）；`skip ≥ 0`，`take` 钳制 1~100。
- 已删除文章不展示。响应 `data` = `List<MarkFavoriteResponse>`。

---

## 7. 数据模型（响应 DTO）

### 7.1 MarkdownResponse（文章详情）

```json
{
  "markDownGuid": "guid",
  "markUserGuid": "guid",
  "name": "文章标题",
  "hash": "sha256hex",
  "fileId": "file-id",
  "fileSize": 12345,
  "fileExt": ".md",
  "coverUrl": "https://...",
  "tags": ["tag1"],
  "auth": "PublicMark",
  "createAt": "2026-08-29T00:00:00Z",
  "updateAt": "2026-08-29T00:00:00Z",
  "quote": { "loveCount": 1, "favoriteCount": 2, "shareCount": 0, "coinCount": 3, "viewCount": 100, "heatScore": 0.86, "totalInteractions": 6 }
}
```

### 7.2 MarkdownSummaryResponse（列表/搜索）

```json
{ "markDownGuid": "guid", "name": "标题", "tags": [], "coverUrl": null, "auth": "PublicMark", "status": "MarkApproved", "createAt": "...", "updateAt": "..." }
```

### 7.3 MarkReviewResponse（评论）

```json
{ "markReviewGuid": "guid", "markDownGuid": "guid", "userId": "commenter-guid", "content": "评论内容", "auth": "ReviewAuthPublic", "reviewImages": null, "reviewTime": "...", "isDeleted": false, "childReviewCount": 2, "quote": { "loveCount": 1, "viewCount": 10, "replyCount": 2, "dislikeCount": 0, "totalInteractions": 3 } }
```

### 7.4 OldMarkDownResponse（历史版本）

```json
{ "oldMarkDownGuid": "guid", "markDownGuid": "guid", "userGuid": "guid", "auth": "PublicMark", "content": "# 旧内容", "hash": "sha256hex", "createAt": "...", "updateAt": "..." }
```

### 7.5 MarkFavoriteResponse（收藏）

```json
{ "markFavoriteGuid": "guid", "markDownGuid": "guid", "markDownName": "文章名", "tags": ["标签"], "createAt": "..." }
```

### 7.6 MarkFavoriteTagResponse（标签库项）

```json
{ "tag": "技术", "useCount": 3, "lastUsedAt": "..." }
```

### 7.7 MarkdownHotResponse（热点榜项）

```json
{ "markDownGuid": "guid", "name": "标题", "heatScore": 0.86, "createAt": "..." }
```

---

## 8. 身份与权限速查

| 资源 | 读取 | 写操作 |
| --- | --- | --- |
| 文章 | 公开已过审任何人；私有/未过审仅作者；不可见一律 404 | 创建需认证；更新/删除/提交审核仅作者；通过/驳回作者或管理员 |
| 评论 | 公开评论任何人；私有/受保护评论仅所有者 | 创建需认证且文章可见；更新/删除仅评论作者本人；点赞/踩任意认证用户 |
| 收藏 | 仅本人 | 需认证（本人） |
| 历史版本 | 门控同文章详情 | 删除/还原仅作者 |

---

## 9. 跨服务事件（供消费端参考）

| 事件（RabbitMQ Exchange: `notcomd_event_bus` / Queue: `markdown_queue`） | 触发 | 载荷要点 |
| --- | --- | --- |
| `MarkdownCreated` | 创建文章 | MarkDownGuid / MarkUserGuid / FileName / CreatedAt |
| `MarkdownInteraction`（DocumentLiked / DocumentCoined / ReviewLiked / ReviewDisliked） | 首次交互 | InteractionType / MarkDownGuid / MarkDownName / ReviewGuid? / ActorUserId / TargetUserId / Amount / OccurredAt |
| `MarkdownCommentPublished` | 发布评论 | MarkDownGuid / MarkDownName / ReviewGuid / ParentReviewGuid? / CommentContent / ActorUserId / TargetUserId / OccurredAt |
| `MarkReviewCreated` / `MarkReviewDeleted` / `MarkReviewLiked` / `ChildReviewAdded` | 评论生命周期 | 评论与文章标识、操作者、时间（兼容保留） |