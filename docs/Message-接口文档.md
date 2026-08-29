# Message 消息服务 · API 文档（接口文档）

> 适用范围：`Message.Web.API` 对外暴露的全部接口。
> 服务提供两套访问通道：**HTTP REST**（`/api/*`，15 组）与 **SignalR 实时通道**（3 个 Hub）。
> 涉及项目：`Message.Domain` / `Message.Infrastructure` / `Message.Web.API`。

---

## 1. 通用约定

### 1.1 通道与地址

| 通道 | 协议 | 路径 | 用途 |
| --- | --- | --- | --- |
| HTTP REST | HTTP/1.1 JSON | `/api/*`（15 组） | 社交域 CRUD / 聊天管理 / 文件上传 |
| SignalR | WebSocket / HTTP | `/MessageHub` `/CommunityHub` `/CallHub` | 实时消息、社区频道、音视频通话 |
| OpenAPI / Scalar | HTTP（仅 Development） | `/openapi/v1.json` `/scalar/v1` | 联调文档 |

- 端口见 `Message.Web.API/Properties/launchSettings.json`；Aspire 编排环境下由 AppHost 注入实际地址。
- Docker 镜像运行时暴露 **8084** 端口。

### 1.2 认证

- **REST**：请求头 `Authorization: Bearer <JWT>`。除显式声明外，所有 `/api/*` 端点均要求 JWT，未认证返回 `401`。
- **SignalR**：WebSocket 无法携带自定义头，JWT 从查询参数 `access_token` 读取（客户端 `accessTokenFactory` 默认如此）。
- 用户身份以服务端从 JWT Claim（`sub` / `NameIdentifier` / `user_guid`）解析为准，**客户端传入的 id 一律忽略**。
- 管理员接口（Audit 组）额外校验管理员角色，非管理员返回 `403`。

### 1.3 统一响应格式

除下载/预览（二进制流）与 TURN 凭证外，所有 REST 接口统一返回 `ApiResponse<T>`：

```jsonc
// 成功
{ "success": true, "message": "可选提示", "data": { }, "code": 200, "timestamp": "2026-08-29T10:00:00Z" }
// 失败
{ "success": false, "message": "错误描述", "data": null, "code": 400, "timestamp": "..." }
```

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `success` | bool | 是否成功 |
| `message` | string? | 提示/错误信息 |
| `data` | T? | 业务数据（`Code=204` 时为空） |
| `code` | int | 200 / 201 / 204 / 400 / 401 / 403 / 404 / 500 |
| `timestamp` | DateTime | UTC 时间戳 |

状态码：`200` 成功 / `201` 创建 / `204` 无内容 / `400` 参数错误 / `401` 未认证 / `403` 无权 / `404` 不存在 / `413` 载荷过大 / `415` 不支持媒体类型 / `500` 服务端错误。

### 1.4 分页约定

分页查询统一使用查询参数 `page`（从 1 开始，默认 1）与 `pageSize`（默认 20/50，视端点而定），返回：

```jsonc
{ "success": true, "data": { "items": [ ], "totalCount": 100, "page": 1, "pageSize": 20 } }
```

---

## 2. HTTP REST 接口

### 2.1 消息 `/api/messages`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /` | `SendMessageRequest`（body） | `Guid`（消息ID） | 发送文本/图片/视频/音频/文件/位置/链接/表情消息；返回新消息 ID |
| `GET /{messageId}/attachments` | — | `IEnumerable<FileAttachmentDto>` | 消息附件列表（仅会话参与者可见） |
| `GET /attachments/{attachmentId}` | — | `FileAttachmentDto` | 附件详情（大小/类型/下载次数/MIME） |
| `POST /{messageId}/attachments` | `AddAttachmentRequest{ fileId }` | `Guid`（附件ID） | 给已发送消息补附件 |
| `DELETE /attachments/{attachmentId}` | — | 无 | 删除附件（软删 + 级联 FileDev 物理删除） |
| `GET /attachments/{attachmentId}/download` | — | 文件流 | 流式下载附件（gRPC 代理，记录下载次数） |
| `GET /attachments/{attachmentId}/preview` | `?w=&h=` | 图片流 | 图片预览（仅图片类型，否则 415；支持服务端缩放） |
| `GET /{id}` | — | `MessageDto` | 消息详情（Redis 缓存，TTL 30min） |
| `GET /sessions/{sessionId}/messages` | `?page=&pageSize=` | `PagedResult<MessageDto>` | 会话消息历史分页 |
| `DELETE /{id}` | `?reason=` | 无 | 撤回消息（`RecallReason` 枚举，默认 `UserRequest`） |
| `POST /{id}/forward` | `ForwardMessageRequest`（body） | `Guid`（新消息ID） | 转发消息到目标会话 |
| `PUT /{id}/read` | — | 无 | 标记已读 |
| `GET /search` | `?sessionId=&searchTerm=&page=&pageSize=` | `PagedResult<MessageDto>` | 会话内搜索消息 |
| `GET /unread` | — | `IEnumerable<MessageDto>` | 当前用户未读消息列表 |

`SendMessageRequest` 关键字段：`sessionId`、`messageType`、`content`、`fileId`（媒体必填）、`thumbnailFileId`、`duration`、`caption`、`latitude/longitude/locationName`、`linkUrl/linkTitle/linkDescription`、`expressionCode`、`replyToMessageId`。

`MessageDto` 主要字段：`messageId`、`sessionId`、`senderId`、`receiverId`、`messageType`、`status`、`content`（正文/媒体地址/链接等）、`sentTime`、`isRecalled`、`isForwarded`、`replyToMessageId`、`attachments`。

### 2.2 会话 `/api/sessions`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /` | `CreateSessionRequest{ sessionType, friendId?, groupId?, initialMembers? }` | `Guid`（会话ID） | 创建私聊/群聊会话 |
| `GET /` | — | `IEnumerable<SessionDto>` | 当前用户所有会话（群聊名/社区名经 Group/Circle 投影） |
| `GET /{id}` | — | `SessionDto` | 会话详情（缓存命中仍校验参与者权限） |
| `PUT /{id}/pin` | `?pin=true\|false` | 无 | 置顶/取消置顶 |
| `PUT /{id}/mute` | `?mute=true\|false` | 无 | 静音/取消静音 |
| `DELETE /{id}` | — | 无 | 解散会话 |
| `GET /{id}/participants` | — | `IEnumerable<Guid>` | 参与者用户 ID 列表 |
| `POST /{id}/participants` | `AddParticipantRequest{ userId }` | 无 | 添加参与者 |
| `DELETE /{id}/participants/{userId}` | — | 无 | 移除参与者 |
| `GET /pinned` | — | `IEnumerable<SessionDto>` | 置顶会话列表 |
| `GET /unread-count` | — | `int` | 全部会话未读总数 |

`SessionDto` 字段：`sessionId`、`sessionType`（Private/Group/Channel）、`sessionName`、`groupId`、`circleId`、`creatorId`、`participants[]`、`lastMessageId/content/time`、`unreadCount`、`createdTime`、`isPinned`、`isMuted`。

### 2.3 文件上传 `/api/files`

> 本组仅负责「上传」，统一经 FileDev.Web.API 的 gRPC 文件服务；响应 `FileRef`（`fileId`、`fileName`、`fileUrl`、`fileSize`、`mimeType`、`thumbnailUrl`）。

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /upload` | multipart：`file`、`description?`、`isPublic`、`contentId?`、`contentType?` | `FileRef` | 小文件上传（≤10MB，超限 413） |
| `POST /upload-image` | multipart：`file`、`description?`、`isPublic?` | `FileRef` | 图片上传（格式校验 + 尺寸解析） |
| `POST /chunk/init` | `ChunkUploadInitRequest`（body） | `ChunkUploadInitResult` | 初始化分片，返回 `fileKey` 与已上传分片 |
| `POST /chunk/upload` | multipart：`fileKey` + `chunkIndex` + 分片文件 | `ChunkUploadResult` | 上传单个分片 |
| `POST /chunk/status` | `ChunkStatusRequest{ fileKey }` | `ChunkStatusResult` | 查询已上传分片索引（断点续传决策） |
| `POST /chunk/merge` | `ChunkMergeRequest{ fileKey, fileName? }` | `MergeChunksResult` | 合并分片生成最终文件 |
| `POST /chunk/cancel` | `ChunkCancelRequest{ fileKey }` | `CancelChunkUploadResult` | 取消并清理临时数据 |
| `POST /chunk/resume` | `ChunkResumeRequest{ fileKey, totalChunks, chunkSize, chunks[] }` | `ChunkStatusResult` | 一次性提交缺失分片完成续传 |

### 2.4 群组 `/api/groups`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /` | `CreateGroupRequest` | `Guid` | 创建群组（可带初始成员） |
| `GET /` | — | `IEnumerable<GroupDto>` | 我的群组列表 |
| `GET /{id}` | — | `GroupDto` | 群组详情 |
| `PUT /{id}/info` | `UpdateGroupInfoRequest` | 无 | 更新群组信息 |
| `DELETE /{id}` | — | 无 | 解散群组 |
| `GET /{id}/members` | — | `IEnumerable<GroupMemberDto>` | 成员列表 |
| `POST /{id}/members` | `AddGroupMemberRequest` | 无 | 添加成员 |
| `DELETE /{id}/members/{userId}` | — | 无 | 移除成员 |
| `PUT /{id}/admins` | `SetAdminRequest` | 无 | 设置/取消管理员 |
| `PUT /{id}/transfer` | `TransferOwnershipRequest` | 无 | 转让群主 |
| `PUT /{id}/members/{userId}/mute` | `MuteMemberRequest` | 无 | 禁言成员 |
| `DELETE /{id}/members/{userId}/mute` | — | 无 | 解除禁言 |
| `PUT /{id}/members/{userId}/ban` | — | 无 | 封禁成员 |
| `DELETE /{id}/members/{userId}/ban` | — | 无 | 解除封禁 |
| `GET /public` | — | `IEnumerable<GroupDto>` | 公开群组列表 |
| `GET /search` | `?keyword=&page=&pageSize=` | `PagedResult<GroupDto>` | 搜索群组 |
| `GET /{id}/member-count` | — | `int` | 成员数 |
| `GET /{id}/is-member/{userId}` | — | `bool` | 是否群成员 |

### 2.5 好友 `/api/friends`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /request` | `SendFriendRequestRequest{ friendId }` | `Guid` | 发送好友请求 |
| `PUT /request/{friendId}` | `HandleFriendRequestRequest{ accept }` | 无 | 接受/拒绝请求 |
| `GET /` | — | `IEnumerable<FriendDto>` | 好友列表 |
| `GET /requests` | — | `IEnumerable<FriendRequestDto>` | 收到的好友请求 |
| `GET /sent-requests` | — | `IEnumerable<FriendRequestDto>` | 发出的好友请求 |
| `DELETE /{friendId}` | — | 无 | 删除好友 |
| `PUT /{friendId}/block` | `?block=` | 无 | 屏蔽/取消屏蔽 |
| `PUT /{friendId}/remark` | `UpdateFriendRemarkRequest{ remark }` | 无 | 更新备注 |
| `PUT /{friendId}/star` | `?star=` | 无 | 星标/取消星标 |
| `PUT /{friendId}/mute` | `?mute=` | 无 | 静音/取消静音 |
| `GET /blocked` | — | `IEnumerable<FriendDto>` | 已屏蔽列表 |
| `GET /starred` | — | `IEnumerable<FriendDto>` | 星标列表 |
| `GET /count` | — | `int` | 好友总数 |
| `GET /search` | `?keyword=` | `IEnumerable<FriendDto>` | 搜索好友 |

### 2.6 圈子 `/api/circles`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /join` | `JoinCircleRequest{ invitationCode/Url? }` | `Guid` | 凭邀请码/链接加入圈子 |
| `GET /invitations/my` | `?page=&pageSize=` | `PagedResult<CircleInvitationDto>` | 我收到的直邀列表 |
| `POST /invitations/{inviteGuid}/accept` | — | `Guid` | 接受直邀 |
| `POST /invitations/{inviteGuid}/reject` | — | 无 | 拒绝直邀 |
| `GET /` | `?keyword=&page=&pageSize=` | `PagedResult<CircleDto>` | 圈子发现（活跃 + 名称搜索） |
| `GET /my` | — | `List<CircleDto>` | 我加入的圈子 |
| `POST /` | `CreateCircleRequest` | `Guid` | 创建圈子 |
| `GET /{circleGuid}` | — | `CircleDto` | 圈子详情 |
| `PUT /{circleGuid}` | `UpdateCircleRequest` | 无 | 更新圈子信息 |
| `DELETE /{circleGuid}` | — | 无 | 解散圈子 |
| `POST /{circleGuid}/invitations` | `GenerateInvitationRequest` | `CircleInvitationResult` | 生成邀请（码/链接/直邀） |
| `GET /{circleGuid}/invitations` | `?page=&pageSize=` | `PagedResult<CircleInvitationDto>` | 圈子邀请列表（圈主/管理员） |
| `DELETE /{circleGuid}/invitations/{inviteGuid}` | — | 无 | 撤销邀请 |
| `GET /{circleGuid}/members` | `?page=&pageSize=` | `PagedResult<CircleMemberDto>` | 成员列表 |
| `POST /{circleGuid}/members/{userGuid}/role` | `SetCircleMemberRoleRequest` | 无 | 设置/取消管理员 |
| `DELETE /{circleGuid}/members/{userGuid}` | — | 无 | 移出成员 |
| `POST /{circleGuid}/transfer` | `CircleTransferOwnershipRequest` | 无 | 转移圈主 |
| `GET /{circleGuid}/posts` | `?page=&pageSize=` | `PagedResult<CommunityPostDto>` | 圈子帖子流 |
| `GET /{circleGuid}/session` | — | `SessionDto` | 获取社区聊天会话（Channel，仅成员可见） |

### 2.7 话题 `/api/topics`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /` | `CreateTopicRequest{ name, description? }` | `Guid` | 创建话题 |
| `GET /` | `?page=&pageSize=` | `PagedResult<TopicDto>` | 话题列表（按帖子数降序） |
| `GET /{topicGuid}/posts` | `?page=&pageSize=` | `PagedResult<CommunityPostDto>` | 话题帖子流 |
| `PUT /{topicGuid}` | `UpdateTopicRequest` | 无 | 更新话题（创建者/管理员） |
| `DELETE /{topicGuid}` | — | 无 | 停用话题（创建者/管理员） |

### 2.8 关注 `/api/follows`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /{userGuid}` | — | 无 | 关注用户 |
| `DELETE /{userGuid}` | — | 无 | 取消关注 |
| `GET /following` | `?page=&pageSize=` | `PagedResult<UserFollowDto>` | 我关注的人 |
| `GET /followers` | `?page=&pageSize=` | `PagedResult<UserFollowDto>` | 我的粉丝 |
| `GET /feed` | `?page=&pageSize=` | `PagedResult<CommunityPostDto>` | 关注 Feed（我 + 关注者的全局帖） |

### 2.9 推文 `/api/tweets`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /` | `CreateTweetRequest{ content, fileIds[], linkUrl?, visibility }` | `Guid` | 创建推文（进审核流） |
| `POST /circle` | `CreateCirclePostRequest{ circleGuid, content, fileIds[], linkUrl?, topicGuids[] }` | `Guid` | 圈子发帖（免审核，仅成员可见） |
| `POST /draft` | `CreateTweetRequest` | `Guid` | 保存草稿 |
| `GET /drafts` | `?page=&pageSize=` | `PagedResult<TweetDto>` | 我的草稿列表 |
| `GET /{tweetGuid}` | — | `TweetDto` | 推文详情（含当前用户交互状态） |
| `GET /user/{userGuid}` | `?page=&pageSize=` | `PagedResult<TweetDto>` | 用户推文列表 |
| `GET /timeline` | `?page=&pageSize=` | `PagedResult<TweetDto>` | 时间线 |
| `GET /trending` | `?page=&pageSize=` | `PagedResult<TweetDto>` | 趋势推文 |
| `PUT /{tweetGuid}` | `UpdateTweetRequest` | 无 | 更新草稿 |
| `DELETE /{tweetGuid}` | — | 无 | 删除推文 |
| `POST /{tweetGuid}/pin` | — | 无 | 置顶 |
| `POST /{tweetGuid}/unpin` | — | 无 | 取消置顶 |
| `POST /{tweetGuid}/like` | — | 无 | 点赞 |
| `DELETE /{tweetGuid}/like` | — | 无 | 取消点赞 |
| `POST /{tweetGuid}/favorite` | — | 无 | 收藏 |
| `DELETE /{tweetGuid}/favorite` | — | 无 | 取消收藏 |
| `POST /{tweetGuid}/share` | — | 无 | 分享 |
| `POST /{tweetGuid}/coin` | — | 无 | 投币（消耗硬币） |
| `POST /{tweetGuid}/view` | — | 无 | 记录查看 |

### 2.10 评论 `/api/comments`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /` | `CreateCommentRequest{ tweetGuid, content, parentGuid?, replyToGuid? }` | 无 | 发布评论/回复 |
| `GET /tweet/{tweetGuid}` | `?page=&pageSize=` | `PagedResult<CommentDto>` | 推文评论列表 |
| `GET /{commentGuid}/replies` | `?page=&pageSize=` | `PagedResult<CommentDto>` | 评论回复列表 |
| `DELETE /{commentGuid}` | — | 无 | 删除评论 |

### 2.11 举报 `/api/reports`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `POST /` | `SubmitReportRequest{ targetType, targetGuid, reason, category, evidenceUrls[] }` | 无 | 提交举报（推文/评论） |
| `GET /my` | `?page=&pageSize=` | `PagedResult<object>` | 我的举报列表 |

### 2.12 通知 `/api/notifications`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `GET /` | `?page=&pageSize=&unreadOnly=` | `PagedResult<NotificationDto>` | 我的通知列表 |
| `GET /unread-count` | — | `int` | 未读通知数 |
| `PUT /read-all` | — | 无 | 全部标记已读 |
| `PUT /{notifyGuid}/read` | — | 无 | 单条标记已读 |

### 2.13 审核 `/api/audit`（仅管理员）

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `GET /tweets/pending` | `?page=&pageSize=` | `PagedResult<object>` | 待审核推文列表 |
| `POST /tweets/{tweetGuid}/approve` | — | 无 | 通过审核 |
| `POST /tweets/{tweetGuid}/reject` | `AuditActionRequest{ reason }` | 无 | 驳回推文 |
| `GET /reports/pending` | `?page=&pageSize=` | `PagedResult<object>` | 待处理举报列表 |
| `POST /reports/{reportGuid}/resolve` | `ResolveReportRequest` | 无 | 处理举报 |

### 2.14 用户资料 `/api/user-info`

| 方法 & 路径 | 请求 | 响应 Data | 说明 |
| --- | --- | --- | --- |
| `GET /me` | — | `UserInfoDto` | 我的资料（等级/硬币/背景封面；默认等级 1 / 硬币 0） |
| `PUT /me/background` | `UpdateBackgroundCoverRequest{ backgroundCoverUrl }` | 无 | 更新背景封面（空白清除） |
| `POST /sign-in` | — | `SignInResultDto` | 每日签到（+250 经验，每日一次，重复 400） |
| `POST /me/coins/add` | `CoinAmountRequest{ amount }` | 无 | 增加硬币 |
| `POST /me/coins/consume` | `CoinAmountRequest{ amount }` | 无 | 扣除硬币（余额不足 400） |

### 2.15 TURN 凭证 `/api/turn`

| 方法 & 路径 | 请求 | 响应 | 说明 |
| --- | --- | --- | --- |
| `GET /credentials` | — | **原始** `TurnCredentialsDto`（**非** `ApiResponse<T>` 包裹） | 按 coturn use-auth-secret 规范签发限时凭证，供 RTCPeerConnection 的 `getIceServers` 直接消费 |

`TurnCredentialsDto` 结构：`{ "urls": ["turn:..."], "username": "...", "credential": "..." }`。

---

## 3. SignalR 实时接口

三个 Hub 均要求 JWT（`access_token` 查询参数）。客户端事件方法名由 C# 接口定义（`IMessageClient` / `ICommunityClient` / `ICallClient`）。

### 3.1 MessageHub（`/MessageHub`）

| 方向 | 方法 | 参数 | 说明 |
| --- | --- | --- | --- |
| 客户端 → 服务端 | `SendMessage` | `(Guid sessionId, SendMessageRequest request)` | 发送消息（会话参与者校验） |
| 客户端 → 服务端 | `SendFileMessage` | `(Guid sessionId, Guid fileId)` | 上传完成后一键发送文件消息 |
| 客户端 → 服务端 | `MarkAsRead` | `(Guid messageId)` | 标记已读并通知其他参与者 |
| 客户端 → 服务端 | `RecallMessage` | `(Guid messageId)` | 撤回消息并通知会话 |
| 客户端 → 服务端 | `SendTypingIndicator` | `(Guid sessionId)` | 发送"正在输入" |
| 客户端 → 服务端 | `JoinSession` | `(Guid sessionId)` | 加入会话群组（`session:{id}`） |
| 客户端 → 服务端 | `LeaveSession` | `(Guid sessionId)` | 移出会话群组 |
| 客户端 → 服务端 | `InitChunkUpload` | `(ChunkUploadInitRequest)` → `ChunkUploadInitResult` | 初始化分片上传 |
| 客户端 → 服务端 | `UploadChunk` | `(ChunkUploadRequest)` → `ChunkUploadResult` | 上传分片 |
| 客户端 → 服务端 | `GetChunkStatus` | `(ChunkStatusRequest)` → `ChunkStatusResult` | 查询分片状态 |
| 客户端 → 服务端 | `MergeChunks` | `(ChunkMergeRequest)` → `MergeChunksResult` | 合并分片 |
| 客户端 → 服务端 | `CancelChunkUpload` | `(ChunkCancelRequest)` → `CancelChunkUploadResult` | 取消上传 |
| 客户端 → 服务端 | `ResumeChunkUpload` | `(ChunkResumeRequest)` → `ChunkStatusResult` | 断点续传（带进度） |

服务端 → 客户端事件（`IMessageClient`）：

| 事件 | 参数 | 说明 |
| --- | --- | --- |
| `ReceiveMessage` | `(MessageDto message)` | 收到新消息 |
| `MessageRecalled` | `(Guid messageId)` | 消息被撤回 |
| `MessageRead` | `(Guid messageId, Guid readerId)` | 消息被读取 |
| `UserOnline` / `UserOffline` | `(Guid userId)` | 好友上线/下线 |
| `TypingIndicator` | `(Guid sessionId, Guid userId)` | 对方正在输入 |
| `UnreadCountUpdated` | `(Guid sessionId, int count)` | 未读数更新 |
| `UploadProgress` | `(ChunkUploadProgress progress)` | 分片上传进度 |

> 连接生命周期：`OnConnectedAsync` 登记连接、置在线、补推离线消息；`OnDisconnectedAsync` 移除连接、无其他连接时置离线并通知好友。

### 3.2 CommunityHub（`/CommunityHub`）

| 方向 | 方法 | 参数 | 说明 |
| --- | --- | --- | --- |
| 客户端 → 服务端 | `JoinCircle` | `(Guid circleGuid)` | 订阅圈子频道（`circle:{id}`，仅成员） |
| 客户端 → 服务端 | `LeaveCircle` | `(Guid circleGuid)` | 取消订阅 |

服务端 → 客户端事件（`ICommunityClient`）：

| 事件 | 参数 | 说明 |
| --- | --- | --- |
| `PostPublished` | `(CommunityPostDto post)` | 圈子新帖发布 |
| `CommentAdded` | `(Guid tweetGuid, CommentDto comment)` | 圈子帖收到新评论 |
| `PostLiked` | `(Guid tweetGuid, Guid userGuid, int likeCount)` | 圈子帖被点赞 |
| `PostFavorited` | `(Guid tweetGuid, Guid userGuid, int favoriteCount)` | 圈子帖被收藏 |
| `MemberJoined` | `(Guid circleGuid, Guid userGuid, string role)` | 新成员加入 |
| `MemberLeft` | `(Guid circleGuid, Guid userGuid)` | 成员退出 |
| `MemberRemoved` | `(Guid circleGuid, Guid userGuid)` | 成员被移出 |
| `InvitedToCircle` | `(Guid circleGuid, Guid inviteGuid)` | 收到圈子邀请（推送给个人） |

### 3.3 CallHub（`/CallHub`，WebRTC 通话）

| 方向 | 方法 | 参数 → 返回 | 说明 |
| --- | --- | --- | --- |
| 客户端 → 服务端 | `StartCall` | `(Guid sessionId, CallType type)` → `CallStartResult` | 基于会话发起语音/视频呼叫 |
| 客户端 → 服务端 | `CancelCall` | `(Guid callId)` | 呼叫方取消（响铃阶段） |
| 客户端 → 服务端 | `AcceptCall` | `(Guid callId)` → `CallInfoDto?` | 接听来电（1 对 1 或群组首名接通者） |
| 客户端 → 服务端 | `RejectCall` | `(Guid callId)` | 拒绝来电 |
| 客户端 → 服务端 | `JoinCall` | `(Guid callId)` → `CallInfoDto?` | 加入通话（群组后续成员/断线重连） |
| 客户端 → 服务端 | `HangUp` | `(Guid callId)` | 挂断/离开通话 |
| 客户端 → 服务端 | `GetCall` | `(Guid callId)` → `CallInfoDto?` | 查询通话状态（非成员 401） |
| 客户端 → 服务端 | `SendSignal` | `(CallSignalDto signal)` | 发送 WebRTC 信令（offer/answer/ice；`FromUserId` 服务端填充） |

服务端 → 客户端事件（`ICallClient`）：

| 事件 | 参数 | 说明 |
| --- | --- | --- |
| `IncomingCall` | `(CallInfoDto call)` | 被叫方收到来电（响铃） |
| `CallStarted` | `(CallInfoDto call)` | 通话建立（含 JoinedMembers 快照） |
| `CallEnded` | `(CallInfoDto call, CallEndReason reason)` | 通话结束（取消/拒绝/超时/全员离开/挂断/错误） |
| `MemberJoined` | `(CallInfoDto call, Guid memberId)` | 成员接通（据此建立对等连接） |
| `MemberLeft` | `(CallInfoDto call, Guid memberId)` | 成员离开（群组通话） |
| `MemberRejected` | `(CallInfoDto call, Guid memberId)` | 成员拒绝（通知呼叫方） |
| `Signal` | `(CallSignalDto signal)` | WebRTC 信令转发 |

---

## 4. 枚举参考

| 枚举 | 取值 |
| --- | --- |
| `SessionType` | Private / Group / Channel |
| `MessageType` | MessageText / MessageImage / MessageVideo / MessageAudio / MessageFile / MessageLocation / MessageLink / MessageExpression |
| `MessageStatus` | Pending / Sent / Delivered / Read / Failed |
| `RecallReason` | UserRequest / AdminAction / Violation / Expired |
| `CircleMemberRole` | Owner / Admin / Member |
| `CircleStatus` | Active / Dissolved |
| `GroupMemberRole` | Owner / Admin / Member |
| `GroupPermission` | （群成员权限位） |
| `ForwardType` | 直接转发 / 引用转发等 |
| `CallType` | Voice / Video |
| `CallStatus` | Ringing / Active / Ended 等 |
| `CallEndReason` | CallerCancelled / CalleeRejected / Timeout / AllLeft / HangUp / Error |
| `ReportTargetType` | Tweet / Comment |
| `ReportStatus` | Pending / Resolved / Dismissed |
| `ReportCategory` | Spam / Abuse / Illegal / Other |
| `TweetStatus` | Draft / Pending / Approved / Rejected |
| `InteractionType` | Like / Favorite / Coin / Share / View |
| `Visibility` | Public / Friends / Private |

---

*文档编制自代码梳理，接口定义以 `Message.Web.API/APIs/*.cs` 与 `Message.Web.API/Hubs/*.cs` 为准。*
