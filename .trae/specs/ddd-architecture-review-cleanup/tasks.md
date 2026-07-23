# Tasks

## Task 1: 领域层（Message.Domain）基础修复
- [x] Task 1.1: 修复 `SeedWork\IUnitOfWork.cs` 拼写错误：`SavaChangesAsync` → `SaveChangesAsync`，`SavaEntitiesAsync` → `SaveEntitiesAsync`
- [x] Task 1.2: 修复 `SeedWork\Entity.cs` 属性命名：`DomainEventbus` → `DomainEvents`
- [x] Task 1.3: 修复 `SeedWork\Entity.cs` 中 `ClearDomainEvents` 方法引用的属性名同步更新
- [x] Task 1.4: 移除 `Message.Domain.csproj` 中不应存在的 NuGet 引用：`Microsoft.AspNetCore.Mvc.Core`、`Microsoft.Extensions.Configuration.Abstractions`、`Microsoft.Extensions.DependencyInjection.Abstractions`

## Task 2: 领域层（Message.Domain）实体修复
- [x] Task 2.1: `TweetInteraction` 继承 `Entity` 基类，添加无参构造函数（EF Core 需要），调整构造函数
- [x] Task 2.2: `TweetNotification` 继承 `Entity` 基类，添加无参构造函数，调整构造函数
- [x] Task 2.3: `MessageRecall` 继承 `Entity` 基类，添加无参构造函数，调整构造函数
- [x] Task 2.4: `MessageForward` 继承 `Entity` 基类，添加无参构造函数，调整构造函数
- [x] Task 2.5: 修复封装性：`RecallConfig` 所有属性 setter 改为 `private set`，添加工厂方法
- [x] Task 2.6: 修复封装性：`MessageForward.ForwardComment` setter 改为 `private set`
- [x] Task 2.7: 修复封装性：`Group.Description`、`Group.Avatar` setter 改为 `private set`，确保有对应的业务方法
- [x] Task 2.8: 修复封装性：`GroupMember.Nickname`、`GroupMember.MuteEndTime` setter 改为 `private set`
- [x] Task 2.9: 修复封装性：`FileAttachment.Description` setter 改为 `private set`

## Task 3: 领域层（Message.Domain）领域事件补全
- [x] Task 3.1: `Group.RemoveMember()` — 取消注释 `AddDomainEvent(new GroupMemberRemovedEvent(...))`
- [x] Task 3.2: `Group.Dismiss()` — 添加 `AddDomainEvent(new GroupDissolvedEvent(...))`
- [x] Task 3.3: `Group.TransferOwnership()` — 添加合适的领域事件触发
- [x] Task 3.4: `MessageFriends.Accept()` — 添加 `AddDomainEvent(new FriendshipAcceptedEvent(...))`
- [x] Task 3.5: `MessageFriends.Reject()` — 添加合适的领域事件触发
- [x] Task 3.6: `ChatSession` 工厂方法（`CreatePrivateSession`、`CreateGroupSession`）— 添加 `AddDomainEvent(new SessionCreatedEvent(...))`

## Task 4: 领域层（Message.Domain）仓储接口规范化
- [x] Task 4.1: `IGroupRepository` 继承 `IRepository<Group>`
- [x] Task 4.2: `IMessageFriendsRepository` 继承 `IRepository<MessageFriends>`
- [x] Task 4.3: `IFileAttachmentRepository` 继承 `IRepository<FileAttachment>`
- [x] Task 4.4: 确保 `Group`、`MessageFriends`、`FileAttachment` 实现 `IAggregateRoot`

## Task 5: 基础设施层（Message.Infrastructure）配置修复
- [x] Task 5.1: 修复 `TweetConfiguration.cs` — 移除 `_media` / `_hashtags` 的 `.Ignore()` 调用
- [x] Task 5.2: 修复 `TweetConfiguration.cs` — 将 `jsonb` 改为 `nvarchar(max)`
- [x] Task 5.3: 修复 `TweetReportConfiguration.cs` — 将 `_evidenceUrls` 的 `jsonb` 改为 `nvarchar(max)`
- [x] Task 5.4: 修复 `GroupConfiguration.cs` 命名空间：`Message.Infrastructure.EntityFramework.EntityConfig` → `Message.Infrastructure.EntityConfig`
- [x] Task 5.5: 修复 `ChatSessionConfiguration.cs` — 将 `Participants` 从 Ignore 改为正确的 EF Core 映射（`nvarchar(max)` + HasConversion）

## Task 6: 基础设施层（Message.Infrastructure）仓储修复
- [x] Task 6.1: 修复 `ChatSessionRepository.cs` — `GetByUserIdAsync` 添加 `userId` 过滤条件
- [x] Task 6.2: 修复 `ChatSessionRepository.cs` — `GetActiveSessionsAsync` 添加 `userId` 过滤条件
- [x] Task 6.3: 修复 `ChatSessionRepository.cs` — `GetPinnedSessionsAsync` 添加 `userId` 过滤条件
- [x] Task 6.4: 修复 `TweetRepository.cs` — `GetByAuthorAsync` 添加 `.Skip()`/`.Take()` 分页
- [x] Task 6.5: 修复 `TweetRepository.cs` — `GetPendingAuditAsync` 添加 `.Skip()`/`.Take()` 分页
- [x] Task 6.6: 修复 `TweetRepository.cs` — `GetTrendingAsync` 添加 `.Skip()`/`.Take()` 分页

## Task 7: 基础设施层（Message.Infrastructure）DbContext 与拼写修正
- [x] Task 7.1: 修复 `MessageDbContext.cs` — `SavaEntitiesAsync` → `SaveEntitiesAsync`
- [x] Task 7.2: 同步更新所有 Provider 中的 `_unitOfWork.SavaEntitiesAsync()` 调用为 `SaveEntitiesAsync()`
- [x] Task 7.3: 同步更新所有引用 `IUnitOfWork` 方法的代码
- [x] Task 7.4: 移除 `AuditProvider.cs` 中未使用的 `using System.Runtime.CompilerServices`

## Task 8: 基础设施层（Message.Infrastructure）Stub 方法补全
- [x] Task 8.1: 实现 `FriendProvider.SendFriendRequestAsync` 完整逻辑
- [x] Task 8.2: 实现 `GroupProvider.CreateGroupAsync` 完整逻辑
- [x] Task 8.3: 实现 `GroupProvider.AddMemberAsync` 完整逻辑
- [x] Task 8.4: 实现 `TweetProvider.GetTrendingAsync` 完整逻辑

## Task 9: 基础设施层（Message.Infrastructure）DI 注册补全
- [x] Task 9.1: 在 `ServiceCollectionExtensions.cs` 中注册 `RedisCacheService`
- [x] Task 9.2: 在 `ServiceCollectionExtensions.cs` 中注册 `SessionCacheService`
- [x] Task 9.3: 在 `ServiceCollectionExtensions.cs` 中注册 `UnreadCountCacheService`
- [x] Task 9.4: 在 `ServiceCollectionExtensions.cs` 中注册 `UserStatusCacheService`

## Task 10: 应用接口层（Message.Web.API）Controller 修复
- [x] Task 10.1: 移除 `TweetsController` 中 `ITweetInteractionRepository` 的直接注入，改为通过 `ITweetProvider` 查询交互状态
- [x] Task 10.2: 在 `ITweetProvider` 中添加 `GetInteractionStatusAsync` 方法并在 `TweetProvider` 中实现
- [x] Task 10.3: 将 `TweetsController` 中 `LinkMetadata.Create()` 调用移至 `TweetProvider`
- [x] Task 10.4: 将 `TweetsController` 中 `ParseVisibility()` 方法移至 `TweetProvider`
- [x] Task 10.5: 将 `ReportsController` 和 `AuditController` 中的 `Enum.Parse` 逻辑移至对应 Provider
- [x] Task 10.6: 将 `SessionsController` 中 SessionType 分支逻辑移至 `ChatSessionProvider`

## Task 11: 应用接口层（Message.Web.API）代码去重
- [x] Task 11.1: 消除 `SendMessageRequest` 重复定义（`Dto/Request/Requests.cs` 与 `Hubs/MessageHub.cs`），统一为一个
- [x] Task 11.2: 将 `Message.MapToDto` 提取为共用扩展方法（`MessagesController` 和 `MessageHub` 中使用同一实现）

## Task 12: 命名空间引用规范化（GlobalUsings）
- [x] Task 12.1: 扩展 `Message.Infrastructure/GlobalUsings.cs`，添加常用命名空间引用
- [x] Task 12.2: 清理 `Message.Infrastructure` 中各文件冗余的 `using` 声明
- [x] Task 12.3: 新建 `Message.Web.API/GlobalUsings.cs`，统一管理 API 层命名空间引用
- [x] Task 12.4: 清理 `Message.Web.API` 中各文件冗余的 `using` 声明

# Task Dependencies
- Task 2（实体修复）依赖 Task 1.2（DomainEvents 重命名可能影响实体）
- Task 3（领域事件补全）依赖 Task 2（实体继承修复后，`AddDomainEvent` 方法才可用）
- Task 5（配置修复）独立于 Task 1-4
- Task 6（仓储修复）可能受 Task 4（仓储接口规范化）影响
- Task 7（拼写修正）依赖 Task 1.1（IUnitOfWork 拼写修正）
- Task 8（Stub 补全）独立于其他任务
- Task 9（DI 注册）独立于其他任务
- Task 10（Controller 修复）依赖 Task 8.4（TweetProvider.GetTrendingAsync 实现）
- Task 11（代码去重）独立于其他任务
- Task 12（GlobalUsings）建议在所有其他任务完成后执行
