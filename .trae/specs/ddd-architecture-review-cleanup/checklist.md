# DDD 架构审查与整理 — 验证检查清单

## 领域层（Message.Domain）

### SeedWork 修复
- [x] `IUnitOfWork.SavaChangesAsync` 已重命名为 `SaveChangesAsync`
- [x] `IUnitOfWork.SavaEntitiesAsync` 已重命名为 `SaveEntitiesAsync`
- [x] `Entity.DomainEventbus` 已重命名为 `DomainEvents`
- [x] `Entity.ClearDomainEvents` 中引用的属性名已同步更新

### NuGet 依赖净化
- [x] `Message.Domain.csproj` 不再引用 `Microsoft.AspNetCore.Mvc.Core`
- [x] `Message.Domain.csproj` 不再引用 `Microsoft.Extensions.Configuration.Abstractions`
- [x] `Message.Domain.csproj` 不再引用 `Microsoft.Extensions.DependencyInjection.Abstractions`
- [x] 项目编译通过，无因移除包引用导致的编译错误

### 实体继承修复
- [x] `TweetInteraction` 继承 `Entity`，拥有 `Id` 属性，支持领域事件
- [x] `TweetNotification` 继承 `Entity`，拥有 `Id` 属性，支持领域事件
- [x] `MessageRecall` 继承 `Entity`，拥有 `Id` 属性，支持领域事件
- [x] `MessageForward` 继承 `Entity`，拥有 `Id` 属性，支持领域事件

### 封装性修复
- [x] `RecallConfig` 所有属性 setter 为 `private set`，通过工厂方法/构造函数设置
- [x] `MessageForward.ForwardComment` setter 为 `private set`
- [x] `Group.Description` setter 为 `private set`
- [x] `Group.Avatar` setter 为 `private set`
- [x] `GroupMember.Nickname` setter 为 `private set`
- [x] `GroupMember.MuteEndTime` setter 为 `private set`
- [x] `FileAttachment.Description` setter 为 `private set`

### 领域事件补全
- [x] `Group.RemoveMember()` 触发 `GroupMemberRemovedEvent`
- [x] `Group.Dismiss()` 触发 `GroupDissolvedEvent`
- [x] `Group.TransferOwnership()` 触发领域事件 (`GroupOwnershipTransferredEvent`)
- [x] `MessageFriends.Accept()` 触发 `FriendshipAcceptedEvent`
- [x] `MessageFriends.Reject()` 触发领域事件 (`FriendshipRejectedEvent`)
- [x] `ChatSession.CreatePrivateSession()` 触发 `SessionCreatedEvent`
- [x] `ChatSession.CreateGroupSession()` 触发 `SessionCreatedEvent`

### 仓储接口规范化
- [x] `IGroupRepository` 继承 `IRepository<Group>`
- [x] `IMessageFriendsRepository` 继承 `IRepository<MessageFriends>`
- [x] `IFileAttachmentRepository` 继承 `IRepository<FileAttachment>`
- [x] `Group` 实现 `IAggregateRoot`
- [x] `MessageFriends` 实现 `IAggregateRoot`
- [x] `FileAttachment` 实现 `IAggregateRoot`

## 基础设施层（Message.Infrastructure）

### EntityConfig 修复
- [x] `TweetConfiguration` 中 `_media` 未被 Ignore，正常映射到 `MediaUrls` 列
- [x] `TweetConfiguration` 中 `_hashtags` 未被 Ignore，正常映射到 `Hashtags` 列
- [x] `TweetConfiguration` 中列类型为 `nvarchar(max)`（非 `jsonb`）
- [x] `TweetReportConfiguration` 中 `_evidenceUrls` 列类型为 `nvarchar(max)`（非 `jsonb`）
- [x] `ChatSessionConfiguration` 中 `Participants` 映射方案一致（HasConversion，非 Ignore）
- [x] `GroupConfiguration` 命名空间为 `Message.Infrastructure.EntityConfig` 且与其他配置一致
- [x] `TweetInteractionConfiguration` 中 `InteractGuid` 已改为 `Id`（适配 Entity 基类）
- [x] `TweetNotificationConfiguration` 中 `NotifyGuid` 已改为 `Id`（适配 Entity 基类）

### 仓储查询修复
- [x] `ChatSessionRepository.GetByUserIdAsync` 按 `userId` 正确过滤
- [x] `ChatSessionRepository.GetActiveSessionsAsync` 按 `userId` 正确过滤
- [x] `ChatSessionRepository.GetPinnedSessionsAsync` 按 `userId` 正确过滤
- [x] `TweetRepository.GetByAuthorAsync` 正确应用分页（`.Skip()`/`.Take()`）
- [x] `TweetRepository.GetPendingAuditAsync` 正确应用分页（`.Skip()`/`.Take()`）
- [x] `TweetRepository.GetTrendingAsync` 正确应用分页（`.Skip()`/`.Take()`）
- [x] `TweetRepository.GetTrendingAsync` 签名修正（移除不必要的 `authorGuid` 参数）
- [x] `ITweetRepository.GetTrendingAsync` 接口签名同步修正
- [x] `TweetNotificationRepository` 中 `NotifyGuid` 已改为 `Id`

### DbContext 拼写修正
- [x] `MessageDbContext.SaveEntitiesAsync` 方法名拼写正确
- [x] 所有 Provider 中 `_unitOfWork.SavaEntitiesAsync()` 已替换为 `SaveEntitiesAsync()`
- [x] Message 项目无残留 `Sava` 拼写错误（其他项目中的 `Sava` 拼写不在本次修复范围）

### Stub 方法补全
- [x] `FriendProvider.SendFriendRequestAsync` 有完整实现（非 `return null`）
- [x] `GroupProvider.CreateGroupAsync` 有完整实现（非 `return null`）
- [x] `GroupProvider.AddMemberAsync` 有完整实现（非空方法体）
- [x] `TweetProvider.GetTrendingAsync` 有完整实现（非 `return null`）

### DI 注册
- [x] `RedisCacheService` 已在 `ServiceCollectionExtensions` 中注册
- [x] `SessionCacheService` 已在 `ServiceCollectionExtensions` 中注册
- [x] `UnreadCountCacheService` 已在 `ServiceCollectionExtensions` 中注册
- [x] `UserStatusCacheService` 已在 `ServiceCollectionExtensions` 中注册

### 代码整洁
- [x] `AuditProvider.cs` 不含未使用的 `using System.Runtime.CompilerServices`

## 应用接口层（Message.Web.API）

### Controller 领域逻辑泄漏修复
- [x] `TweetsController` 不再直接注入 `ITweetInteractionRepository`
- [x] `ITweetProvider` 包含 `GetInteractionStatusAsync` 方法
- [x] `TweetProvider` 实现了 `GetInteractionStatusAsync`
- [x] `TweetsController` 通过 `ITweetProvider` 获取交互状态
- [x] `TweetsController` 不再直接调用 `LinkMetadata.Create()`
- [x] `TweetsController` 不再包含 `ParseVisibility()` 方法
- [x] `ReportsController` 中的 `Enum.Parse` 已移至 Provider
- [x] `AuditController` 中的 `Enum.Parse` 和业务规则判断已移至 Provider
- [x] `SessionsController` 中的 SessionType 分支逻辑已移至 `ChatSessionProvider`
- [x] `AuditController` 和 `ReportsController` 中移除冗余 `using Message.Domain.Dto`

### 代码去重
- [x] `SendMessageRequest` 仅有一处定义
- [x] `MapToDto` 提取为单一扩展方法（`MessageMappingExtensions.cs`），MessagesController 和 MessageHub 统一使用 `message.MapToDto()`
- [x] 事件处理器中 `NotifyGuid` 已改为 `Id`（TweetApprovedEventHandler、TweetRejectedEventHandler、ReportResolvedEventHandler）

### GlobalUsings
- [x] `Message.Web.API/GlobalUsings.cs` 已创建（含 15 个常用命名空间引用）
- [x] `Message.Web.API` 中各文件冗余 `using` 声明已清理
- [x] `Message.Infrastructure/GlobalUsings.cs` 已扩展覆盖常用命名空间（新增 8 个 global using）
- [x] `Message.Infrastructure` 中各文件冗余 `using` 声明已清理

## 整体验证
- [x] `Message.Domain` 项目编译通过（0 错误）
- [x] `Message.Infrastructure` 项目编译通过（0 错误）
- [x] `Message.Web.API` 项目编译通过（0 错误）
- [x] 全解决方案编译通过，无编译错误
