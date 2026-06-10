# Message 实时聊天服务开发计划

## 文档版本

| 版本 | 日期 | 作者 | 说明 |
|------|------|------|------|
| 1.0 | 2026-03-26 | AI Assistant | 初始版本 |

---

## 一、项目概述

### 1.1 项目范围

本文档定义 Message 实时聊天服务的开发计划，**不包含用户认证功能**。用户认证由独立的服务模块实现，本服务模块通过既定接口与认证服务交互。

### 1.2 模块边界

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Message 实时聊天服务模块                               │
├─────────────────────────────────────────────────────────────────────────────┤
│  ✓ 消息收发（文本、图片、文件、语音等）                                        │
│  ✓ 会话管理（私聊、群聊）                                                     │
│  ✓ 好友关系管理                                                               │
│  ✓ 群组管理                                                                   │
│  ✓ 实时通信（SignalR）                                                        │
│  ✓ 文件传输                                                                   │
│  ✓ 消息撤回/转发                                                              │
│  ✓ 离线消息存储                                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│  ✗ 用户认证（由独立认证服务模块实现）                                          │
│  ✗ 身份验证逻辑                                                               │
│  ✗ 权限管理                                                                   │
│  ✗ Token 生成与验证                                                           │
│  ✗ 密码管理                                                                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.3 与认证服务的交互

| 交互场景 | 认证服务职责 | 本服务职责 |
|----------|--------------|------------|
| 用户登录后 | 生成 JWT Token | 接收用户信息，初始化在线状态 |
| API 请求 | 验证 Token 有效性 | 从 Token 获取用户ID，执行业务逻辑 |
| SignalR 连接 | 验证连接 Token | 建立连接，管理用户会话 |
| 权限校验 | 提供用户角色信息 | 根据角色执行业务权限判断 |

---

## 二、开发阶段规划

### 阶段一：基础设施完善（第1周）

#### 2.1.1 任务清单

| 任务ID | 任务描述 | 优先级 | 预计工时 | 依赖 |
|--------|----------|--------|----------|------|
| T1-001 | 修复 Entity 基类（支持 Guid 主键） | 高 | 2h | - |
| T1-002 | 修复领域事件方法签名 | 高 | 1h | T1-001 |
| T1-003 | 创建 FileSize 值对象 | 中 | 1h | - |
| T1-004 | 创建 MessageContent 值对象 | 中 | 1h | - |
| T1-005 | 创建 GeoLocation 值对象 | 低 | 1h | - |
| T1-006 | 完善 FileAttachmentRepository 实现 | 高 | 2h | - |
| T1-007 | 更新 ServiceCollectionExtensions | 中 | 1h | T1-006 |
| T1-008 | 创建数据库迁移脚本 | 中 | 2h | - |

#### 2.1.2 交付物

- [ ] Entity 基类支持 Guid 主键
- [ ] 领域事件正确发布
- [ ] 值对象定义完成
- [ ] 所有仓储实现完成
- [ ] 数据库迁移脚本

---

### 阶段二：API 层开发（第2-3周）

#### 2.2.1 任务清单

| 任务ID | 任务描述 | 优先级 | 预计工时 | 依赖 |
|--------|----------|--------|----------|------|
| T2-001 | 创建基础 DTO 类 | 高 | 2h | - |
| T2-002 | 创建消息相关 DTO | 高 | 2h | T2-001 |
| T2-003 | 创建会话相关 DTO | 高 | 1h | T2-001 |
| T2-004 | 创建好友相关 DTO | 高 | 1h | T2-001 |
| T2-005 | 创建群组相关 DTO | 高 | 1h | T2-001 |
| T2-006 | 创建文件相关 DTO | 中 | 1h | T2-001 |
| T2-007 | 实现 MessagesController | 高 | 4h | T2-002 |
| T2-008 | 实现 SessionsController | 高 | 3h | T2-003 |
| T2-009 | 实现 FriendsController | 高 | 3h | T2-004 |
| T2-010 | 实现 GroupsController | 高 | 4h | T2-005 |
| T2-011 | 实现 FilesController | 中 | 3h | T2-006 |
| T2-012 | 创建全局异常处理中间件 | 高 | 2h | - |
| T2-013 | 配置 FluentValidation | 中 | 2h | T2-001 |
| T2-014 | 完善 OpenAPI 文档 | 中 | 2h | T2-007~T2-011 |

#### 2.2.2 API 端点设计

**消息模块 API**

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | /api/messages | 发送消息 |
| GET | /api/messages/{id} | 获取消息详情 |
| GET | /api/sessions/{sessionId}/messages | 获取会话消息列表 |
| DELETE | /api/messages/{id} | 撤回消息 |
| POST | /api/messages/{id}/forward | 转发消息 |
| PUT | /api/messages/{id}/read | 标记已读 |
| GET | /api/messages/search | 搜索消息 |

**会话模块 API**

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | /api/sessions | 创建会话 |
| GET | /api/sessions | 获取用户会话列表 |
| GET | /api/sessions/{id} | 获取会话详情 |
| PUT | /api/sessions/{id}/pin | 置顶会话 |
| PUT | /api/sessions/{id}/mute | 静音会话 |
| DELETE | /api/sessions/{id} | 解散会话 |

**好友模块 API**

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | /api/friends/request | 发送好友请求 |
| PUT | /api/friends/request/{id} | 处理好友请求 |
| GET | /api/friends | 获取好友列表 |
| GET | /api/friends/requests | 获取好友请求列表 |
| DELETE | /api/friends/{id} | 删除好友 |
| PUT | /api/friends/{id}/block | 屏蔽好友 |
| PUT | /api/friends/{id}/remark | 设置备注 |

**群组模块 API**

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | /api/groups | 创建群组 |
| GET | /api/groups | 获取用户群组列表 |
| GET | /api/groups/{id} | 获取群组详情 |
| POST | /api/groups/{id}/members | 添加成员 |
| DELETE | /api/groups/{id}/members/{userId} | 移除成员 |
| PUT | /api/groups/{id}/admins | 设置管理员 |
| PUT | /api/groups/{id}/transfer | 转让群主 |
| DELETE | /api/groups/{id} | 解散群组 |

**文件模块 API**

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | /api/files | 上传文件 |
| GET | /api/files/{id} | 下载文件 |
| GET | /api/files/{id}/preview | 文件预览 |
| DELETE | /api/files/{id} | 删除文件 |

#### 2.2.3 交付物

- [ ] 所有 DTO 类定义完成
- [ ] 所有 Controller 实现完成
- [ ] 全局异常处理配置
- [ ] 请求验证配置
- [ ] API 文档完善

---

### 阶段三：实时通信开发（第4-5周）

#### 2.3.1 任务清单

| 任务ID | 任务描述 | 优先级 | 预计工时 | 依赖 |
|--------|----------|--------|----------|------|
| T3-001 | 创建 MessageHub 基础结构 | 高 | 2h | - |
| T3-002 | 实现连接管理（OnConnectedAsync） | 高 | 2h | T3-001 |
| T3-003 | 实现断开连接处理（OnDisconnectedAsync） | 高 | 2h | T3-001 |
| T3-004 | 实现消息发送（SendMessage） | 高 | 3h | T3-002 |
| T3-005 | 实现消息已读（MarkAsRead） | 高 | 2h | T3-004 |
| T3-006 | 实现消息撤回（RecallMessage） | 中 | 2h | T3-004 |
| T3-007 | 实现会话加入/离开 | 中 | 2h | T3-002 |
| T3-008 | 实现输入状态指示 | 低 | 1h | T3-002 |
| T3-009 | 创建 IConnectionManager 接口 | 高 | 1h | - |
| T3-010 | 实现 RedisConnectionManager | 高 | 3h | T3-009 |
| T3-011 | 配置 SignalR Redis Backplane | 高 | 2h | T3-010 |
| T3-012 | 创建离线消息服务 | 高 | 3h | T3-010 |
| T3-013 | 实现离线消息推送 | 高 | 2h | T3-012 |

#### 2.3.2 SignalR Hub 设计

```csharp
public interface IMessageHub
{
    // 连接管理
    Task OnConnectedAsync();
    Task OnDisconnectedAsync(Exception? exception);
    
    // 消息操作
    Task SendMessage(Guid sessionId, SendMessageRequest request);
    Task SendFileMessage(Guid sessionId, FileUploadRequest request);
    Task MarkAsRead(Guid messageId);
    Task RecallMessage(Guid messageId);
    
    // 会话操作
    Task JoinSession(Guid sessionId);
    Task LeaveSession(Guid sessionId);
    
    // 状态指示
    Task SendTypingIndicator(Guid sessionId);
}

public interface IMessageClient
{
    Task ReceiveMessage(MessageDto message);
    Task MessageRecalled(Guid messageId);
    Task MessageRead(Guid messageId, Guid readerId);
    Task UserOnline(Guid userId);
    Task UserOffline(Guid userId);
    Task TypingIndicator(Guid sessionId, Guid userId);
    Task UnreadCountUpdated(Guid sessionId, int count);
}
```

#### 2.3.3 连接管理架构

```
┌─────────────────────────────────────────────────────────────┐
│                    SignalR 连接管理                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Redis 存储结构:                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Key: "message:user:{userId}:connections"             │   │
│  │ Value: Set<connectionId>                            │   │
│  │ TTL: 24小时                                         │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Key: "message:connection:{connectionId}:user"        │   │
│  │ Value: userId                                       │   │
│  │ TTL: 24小时                                         │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Key: "message:user:{userId}:status"                  │   │
│  │ Value: { status, lastOnlineTime }                   │   │
│  │ TTL: 5分钟                                          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### 2.3.4 交付物

- [ ] SignalR Hub 实现完成
- [ ] 连接管理服务实现
- [ ] Redis Backplane 配置
- [ ] 离线消息服务实现
- [ ] 实时推送功能测试通过

---

### 阶段四：领域事件处理（第6周）

#### 2.4.1 任务清单

| 任务ID | 任务描述 | 优先级 | 预计工时 | 依赖 |
|--------|----------|--------|----------|------|
| T4-001 | 创建 MessageSentEventHandler | 高 | 2h | T3-004 |
| T4-002 | 创建 MessageReadEventHandler | 高 | 1h | T3-005 |
| T4-003 | 创建 MessageRecalledEventHandler | 中 | 1h | T3-006 |
| T4-004 | 创建 MessageForwardedEventHandler | 中 | 1h | T2-007 |
| T4-005 | 创建 SessionCreatedEventHandler | 中 | 1h | T2-008 |
| T4-006 | 创建 GroupCreatedEventHandler | 中 | 1h | T2-010 |
| T4-007 | 创建 GroupMemberJoinedEventHandler | 中 | 1h | T2-010 |
| T4-008 | 创建 GroupMemberLeftEventHandler | 中 | 1h | T2-010 |
| T4-009 | 创建 UserOnlineEventHandler | 高 | 1h | T3-002 |
| T4-010 | 创建 UserOfflineEventHandler | 高 | 1h | T3-003 |
| T4-011 | 创建 FileUploadedEventHandler | 中 | 1h | T2-011 |

#### 2.4.2 事件处理流程

```
MessageSentEvent
    │
    ├──▶ MessageSentEventHandler
    │        ├── 更新会话最后消息
    │        ├── 推送给在线用户 (SignalR)
    │        └── 存储离线消息 (Redis)
    │
    └──▶ NotificationEventHandler (可选)
             └── 推送外部通知
```

#### 2.4.3 交付物

- [ ] 所有领域事件处理器实现
- [ ] 事件处理流程测试通过

---

### 阶段五：性能优化（第7周）

#### 2.5.1 任务清单

| 任务ID | 任务描述 | 优先级 | 预计工时 | 依赖 |
|--------|----------|--------|----------|------|
| T5-001 | 实现用户状态缓存 | 高 | 2h | T3-010 |
| T5-002 | 实现会话信息缓存 | 高 | 2h | T2-008 |
| T5-003 | 实现消息列表缓存 | 中 | 2h | T2-007 |
| T5-004 | 优化数据库查询索引 | 高 | 3h | - |
| T5-005 | 实现批量消息插入 | 中 | 2h | T2-007 |
| T5-006 | 配置数据库连接池 | 中 | 1h | - |
| T5-007 | 实现消息队列异步处理 | 中 | 3h | - |
| T5-008 | 性能基准测试 | 高 | 4h | T5-001~T5-007 |

#### 2.5.2 缓存策略

| 缓存类型 | Key 格式 | TTL | 说明 |
|----------|----------|-----|------|
| 用户状态 | `message:user:{userId}:status` | 5分钟 | 在线状态、连接数 |
| 会话信息 | `message:session:{sessionId}:info` | 30分钟 | 参与者、最后消息 |
| 未读计数 | `message:user:{userId}:unread:{sessionId}` | 1小时 | 未读消息数 |
| 离线消息 | `message:user:{userId}:offline` | 7天 | 离线消息队列 |

#### 2.5.3 交付物

- [ ] Redis 缓存实现
- [ ] 数据库索引优化
- [ ] 性能测试报告

---

### 阶段六：测试与文档（第8周）

#### 2.6.1 任务清单

| 任务ID | 任务描述 | 优先级 | 预计工时 | 依赖 |
|--------|----------|--------|----------|------|
| T6-001 | 编写单元测试 | 高 | 8h | 阶段1-5 |
| T6-002 | 编写集成测试 | 高 | 6h | 阶段1-5 |
| T6-003 | 编写 API 使用文档 | 中 | 4h | 阶段2 |
| T6-004 | 编写部署文档 | 中 | 2h | - |
| T6-005 | 编写运维手册 | 低 | 2h | - |
| T6-006 | 代码审查与重构 | 中 | 4h | 阶段1-5 |

#### 2.6.2 测试覆盖要求

| 测试类型 | 覆盖率要求 | 重点测试内容 |
|----------|------------|--------------|
| 单元测试 | ≥ 80% | 领域实体、业务逻辑 |
| 集成测试 | ≥ 60% | API 端点、数据库操作 |
| 端到端测试 | 核心流程 | 消息收发、实时推送 |

#### 2.6.3 交付物

- [ ] 单元测试通过
- [ ] 集成测试通过
- [ ] API 文档完成
- [ ] 部署文档完成

---

## 三、与认证服务的集成规范

### 3.1 接口约定

本服务模块通过以下方式与认证服务交互：

```
┌─────────────────┐                    ┌─────────────────┐
│  认证服务模块    │                    │  Message 服务    │
│  (独立服务)     │                    │  (本模块)        │
└────────┬────────┘                    └────────┬────────┘
         │                                      │
         │  1. JWT Token (HTTP Header)          │
         │─────────────────────────────────────▶│
         │                                      │
         │  2. 用户信息查询 (可选 API)           │
         │◀─────────────────────────────────────│
         │                                      │
         │  3. Token 验证结果 (中间件)           │
         │─────────────────────────────────────▶│
         │                                      │
```

### 3.2 Token 使用规范

```csharp
// 从 JWT Token 获取用户信息
public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Guid GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst("sub")?.Value 
            ?? _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return Guid.TryParse(userIdClaim, out var userId) 
            ? userId 
            : throw new UnauthorizedAccessException("无效的用户标识");
    }

    public string? GetUserRole()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Role)?.Value;
    }
}
```

### 3.3 SignalR 认证配置

```csharp
// Program.cs 中配置 SignalR 认证
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        // 由认证服务提供配置
        // 本服务仅使用，不生成/验证 Token
    });

// SignalR Hub 认证
app.MapHub<MessageHub>("/hubs/message", options =>
{
    options.CloseOnAuthenticationExpiration = true;
});
```

---

## 四、技术实现规范

### 4.1 代码规范

| 规范项 | 要求 |
|--------|------|
| 命名规范 | 遵循 C# 编码规范 |
| 注释语言 | 中文注释 |
| 异常处理 | 使用全局异常中间件 |
| 日志记录 | 使用 ILogger，结构化日志 |
| 依赖注入 | 构造函数注入 |

### 4.2 数据库规范

| 规范项 | 要求 |
|--------|------|
| 表名 | 复数形式，如 Users、Messages |
| 主键 | Guid 类型，非自增 |
| 索引 | 根据查询场景设计 |
| 软删除 | 使用 IsDeleted 标记 |
| 时间字段 | UTC 时间存储 |

### 4.3 API 规范

| 规范项 | 要求 |
|--------|------|
| 路由 | RESTful 风格 |
| 版本 | URL 版本控制 /api/v1/ |
| 响应格式 | 统一 ApiResponse<T> |
| 错误码 | 标准化错误码定义 |
| 分页 | 使用游标分页 |

---

## 五、里程碑与验收标准

### 5.1 里程碑

| 里程碑 | 完成时间 | 验收标准 |
|--------|----------|----------|
| M1 - 基础设施 | 第1周末 | Entity 基类修复，所有仓储实现完成 |
| M2 - API 层 | 第3周末 | 所有 API 端点可用，文档完善 |
| M3 - 实时通信 | 第5周末 | SignalR 连接稳定，消息实时推送 |
| M4 - 事件处理 | 第6周末 | 所有事件处理器实现完成 |
| M5 - 性能优化 | 第7周末 | 缓存实现，性能达标 |
| M6 - 测试发布 | 第8周末 | 测试通过，文档完成 |

### 5.2 性能验收标准

| 指标 | 目标值 | 测试方法 |
|------|--------|----------|
| 消息延迟 | < 100ms | 端到端测试 |
| 并发连接 | 10,000+ | 压力测试 |
| 消息吞吐量 | 5,000 条/秒 | 基准测试 |
| API 响应时间 | < 50ms (P95) | APM 监控 |
| 数据库查询 | < 10ms (P95) | 慢查询日志 |

### 5.3 功能验收清单

| 功能模块 | 验收项 | 状态 |
|----------|--------|------|
| 消息收发 | 文本消息发送/接收 | ⬜ |
| 消息收发 | 图片消息发送/接收 | ⬜ |
| 消息收发 | 文件消息发送/接收 | ⬜ |
| 消息收发 | 语音消息发送/接收 | ⬜ |
| 消息操作 | 消息撤回 | ⬜ |
| 消息操作 | 消息转发 | ⬜ |
| 消息操作 | 消息已读回执 | ⬜ |
| 会话管理 | 私聊会话创建 | ⬜ |
| 会话管理 | 群聊会话创建 | ⬜ |
| 会话管理 | 会话置顶/静音 | ⬜ |
| 好友系统 | 好友请求发送 | ⬜ |
| 好友系统 | 好友请求处理 | ⬜ |
| 好友系统 | 好友屏蔽 | ⬜ |
| 群组功能 | 群组创建 | ⬜ |
| 群组功能 | 成员管理 | ⬜ |
| 群组功能 | 权限管理 | ⬜ |
| 实时通信 | SignalR 连接 | ⬜ |
| 实时通信 | 消息实时推送 | ⬜ |
| 实时通信 | 离线消息推送 | ⬜ |

---

## 六、风险与应对

| 风险 | 可能性 | 影响 | 应对措施 |
|------|--------|------|----------|
| SignalR 连接不稳定 | 中 | 高 | 实现重连机制，心跳检测 |
| 数据库性能瓶颈 | 中 | 高 | 索引优化，读写分离 |
| Redis 缓存穿透 | 低 | 中 | 布隆过滤器，空值缓存 |
| 消息丢失 | 低 | 高 | 消息持久化，确认机制 |
| 并发冲突 | 中 | 中 | 乐观锁，幂等设计 |

---

## 七、附录

### 7.1 项目文件结构（最终）

```
Message/
├── Message.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Events/
│   ├── IProvider/
│   ├── IRepository/
│   ├── SeedWork/
│   └── ValueObjects/          # 新增
│
├── Message.Infrastructure/
│   ├── EntityFramework/
│   ├── Provider/
│   ├── Repository/
│   ├── Services/              # 新增
│   │   ├── IConnectionManager.cs
│   │   ├── RedisConnectionManager.cs
│   │   └── IOfflineMessageService.cs
│   └── ServiceCollectionExtensions.cs
│
└── Message.Web.API/
    ├── Application/
    │   ├── Commands/
    │   ├── CommandHandlers/
    │   ├── DomainEventHandlers/
    │   └── Behaviors/
    ├── Controllers/            # 新增
    │   ├── MessagesController.cs
    │   ├── SessionsController.cs
    │   ├── FriendsController.cs
    │   ├── GroupsController.cs
    │   └── FilesController.cs
    ├── Dto/
    │   ├── Request/
    │   └── Response/
    ├── Hubs/                   # 新增
    │   └── MessageHub.cs
    ├── Middleware/             # 新增
    │   └── ExceptionHandlingMiddleware.cs
    └── Program.cs
```

### 7.2 开发环境要求

| 软件 | 版本 | 说明 |
|------|------|------|
| .NET SDK | 10.0 | 开发框架 |
| PostgreSQL | 16+ | 主数据库 |
| Redis | 7.0+ | 缓存服务 |
| RabbitMQ | 3.12+ | 消息队列 |
| Docker | 最新 | 容器化部署 |
| IDE | Rider/VS 2022 | 开发工具 |

---

**文档结束**
