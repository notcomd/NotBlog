# Tasks

## Task 1: 基础设施准备

* [ ] Task 1.1: 创建 `Message.Web.API\APIs\` 目录

* [ ] Task 1.2: 在 `Message.Web.API\GlobalUsings.cs` 中添加 mini API 所需的命名空间引用（`Microsoft.AspNetCore.Builder`, `Microsoft.AspNetCore.Http`, `Microsoft.AspNetCore.Routing`）

## Task 2: 转换 AuditController → AuditApi.cs（5 端点）

* [ ] Task 2.1: 创建 `APIs\AuditApi.cs`，实现 GET `api/audit/tweets/pending` — 获取待审核推文列表

* [ ] Task 2.2: 实现 POST `api/audit/tweets/{tweetGuid}/approve` — 审核通过推文

* [ ] Task 2.3: 实现 POST `api/audit/tweets/{tweetGuid}/reject` — 驳回推文

* [ ] Task 2.4: 实现 GET `api/audit/reports/pending` — 获取待处理举报列表

* [ ] Task 2.5: 实现 POST `api/audit/reports/{reportGuid}/resolve` — 处理举报

## Task 3: 转换 CommentsController → CommentsApi.cs（4 端点）

* [ ] Task 3.1: 创建 `APIs\CommentsApi.cs`，实现 POST `api/comments` — 发布评论

* [ ] Task 3.2: 实现 GET `api/comments/tweet/{tweetGuid}` — 获取推文评论列表

* [ ] Task 3.3: 实现 GET `api/comments/{commentGuid}/replies` — 获取评论回复列表

* [ ] Task 3.4: 实现 DELETE `api/comments/{commentGuid}` — 删除评论

## Task 4: 转换 FilesController → FilesApi.cs（10 端点）

* [ ] Task 4.1: 创建 `APIs\FilesApi.cs`，实现 POST `api/files` — 上传文件

* [ ] Task 4.2: 实现 GET `api/files/{id}` — 获取文件信息

* [ ] Task 4.3: 实现 GET `api/files/{id}/download` — 下载文件（重定向）

* [ ] Task 4.4: 实现 GET `api/files/{id}/preview` — 预览文件

* [ ] Task 4.5: 实现 DELETE `api/files/{id}` — 删除文件

* [ ] Task 4.6: 实现 GET `api/files/message/{messageId}` — 获取消息附件

* [ ] Task 4.7: 实现 GET `api/files/{id}/exists` — 检查文件存在

* [ ] Task 4.8: 实现 GET `api/files/{id}/download-count` — 获取下载次数

* [ ] Task 4.9: 实现 GET `api/files/{id}/size` — 获取文件大小

* [ ] Task 4.10: 实现 GET `api/files/{id}/type` — 获取文件类型

## Task 5: 转换 FriendsController → FriendsApi.cs（14 端点）

* [ ] Task 5.1: 创建 `APIs\FriendsApi.cs`，实现 POST `api/friends/request` — 发送好友请求

* [ ] Task 5.2: 实现 PUT `api/friends/request/{friendId}` — 处理好友请求

* [ ] Task 5.3: 实现 GET `api/friends` — 获取好友列表

* [ ] Task 5.4: 实现 GET `api/friends/requests` — 获取收到的好友请求

* [ ] Task 5.5: 实现 GET `api/friends/sent-requests` — 获取发出的好友请求

* [ ] Task 5.6: 实现 DELETE `api/friends/{friendId}` — 删除好友

* [ ] Task 5.7: 实现 PUT `api/friends/{friendId}/block` — 屏蔽/解除好友

* [ ] Task 5.8: 实现 PUT `api/friends/{friendId}/remark` — 修改好友备注

* [ ] Task 5.9: 实现 PUT `api/friends/{friendId}/star` — 星标/取消星标

* [ ] Task 5.10: 实现 PUT `api/friends/{friendId}/mute` — 免打扰/取消

* [ ] Task 5.11: 实现 GET `api/friends/blocked` — 获取屏蔽列表

* [ ] Task 5.12: 实现 GET `api/friends/starred` — 获取星标列表

* [ ] Task 5.13: 实现 GET `api/friends/count` — 获取好友数量

* [ ] Task 5.14: 实现 GET `api/friends/search` — 搜索好友

## Task 6: 转换 GroupsController → GroupsApi.cs（18 端点）

* [ ] Task 6.1: 创建 `APIs\GroupsApi.cs`，实现 POST `api/groups` — 创建群组

* [ ] Task 6.2: 实现 GET `api/groups` — 获取我的群组列表

* [ ] Task 6.3: 实现 GET `api/groups/{id}` — 获取群组详情

* [ ] Task 6.4: 实现 PUT `api/groups/{id}/info` — 更新群组信息

* [ ] Task 6.5: 实现 DELETE `api/groups/{id}` — 解散群组

* [ ] Task 6.6: 实现 GET `api/groups/{id}/members` — 获取群成员列表

* [ ] Task 6.7: 实现 POST `api/groups/{id}/members` — 添加群成员

* [ ] Task 6.8: 实现 DELETE `api/groups/{id}/members/{userId}` — 移除群成员

* [ ] Task 6.9: 实现 PUT `api/groups/{id}/admins` — 设置管理员

* [ ] Task 6.10: 实现 PUT `api/groups/{id}/transfer` — 转让群主

* [ ] Task 6.11: 实现 PUT `api/groups/{id}/members/{userId}/mute` — 禁言成员

* [ ] Task 6.12: 实现 DELETE `api/groups/{id}/members/{userId}/mute` — 解除禁言

* [ ] Task 6.13: 实现 PUT `api/groups/{id}/members/{userId}/ban` — 拉黑成员

* [ ] Task 6.14: 实现 DELETE `api/groups/{id}/members/{userId}/ban` — 解除拉黑

* [ ] Task 6.15: 实现 GET `api/groups/public` — 获取公开群组

* [ ] Task 6.16: 实现 GET `api/groups/search` — 搜索群组

* [ ] Task 6.17: 实现 GET `api/groups/{id}/member-count` — 获取成员数量

* [ ] Task 6.18: 实现 GET `api/groups/{id}/is-member/{userId}` — 检查是否群成员

## Task 7: 转换 MessagesController → MessagesApi.cs（8 端点）

* [ ] Task 7.1: 创建 `APIs\MessagesApi.cs`，实现 POST `api/messages` — 发送消息

* [ ] Task 7.2: 实现 GET `api/messages/{id}` — 获取消息详情

* [ ] Task 7.3: 实现 GET `api/messages/sessions/{sessionId}/messages` — 获取会话消息列表

* [ ] Task 7.4: 实现 DELETE `api/messages/{id}` — 撤回消息

* [ ] Task 7.5: 实现 POST `api/messages/{id}/forward` — 转发消息

* [ ] Task 7.6: 实现 PUT `api/messages/{id}/read` — 标记消息已读

* [ ] Task 7.7: 实现 GET `api/messages/search` — 搜索消息

* [ ] Task 7.8: 实现 GET `api/messages/unread` — 获取未读消息

## Task 8: 转换 ReportsController → ReportsApi.cs（2 端点）

* [ ] Task 8.1: 创建 `APIs\ReportsApi.cs`，实现 POST `api/reports` — 提交举报

* [ ] Task 8.2: 实现 GET `api/reports/my` — 获取我的举报列表

## Task 9: 转换 SessionsController → SessionsApi.cs（11 端点）

* [ ] Task 9.1: 创建 `APIs\SessionsApi.cs`，实现 POST `api/sessions` — 创建会话

* [ ] Task 9.2: 实现 GET `api/sessions` — 获取会话列表

* [ ] Task 9.3: 实现 GET `api/sessions/{id}` — 获取会话详情

* [ ] Task 9.4: 实现 PUT `api/sessions/{id}/pin` — 置顶/取消置顶

* [ ] Task 9.5: 实现 PUT `api/sessions/{id}/mute` — 免打扰/取消

* [ ] Task 9.6: 实现 DELETE `api/sessions/{id}` — 删除会话

* [ ] Task 9.7: 实现 GET `api/sessions/{id}/participants` — 获取会话参与者

* [ ] Task 9.8: 实现 POST `api/sessions/{id}/participants` — 添加会话参与者

* [ ] Task 9.9: 实现 DELETE `api/sessions/{id}/participants/{userId}` — 移除参与者

* [ ] Task 9.10: 实现 GET `api/sessions/pinned` — 获取置顶会话

* [ ] Task 9.11: 实现 GET `api/sessions/unread-count` — 获取未读会话数

## Task 10: 转换 TweetsController → TweetsApi.cs（17 端点）

* [ ] Task 10.1: 创建 `APIs\TweetsApi.cs`，实现 POST `api/tweets` — 发布推文

* [ ] Task 10.2: 实现 POST `api/tweets/draft` — 保存草稿

* [ ] Task 10.3: 实现 GET `api/tweets/{tweetGuid}` — 获取推文详情

* [ ] Task 10.4: 实现 GET `api/tweets/user/{userGuid}` — 获取用户推文列表

* [ ] Task 10.5: 实现 GET `api/tweets/timeline` — 获取时间线

* [ ] Task 10.6: 实现 GET `api/tweets/trending` — 获取热门推文

* [ ] Task 10.7: 实现 PUT `api/tweets/{tweetGuid}` — 更新草稿

* [ ] Task 10.8: 实现 DELETE `api/tweets/{tweetGuid}` — 删除推文

* [ ] Task 10.9: 实现 POST `api/tweets/{tweetGuid}/pin` — 置顶推文

* [ ] Task 10.10: 实现 POST `api/tweets/{tweetGuid}/unpin` — 取消置顶

* [ ] Task 10.11: 实现 POST `api/tweets/{tweetGuid}/like` — 点赞

* [ ] Task 10.12: 实现 DELETE `api/tweets/{tweetGuid}/like` — 取消点赞

* [ ] Task 10.13: 实现 POST `api/tweets/{tweetGuid}/favorite` — 收藏

* [ ] Task 10.14: 实现 DELETE `api/tweets/{tweetGuid}/favorite` — 取消收藏

* [ ] Task 10.15: 实现 POST `api/tweets/{tweetGuid}/share` — 转发

* [ ] Task 10.16: 实现 POST `api/tweets/{tweetGuid}/coin` — 投币

* [ ] Task 10.17: 实现 POST `api/tweets/{tweetGuid}/view` — 记录查看

## Task 11: Program.cs 更新与 Controller 清理

* [ ] Task 11.1: 修改 `Program.cs`，移除 `builder.Services.AddControllers()` 和 `app.MapControllers()`

* [ ] Task 11.2: 在 `Program.cs` 中调用所有 9 个 `MapXxxApi()` 扩展方法注册 mini API

* [ ] Task 11.3: 删除 `Controllers\` 目录下全部 9 个 .cs 文件

## Task 12: SignalR MessageHub 增强

* [ ] Task 12.1: 在 `MessageHub.cs` 添加 `[Authorize]` 特性确保连接安全性

* [ ] Task 12.2: 添加连接异常处理和日志记录

* [ ] Task 12.3: 确保 Hub 使用的 DTO 与 `Dto/` 目录一致

# Task Dependencies

* Task 2-10 相互独立，可完全并行执行

* Task 11 依赖 Task 2-10（需所有 API 模块就绪后更新 Program.cs）

* Task 12 独立于其他任务

