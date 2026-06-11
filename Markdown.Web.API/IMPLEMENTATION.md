# Markdown.Web.API 实现说明

## ✅ 已完成的功能

### 1. MarkDownRepository CRUD 操作

**文件位置**: `F:\NotBlog\Markdown.Infrastructure\Repository\MarkDownRepository.cs`

实现了以下方法：

- `FindAsync()`: 根据 MarkDownGuid 查找文档
- `UpDataAsync()`: 更新文档内容（使用实体的 UpDataByMarkDownAsync 方法）
- `DeleteAsync()`: 删除文档（当前实现为硬删除，可改为软删除）
- `UnitOfWork`: 提供工作单元支持事务操作

**特点**:

- 使用 EF Core 异步查询
- 包含完整的日志记录
- 异常处理和验证逻辑
- 支持无跟踪查询（AsNoTracking）

### 2. MarkDownDbContext 持久化配置

**文件位置**: `F:\NotBlog\Markdown.Infrastructure\EntityFramework\MarkDownDbContext.cs`

**核心功能**:

- `SavaChangesAsync()`: 保存更改并自动分发领域事件
- `SavaEntitiesAsync()`: 事务性保存实体
- `OnModelCreating()`: 配置实体映射（包括 JSON 列存储集合）

**领域事件集成**:

```csharp
await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
```

每次保存时会自动触发领域事件处理器。

### 3. EventBus 配置

**文件位置**: `F:\NotBlog\Markdown.Web.API\Program.cs`

**配置步骤**:

1. 引用 Evenbus 项目
2. 配置 RabbitMQ 连接选项（从 appsettings.json 读取）
3. 注册 EventBus 服务
4. 在中间件管道中使用 EventBus

**appsettings.json 配置**:

```json
{
  "EventBus": {
    "HostName": "localhost",
    "ExchangeName": "markdown_events",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

## 📁 新增文件结构

### Application 层（CQRS 模式）

```
Markdown.Web.API/
├── Application/
│   ├── Commands/                    # 命令定义
│   │   └── CreateMarkdownCommand.cs
│   ├── CommandHandlers/             # 命令处理器
│   │   └── CreateMarkdownCommandHandler.cs
│   ├── DomainEventHandlers/         # 领域事件处理器
│   │   └── MarkdownCreatedDomainEventHandler.cs
│   └── IntegrationEventHandlers/    # 集成事件处理器
│       └── MarkdownCreatedEventHandler.cs
└── Apis/                            # API 控制器
    └── Controllers/
        └── MarkdownController.cs
```

## 🔄 事件流程

### 1. 领域事件（Domain Events）

- **触发时机**: 实体状态变更时自动添加领域事件
- **处理器**: `MarkdownCreatedDomainEventHandler`
- **用途**: 处理聚合根内部的业务逻辑

### 2. 集成事件（Integration Events）

- **触发时机**: Command Handler 中手动发布
- **处理器**: `MarkdownCreatedEventHandler`
- **用途**: 跨服务通信、消息队列异步处理

## 📝 使用示例

### 创建 Markdown 文档

**API 请求**:

```http
POST /api/markdown
Content-Type: application/json

{
  "markUserGuid": "550e8400-e29b-41d4-a716-446655440000",
  "markDownName": "我的第一篇博客",
  "markDownContent": "# Hello World\n这是内容...",
  "tags": ["技术", ".NET"]
}
```

**执行流程**:

1. Controller 接收请求
2. 创建 `CreateMarkdownCommand` 命令
3. NotMediator 路由到对应的 Handler
4. Handler 构建实体并保存到数据库
5. 自动触发领域事件（通过 DbContext）
6. 手动发布集成事件（通过 EventBus）
7. RabbitMQ 将事件分发给订阅者

## 🔧 依赖服务

需要确保以下服务运行：

- **PostgreSQL**: 数据库连接字符串在 appsettings.json
- **RabbitMQ**: 用于 EventBus 消息队列

## 🎯 下一步建议

1. **实现 Service 层**: 封装复杂的业务逻辑
2. **添加验证器**: 使用 FluentValidation 验证 Command
3. **实现 CQRS 分离**: 为查询创建单独的 Query 和 Handler
4. **添加单元测试**: 测试 Repository、Command Handler、Event Handler
5. **实现软删除**: 修改 DeleteAsync 为标记删除而非物理删除
6. **添加缓存**: 使用 Redis 缓存热点数据

## ⚠️ 注意事项

1. **事务管理**: `SavaEntitiesAsync()` 提供了事务支持，但需要确保正确使用
2. **领域事件清理**: 事件发布后会自动从实体的 DomainEventbus 中清除
3. **RabbitMQ 重连**: EventBus 内置了自动重连机制
4. **日志记录**: 所有关键操作都有日志输出，便于调试
