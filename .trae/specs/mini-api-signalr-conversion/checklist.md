# Mini API 转换与 SignalR 集成 — 验证检查清单

## 目录结构
- [ ] `Message.Web.API\APIs\` 目录已创建
- [ ] `Message.Web.API\APIs\AuditApi.cs` 存在
- [ ] `Message.Web.API\APIs\CommentsApi.cs` 存在
- [ ] `Message.Web.API\APIs\FilesApi.cs` 存在
- [ ] `Message.Web.API\APIs\FriendsApi.cs` 存在
- [ ] `Message.Web.API\APIs\GroupsApi.cs` 存在
- [ ] `Message.Web.API\APIs\MessagesApi.cs` 存在
- [ ] `Message.Web.API\APIs\ReportsApi.cs` 存在
- [ ] `Message.Web.API\APIs\SessionsApi.cs` 存在
- [ ] `Message.Web.API\APIs\TweetsApi.cs` 存在

## Program.cs 更新
- [ ] `builder.Services.AddControllers()` 已移除
- [ ] `app.MapControllers()` 已移除
- [ ] 9 个 `app.MapXxxApi()` 调用已添加
- [ ] 现有中间件管线保留（ExceptionHandling、UserContext、SignalR、Scalar）

## Controller 清理
- [ ] `Controllers\AuditController.cs` 已删除
- [ ] `Controllers\CommentsController.cs` 已删除
- [ ] `Controllers\FilesController.cs` 已删除
- [ ] `Controllers\FriendsController.cs` 已删除
- [ ] `Controllers\GroupsController.cs` 已删除
- [ ] `Controllers\MessagesController.cs` 已删除
- [ ] `Controllers\ReportsController.cs` 已删除
- [ ] `Controllers\SessionsController.cs` 已删除
- [ ] `Controllers\TweetsController.cs` 已删除

## AuditApi.cs（5 端点）功能验证
- [ ] GET `/api/audit/tweets/pending` — 返回 `ApiResponse<PagedResult<object>>`，仅管理员可访问
- [ ] POST `/api/audit/tweets/{tweetGuid}/approve` — 返回 `ApiResponse`，仅管理员可访问
- [ ] POST `/api/audit/tweets/{tweetGuid}/reject` — 接受 `AuditActionRequest` body，返回 `ApiResponse`
- [ ] GET `/api/audit/reports/pending` — 返回 `ApiResponse<PagedResult<object>>`，仅管理员可访问
- [ ] POST `/api/audit/reports/{reportGuid}/resolve` — 接受 `ResolveReportRequest` body，返回 `ApiResponse`

## CommentsApi.cs（4 端点）功能验证
- [ ] POST `/api/comments` — 接受 `CreateCommentRequest` body，返回 `ApiResponse`
- [ ] GET `/api/comments/tweet/{tweetGuid}` — 支持 `page`/`pageSize` 查询参数，返回 `ApiResponse<PagedResult<CommentDto>>`
- [ ] GET `/api/comments/{commentGuid}/replies` — 支持 `page`/`pageSize`，返回 `ApiResponse<PagedResult<CommentDto>>`
- [ ] DELETE `/api/comments/{commentGuid}` — 返回 `ApiResponse`

## FilesApi.cs（10 端点）功能验证
- [ ] POST `/api/files` — 接受 `UploadFileRequest` body，返回 `ApiResponse<FileAttachmentDto>`
- [ ] GET `/api/files/{id}` — 返回 `ApiResponse<FileAttachmentDto>`
- [ ] GET `/api/files/{id}/download` — 返回重定向
- [ ] GET `/api/files/{id}/preview` — 返回 `ApiResponse<FileAttachmentDto>`
- [ ] DELETE `/api/files/{id}` — 返回 `ApiResponse`
- [ ] GET `/api/files/message/{messageId}` — 返回 `ApiResponse<IEnumerable<FileAttachmentDto>>`
- [ ] GET `/api/files/{id}/exists` — 返回 `ApiResponse<bool>`
- [ ] GET `/api/files/{id}/download-count` — 返回 `ApiResponse<int>`
- [ ] GET `/api/files/{id}/size` — 返回 `ApiResponse<string>`
- [ ] GET `/api/files/{id}/type` — 返回 `ApiResponse<FileTypeInfo>`

## FriendsApi.cs（14 端点）功能验证
- [ ] POST `/api/friends/request` — 接受 `SendFriendRequestRequest` body
- [ ] PUT `/api/friends/request/{friendId}` — 接受 `HandleFriendRequestRequest` body
- [ ] GET `/api/friends` — 返回 `ApiResponse<IEnumerable<FriendDto>>`
- [ ] GET `/api/friends/requests` — 返回 `ApiResponse<IEnumerable<FriendRequestDto>>`
- [ ] GET `/api/friends/sent-requests` — 返回 `ApiResponse<IEnumerable<FriendRequestDto>>`
- [ ] DELETE `/api/friends/{friendId}` — 返回 `ApiResponse`
- [ ] PUT `/api/friends/{friendId}/block?block=` — 支持 `block` 查询参数
- [ ] PUT `/api/friends/{friendId}/remark` — 接受 `UpdateFriendRemarkRequest` body
- [ ] PUT `/api/friends/{friendId}/star?star=` — 支持 `star` 查询参数
- [ ] PUT `/api/friends/{friendId}/mute?mute=` — 支持 `mute` 查询参数
- [ ] GET `/api/friends/blocked` — 返回列表
- [ ] GET `/api/friends/starred` — 返回列表
- [ ] GET `/api/friends/count` — 返回 `ApiResponse<int>`
- [ ] GET `/api/friends/search?searchTerm=` — 搜索好友

## GroupsApi.cs（18 端点）功能验证
- [ ] POST `/api/groups` — 创建群组
- [ ] GET `/api/groups` — 获取我的群组列表
- [ ] GET `/api/groups/{id}` — 获取群组详情
- [ ] PUT `/api/groups/{id}/info` — 更新群组信息
- [ ] DELETE `/api/groups/{id}` — 解散群组
- [ ] GET `/api/groups/{id}/members` — 获取群成员
- [ ] POST `/api/groups/{id}/members` — 添加成员
- [ ] DELETE `/api/groups/{id}/members/{userId}` — 移除成员
- [ ] PUT `/api/groups/{id}/admins` — 设置管理员
- [ ] PUT `/api/groups/{id}/transfer` — 转让群主
- [ ] PUT `/api/groups/{id}/members/{userId}/mute` — 禁言成员
- [ ] DELETE `/api/groups/{id}/members/{userId}/mute` — 解除禁言
- [ ] PUT `/api/groups/{id}/members/{userId}/ban` — 拉黑成员
- [ ] DELETE `/api/groups/{id}/members/{userId}/ban` — 解除拉黑
- [ ] GET `/api/groups/public` — 获取公开群组
- [ ] GET `/api/groups/search` — 搜索群组
- [ ] GET `/api/groups/{id}/member-count` — 获取成员数量
- [ ] GET `/api/groups/{id}/is-member/{userId}` — 检查是否成员

## MessagesApi.cs（8 端点）功能验证
- [ ] POST `/api/messages` — 发送消息，返回 `ApiResponse<MessageDto>`
- [ ] GET `/api/messages/{id}` — 获取消息详情
- [ ] GET `/api/messages/sessions/{sessionId}/messages` — 获取会话消息列表（分页）
- [ ] DELETE `/api/messages/{id}?reason=` — 撤回消息
- [ ] POST `/api/messages/{id}/forward` — 转发消息，接受 `ForwardMessageRequest` body
- [ ] PUT `/api/messages/{id}/read` — 标记已读
- [ ] GET `/api/messages/search` — 搜索消息
- [ ] GET `/api/messages/unread` — 获取未读消息

## ReportsApi.cs（2 端点）功能验证
- [ ] POST `/api/reports` — 提交举报，接受 `SubmitReportRequest` body
- [ ] GET `/api/reports/my` — 获取我的举报列表（分页）

## SessionsApi.cs（11 端点）功能验证
- [ ] POST `/api/sessions` — 创建会话
- [ ] GET `/api/sessions` — 获取会话列表
- [ ] GET `/api/sessions/{id}` — 获取会话详情
- [ ] PUT `/api/sessions/{id}/pin?pin=` — 置顶/取消
- [ ] PUT `/api/sessions/{id}/mute?mute=` — 免打扰/取消
- [ ] DELETE `/api/sessions/{id}` — 删除会话
- [ ] GET `/api/sessions/{id}/participants` — 获取参与者
- [ ] POST `/api/sessions/{id}/participants` — 添加参与者
- [ ] DELETE `/api/sessions/{id}/participants/{userId}` — 移除参与者
- [ ] GET `/api/sessions/pinned` — 获取置顶会话
- [ ] GET `/api/sessions/unread-count` — 获取未读数

## TweetsApi.cs（17 端点）功能验证
- [ ] POST `/api/tweets` — 发布推文
- [ ] POST `/api/tweets/draft` — 保存草稿
- [ ] GET `/api/tweets/{tweetGuid}` — 获取推文详情
- [ ] GET `/api/tweets/user/{userGuid}` — 获取用户推文
- [ ] GET `/api/tweets/timeline` — 获取时间线
- [ ] GET `/api/tweets/trending` — 获取热门推文
- [ ] PUT `/api/tweets/{tweetGuid}` — 更新草稿
- [ ] DELETE `/api/tweets/{tweetGuid}` — 删除推文
- [ ] POST `/api/tweets/{tweetGuid}/pin` — 置顶
- [ ] POST `/api/tweets/{tweetGuid}/unpin` — 取消置顶
- [ ] POST `/api/tweets/{tweetGuid}/like` — 点赞
- [ ] DELETE `/api/tweets/{tweetGuid}/like` — 取消点赞
- [ ] POST `/api/tweets/{tweetGuid}/favorite` — 收藏
- [ ] DELETE `/api/tweets/{tweetGuid}/favorite` — 取消收藏
- [ ] POST `/api/tweets/{tweetGuid}/share` — 转发
- [ ] POST `/api/tweets/{tweetGuid}/coin` — 投币
- [ ] POST `/api/tweets/{tweetGuid}/view` — 记录查看

## SignalR MessageHub 增强
- [ ] `MessageHub` 添加了 `[Authorize]` 特性
- [ ] `OnConnectedAsync` 中有身份验证检查和异常处理
- [ ] Hub 中不包含重复的 `SendMessageRequest` 类定义
- [ ] SignalR 连接在 `Program.cs` 中正确配置（`app.MapHub<MessageHub>("/MessageHub")`）

## 错误处理与日志
- [ ] 所有 Mini API 端点包含 try-catch 块
- [ ] 异常被捕获并返回 `ApiResponse.Error()` 格式
- [ ] 关键业务节点有日志记录（`ILogger<T>`）

## API 文档
- [ ] 每个端点组注册使用了 `WithTags()` 或 `WithGroupName()`
- [ ] 每个端点使用了 `Produces<T>()` 声明响应类型
- [ ] 接受 body 的端点使用了 `Accepts<T>()` 声明请求类型
- [ ] Scalar API Reference 可正常浏览所有端点

## 整体验证
- [ ] `Message.Web.API` 项目编译通过（0 错误）
- [ ] 所有 89 个 API 端点的路由与原 Controller 完全一致
- [ ] 所有 89 个 API 端点的请求/响应格式与原 Controller 完全一致
