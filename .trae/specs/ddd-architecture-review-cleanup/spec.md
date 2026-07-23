# DDD 架构审查与整理 Spec

## Why
Message 项目（含 `Message.Domain`、`Message.Infrastructure`、`Message.Web.API` 三个子项目）需要一次全面的架构审查，以评估其是否符合领域驱动设计（DDD）基本范式，并系统性地修复发现的问题。

## What Changes

### 1. 领域层净化（Message.Domain）
- **移除不应存在的 NuGet 引用**：删除 `Microsoft.AspNetCore.Mvc.Core`、`Microsoft.Extensions.Configuration.Abstractions`、`Microsoft.Extensions.DependencyInjection.Abstractions` 等基础设施/表示层依赖
- **修复 SeedWork 拼写错误**：将 `IUnitOfWork` 中的 `SavaChangesAsync` / `SavaEntitiesAsync` 修正为 `SaveChangesAsync` / `SaveEntitiesAsync`
- **修复 Entity 属性命名**：将 `DomainEventbus` 修正为 `DomainEvents`
- **修复缺失的领域事件**：
  - `Group.RemoveMember()` 中取消注释 `GroupMemberRemovedEvent` 的触发
  - `Group.Dismiss()` 中添加 `GroupDissolvedEvent` 触发
  - `Group.TransferOwnership()` 中添加领域事件触发
  - `MessageFriends.Accept()` 中添加 `FriendshipAcceptedEvent` 触发
  - `MessageFriends.Reject()` 中添加领域事件触发
  - `ChatSession` 工厂方法中添加 `SessionCreatedEvent` 触发
- **修复未继承 Entity 基类的实体**：`TweetInteraction`、`TweetNotification`、`MessageRecall`、`MessageForward` 应继承 `Entity` 基类
- **修复封装性**：将 `RecallConfig`、`MessageForward.ForwardComment`、`Group.Description`/`Avatar`、`GroupMember.Nickname`/`MuteEndTime`、`FileAttachment.Description` 等公开 setter 改为 `private set`
- **Repository 接口规范化**：`IGroupRepository`、`IMessageFriendsRepository`、`IFileAttachmentRepository` 应继承 `IRepository<T>`
- **识别应移动至应用层的类型**：记录 `IServices`、`IProvider`、`Dto/PagedResult` 等应迁移（本次不改动，仅记录）

### 2. 基础设施层修复（Message.Infrastructure）
- **修复 `TweetConfiguration` 字段映射 BUG**：移除 `_media` / `_hashtags` 的 `.Ignore()` 调用，使字段能正常持久化
- **修复 `jsonb` 类型不兼容**：将 `TweetConfiguration` 和 `TweetReportConfiguration` 中的 `jsonb` 改为 `nvarchar(max)`（适配 SQL Server）
- **修复 `ChatSessionRepository` 查询缺陷**：`GetByUserIdAsync`、`GetActiveSessionsAsync`、`GetPinnedSessionsAsync` 需按 `userId` 过滤
- **修复 `ChatSessionConfiguration`**：解决 `Participants` 被 Ignore 但 LINQ 查询却使用 `Participants.Contains()` 的矛盾
- **修复 `TweetRepository` 分页缺失**：`GetByAuthorAsync`、`GetPendingAuditAsync`、`GetTrendingAsync` 应正确应用 `.Skip()`/`.Take()`
- **修复 `MessageDbContext` 拼写错误**：将 `SavaChangesAsync` / `SavaEntitiesAsync` 修正为 `SaveChangesAsync` / `SaveEntitiesAsync`，并同步更新所有 Provider 中的调用
- **修复 `GroupConfiguration` 命名空间不一致**：统一为 `Message.Infrastructure.EntityConfig`
- **补全 Stub 方法**：`FriendProvider.SendFriendRequestAsync`、`GroupProvider.CreateGroupAsync`、`GroupProvider.AddMemberAsync`、`TweetProvider.GetTrendingAsync` 需实现完整逻辑
- **移除 `AuditProvider.cs` 中未使用的 `using System.Runtime.CompilerServices`**
- **注册缺失的 Redis 缓存服务**：`RedisCacheService`、`SessionCacheService`、`UnreadCountCacheService`、`UserStatusCacheService` 应加入 DI 注册

### 3. 应用接口层修复（Message.Web.API）
- **修复 `TweetsController` 中绕过 Provider 直接注入 Repository**：将 `ITweetInteractionRepository` 的直接注入移除，通过 `ITweetProvider` 提供交互状态查询
- **修复 Controller 中的领域逻辑泄漏**：
  - `TweetsController` 中 `LinkMetadata.Create()` 调用应移至 Provider
  - `TweetsController` 中 `ParseVisibility()` 方法应移至 Provider 或领域层
  - `ReportsController` 和 `AuditController` 中的 `Enum.Parse` 应在 Provider 中处理
  - `SessionsController` 中 SessionType 分支逻辑应移至 Provider
- **消除 `SendMessageRequest` 重复定义**：统一为一个定义
- **消除 `Message.MapToDto` 重复代码**：提取为共用扩展方法

### 4. 命名空间引用规范化（GlobalUsings）
- **`Message.Infrastructure` 的 `GlobalUsings.cs`** 需扩展，覆盖项目内所有常用命名空间，移除各文件中的冗余 `using` 声明
- **`Message.Web.API`** 需新建 `GlobalUsings.cs`，统一管理命名空间引用
- **`Message.Domain`** 需评估是否适合使用 GlobalUsings（领域项目通常不建议，因为领域类型应显式引用以保持独立性）

## Impact
- Affected specs: 本项目为独立审查任务，不影响其他 spec
- Affected code:
  - `Message.Domain/`：SeedWork、Entities、IRepository、Message.Domain.csproj
  - `Message.Infrastructure/`：EntityConfig、Repository、Provider、EntityFramework、Services、GlobalUsings、ServiceCollectionExtensions
  - `Message.Web.API/`：Controllers、Hubs、Program.cs、Dto

## ADDED Requirements

### Requirement: 领域层保持纯领域关注点
领域项目 SHALL NOT 引用任何基础设施或表示层框架。

#### Scenario: 移除基础设施依赖
- **WHEN** 审查 Message.Domain.csproj
- **THEN** 不应包含 `Microsoft.AspNetCore.Mvc.Core`、`Microsoft.Extensions.Configuration.Abstractions`、`Microsoft.Extensions.DependencyInjection.Abstractions` 等包引用

### Requirement: Repository 接口应继承 IRepository\<T\>
所有聚合根的 Repository 接口 SHALL 继承 `IRepository<T>` where `T : IAggregateRoot`。

#### Scenario: 仓储接口继承检查
- **WHEN** 检查 `IGroupRepository`、`IMessageFriendsRepository`、`IFileAttachmentRepository`
- **THEN** 均应继承 `IRepository<T>` 基接口

### Requirement: 领域实体应继承 Entity 基类
所有拥有独立标识的实体 SHALL 继承 `SeedWork.Entity` 基类。

#### Scenario: 实体基类继承检查
- **WHEN** 检查 `TweetInteraction`、`TweetNotification`、`MessageRecall`、`MessageForward`
- **THEN** 均继承 `Entity` 基类

### Requirement: 关键领域操作应触发领域事件
聚合根上的状态变更操作 SHALL 触发对应的领域事件。

#### Scenario: 领域事件触发完整性
- **WHEN** 执行 `Group.RemoveMember()`、`Group.Dismiss()`、`Group.TransferOwnership()`、`MessageFriends.Accept()`、`MessageFriends.Reject()`、`ChatSession` 创建
- **THEN** 应触发相应的领域事件

## MODIFIED Requirements

### Requirement: IUnitOfWork 方法命名
`IUnitOfWork` 接口中的方法 SHALL 命名为 `SaveChangesAsync` 和 `SaveEntitiesAsync`（修正拼写错误）。

#### Scenario: 方法命名检查
- **WHEN** 检查 `IUnitOfWork` 接口和所有实现
- **THEN** 方法名应为正确的英文拼写 `SaveChangesAsync` / `SaveEntitiesAsync`

## REMOVED Requirements
无。
