# Controller → Mini API 转换与 SignalR 集成 Spec

## Why
当前 `Message.Web.API` 使用传统 Controller-based API 模式。项目需要迁移至 ASP.NET Core Mini API 格式以简化代码结构、提升性能，并通过 SignalR 增强实时通信能力。本次转换涵盖 9 个 Controller、89 个 API 端点。

## What Changes

### 1. Mini API 转换
- 将 9 个 Controller 全部转为 Mini API 形式的 Route Handler
- 所有端点按功能域拆分到 `Message.Web.API\APIs\` 目录下的独立文件：
  - `AuditApi.cs` — 5 个端点（审核管理）
  - `CommentsApi.cs` — 4 个端点（评论管理）
  - `FilesApi.cs` — 10 个端点（文件管理）
  - `FriendsApi.cs` — 14 个端点（好友管理）
  - `GroupsApi.cs` — 18 个端点（群组管理）
  - `MessagesApi.cs` — 8 个端点（消息管理）
  - `ReportsApi.cs` — 2 个端点（举报管理）
  - `SessionsApi.cs` — 11 个端点（会话管理）
  - `TweetsApi.cs` — 17 个端点（推文管理）

### 2. Program.cs 更新
- 移除 `builder.Services.AddControllers()` 和 `app.MapControllers()`
- 新增各 API 模块的 `MapXxxApi()` 扩展方法注册调用
- 保留现有中间件管线（ExceptionHandling、UserContext、SignalR、Scalar OpenAPI）

### 3. SignalR 增强
- 增强 `MessageHub` 安全性：添加 `[Authorize]` 特性，验证连接用户的身份
- 稳定连接：实现心跳机制、断线重连处理、异常捕获
- 确保 Hub 中引用的 `SendMessageRequest` 与 `Dto/Request/Requests.cs` 一致（已在上次重构中解决）

### 4. 错误处理与日志
- 所有 Mini API 端点统一使用 try-catch 包裹业务逻辑
- 使用 `ILogger<T>` 记录请求入口、业务关键节点、异常信息
- 保持与现有 `ApiResponse` 统一的错误响应格式

### 5. API 文档
- 利用 Mini API 的 `WithOpenApi()` 和 `WithSummary()`/`WithDescription()` 为每个端点添加文档元数据
- 保留 Scalar API Reference 文档界面
- 新增 `Produces<T>` 和 `Accepts<T>` 类型约束

## Impact
- Affected specs: 无（新功能）
- Affected code:
  - `Message.Web.API/Program.cs` — 移除 Controller 注册，新增 Mini API 注册
  - `Message.Web.API/Controllers/` — 删除 9 个 Controller 文件
  - `Message.Web.API/APIs/` — 新建 9 个 API 模块文件
  - `Message.Web.API/Hubs/MessageHub.cs` — SignalR 增强（Authorization、心跳）
- **BREAKING**: 保持 API 路由和契约完全不变，客户端无需任何改动

## ADDED Requirements

### Requirement: Mini API 功能等价性
每个 Mini API 端点 SHALL 保留原 Controller 端点的所有功能和业务逻辑，包括授权检查、参数验证、响应格式。

#### Scenario: 端点等价性
- **WHEN** 客户端调用 `/api/tweets` 的 POST 方法
- **THEN** 返回格式、状态码和业务行为与原 Controller 完全一致

### Requirement: SignalR 安全连接
SignalR Hub SHALL 验证连接用户的身份，拒绝未认证连接。

#### Scenario: 未认证连接被拒
- **WHEN** 未认证用户尝试连接 `/MessageHub`
- **THEN** 连接被拒绝，返回 401

### Requirement: 统一错误处理
所有 Mini API 端点 SHALL 使用 try-catch 捕获异常并返回标准 `ApiResponse.Error()` 格式。

#### Scenario: 业务异常转换为标准错误响应
- **WHEN** Provider 层抛出 `InvalidOperationException`
- **THEN** 返回 `ApiResponse.Error("xxx")`，状态码 400

### Requirement: API 文档完整性
每个 Mini API 端点 SHALL 包含 OpenAPI 元数据（Summary、Description、Produces、Accepts）。

#### Scenario: Scalar 文档界面可浏览所有端点
- **WHEN** 在开发环境访问 Scalar API Reference
- **THEN** 所有 89 个端点均有描述性文档

## MODIFIED Requirements
无。

## REMOVED Requirements
- `builder.Services.AddControllers()` — 移除 Controller 注册
- `app.MapControllers()` — 移除 Controller 路由映射
- 删除 `Controllers/` 目录下全部 9 个 .cs 文件
