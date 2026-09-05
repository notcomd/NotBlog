# Message 消息/对话数据 MongoDB 迁移设计文档（v0.1）

> **定位**：对"将消息未读数据 + 用户对话数据从 PostgreSQL 迁移到 MongoDB"这一改动的**可行性评估与迁移设计方案**。
> **适用范围**：Message.Web.API / Message.Application / Message.Domain / Message.Infrastructure / NotBlog.AppHost / 部署文档
> **对标先例**：FileDev 分片跟踪已从 PostgreSQL 迁移到 MongoDB（见 `FileDev-ObjectStorage-Redesign.md`、`MongoChunkCollection.cs`），本文复用其落地方案与约束。

---

## 1. 现状诊断

### 1.1 数据落点（当前全部在 PostgreSQL）

`MessageDbContext` 相关实体（[MessageDbContext.cs](file:///f:/NotBlog/Message.Infrastructure/EntityFramework/MessageDbContext.cs#L15-L40)）：

| 实体 | 表 | 与本次改动的相关性 |
|---|---|---|
| `Message` | Messages | **核心**：未读即 `Status==Sent && ReceiverId==用户` 的行（无独立未读表） |
| `ChatSession` | ChatSessions | **核心**：会话，含参与者/最后消息/未读总览 |
| `MessageFriends` | MessageFriends | 高耦合：好友（私聊会话来源） |
| `Group` / `GroupMember` | Groups / GroupMembers | 高耦合：群聊会话 |
| `FileAttachment` | FileAttachments | 高耦合：消息附件 |
| `Tweet*` / `Circle*` / `UserInfo` 等 | 多表 | 低耦合：社区动态/圈子，属另一业务域 |

### 1.2 未读机制（现状）

- **无独立未读表**：未读信息通过 `Status` 状态机表达（`Sent`→`Read`），未读数由 DB 实时聚合。
- **Redis 缓存**：`UnreadCountCacheService`（Write-through）——写入时失效，读未命中回源 DB 重建。DB 是权威。
- **离线补推**：接收者不在线时消息照常落库（`ReceiverId` 指向对端），推送因无在线连接而跳过；重连时 `OnConnectedAsync` 从 DB `GetUnreadMessagesAsync`（`Status==Sent && ReceiverId==userId`）补推。

### 1.3 现状的一个关键约束

消息发送/未读操作目前共用一个 **EF Core `IUnitOfWork` 事务**（`SaveEntitiesAsync`：先 `DispatchDomainEventsAsync` 派发领域事件再 `SaveChanges`）。发送路径在**一个 PG 事务**里完成：
`Message` 入库 + `FileAttachment` 入库 + `ChatSession.UpdateLastMessage` 更新。

`Message` 是**聚合根**，带丰富领域事件（`MessageSentEvent` / `MessageReceivedEvent` / `MessageReadEvent` / `MessageRecalledEvent` / `MessageForwardedEvent`），由静态工厂 `CreateXxxMessage` 触发 `RaiseMessageSentEvent`，在 `SaveEntitiesAsync` 处经 EF `ChangeTracker` 收集派发。

> **这与 FileDev 的分片 POCO 迁移有本质区别**：`FileChunkRecord` 是纯文档型 POCO（无领域事件、无 UoW、无跨表事务），可零成本平移。`Message` 不是。

### 1.4 现状缺陷（促成改动的动机）

- 高写入/海量历史消息持续压 PG（Messages 表可能膨胀）。
- 会话列表/未读总览存在跨表聚合的重复计算。
- 消息附件 `Include` 加载、全文搜索（`SearchAsync`）在 PG 上较重。

---

## 2. 目标架构

### 2.0 重要结论：三阶段、冷热分离，而非一次性全迁

**风险提示**：一次性把消息+会话迁 Mongo，会同时触发"拆事务 + 弃领域事件 + 重写 40+ 查询 + 双库 join + 存量迁移"四处高成本重构，**风险过大**。本设计采用**分阶段、先冷数据后热数据**的收敛路径。

### 2.1 阶段一（低风险）：历史消息冷数据下沉 Mongo

- **发送/未读/热点链路完全不动**（仍 PG 单事务）。
- 把 `SentTime` 早于归档阈值的**历史消息（含附件）**迁到 Mongo，读历史走 Mongo，读最近走 PG。归档阈值同时给会话/热点表瘦身。
- 未读计数、离线补推不变（仍只看最近热区的 `Status==Sent`）。
- 收益：Messages 表不再无限膨胀；发送链路保持单事务，零一致性风险。

### 2.2 阶段二（完整对话迁移，高成本，审慎）

把 `Message` + `ChatSession`（+ 可选择 `FileAttachment`）完整迁 Mongo。四个前置决策必须先定，否则不可落地：

| 决策 | 选项 |
|---|---|
| D-A 事务边界 | 接受跨存储**最终一致**，或用「Mongo 单文档多写 + PG 补偿」 |
| D-B 领域事件 | 保留消息事件体系（需为 Mongo 引入事件收集/派发），或降级为纯仓储 |
| D-C 关系建模 | Mongo 内嵌 vs 引用（涉及 `$lookup`） |
| D-D 搜索 | 全文搜索 `SearchAsync` 在 Mongo 的替代方案 |

> 推荐：阶段二只在**阶段一明确收益后**再评估。本文档主体面向阶段一，阶段二给出方向与风险清单。

---

## 3. 复用 FileDev 的 Mongo 落地方案

对标 `MongoFileChunkRepository` + `MongoChunkCollection` 的既有范式：

1. **Domain 零 Mongo 依赖**：业务实体保持纯 POCO，主键（`MessageId` / `SessionId`）由 BSON 类映射显式声明为 `_id`（参照 `EnsureClassMapRegistered`）。
2. **集合装配助手**（参照 `MongoChunkCollection`）：`EnsureClassMapRegistered()` + 唯一/常用索引 `CreateOne`（幂等），进程级双检锁单例缓存集合。
3. **双模式注册**（参照 FileDev `Program.cs` L57-78）：
   - 单机分支：`MongoDb:ConnectionString`（缺省回落 `mongodb://127.0.0.1:27017`）→ `AddSingleton<IMongoClient>`。
   - Aspire 分支：`builder.AddMongoDBClient("NotFileMongo")`。
   - 统一外层：`AddSingleton<IMongoDatabase>`。
4. **新增依赖声明**：Mongo 成为必选依赖后，必须在部署文档/AppHost 标注（项目硬约束：**新增 MongoDB 依赖必须在部署说明中标注**）。

---

## 4. 数据模型设计（阶段二目标建模）

### 4.1 范式 vs 反范式取舍

Mongo 文档库面向"按会话读取"，推荐：

- **会话内每条消息为独立文档**：`message` 集合，字段 = `MessageEntity` 全字段 + `attachments` 内嵌数组（替代 `FileAttachment` 外键 + `Include`）。
- **会话为独立文档**：`chat_session` 集合，含 `participants` 内嵌数组、`lastMessage` 内嵌摘要、`idempotencyVersion`（并发乐观锁）。
- **未读**：不再依赖 `Status` 过滤，改为会话文档内 `participants[].unreadCount`、`participants[].lastReadId` 计数，读时写文档内存量更新（Write-through 语义与现 Redis 缓存一致）。

```
chat_session 集合示例
{
  "_id": SessionId,
  "type": "Private | Group",
  "participants": [{ "userId": u, "joinTime": t, "lastReadId": id, "unreadCount": 3, "pinned": false }],
  "lastMessage": { "id": m, "senderId", "content", "sentTime" },
  "createdAt": t, "updatedAt": t, "__v": n
}
```

### 4.2 索引设计

- `message` 集合：`(sessionId, sentTime)` 复合索引（会话历史分页）；`(receiverId, status)`（离线补推——阶段一同构 PG 语义）；`senderId`（已发消息查询）；`sentTime` 单列（归档/冷数据）。
- `chat_session` 集合：`participants.userId` 多键索引（`GetByUserIdAsync` / 会话列表 / 未读总览）；`type`；`__v`（乐观锁）。
- 唯一索引：`(sessionId, messageId)`（幂等去重，参照 FileDev `UniqueFileKeyIndex` 的幂等建索引方式）。

### 4.3 现查询的跨表语义迁移对照（重点）

| 现状（PG） | Mongo 落法 | 代价 |
|---|---|---|
| `GetSessionsWithUnreadMessagesAsync` 聚合未读 | 会话文档 `participants[].unreadCount` | 需在发送时**按会话更新成员计数**（Write 放大但常驻内存热路径） |
| `GetUnreadMessagesAsync` 补推 | `(receiverId, status)` 索引查询 | 同构，可行 |
| `MarkAllAsReadAsync` / 会话级已读 | 会话 `participants[].lastReadId` 前移 + 清 `unreadCount` | 一条文档原子更新，更优 |
| `GetBySessionIdAsync` 分页历史 | `(sessionId, sentTime)` + 游标分页 | skip/limit 或 keyset |
| `FileAttachment` `Include` | 内嵌 `attachments` 数组 | 复用操作需 `$addToSet`/数组更新 |
| `SearchAsync` 全文 | Mongo 文本索引 或 回落关键词 | **能力弱于 PG**，需专项方案 |

### 4.4 D2-1 单文档字段级模型（基于现有实体字段映射）

> **诚实澄清**：D2-1 并非"全部塞进一个文档"（消息无界，不可能内嵌进会话文档）。它指**每次写 = 单个文档的一条原子操作**；跨集合（插入消息 + 会话未读 `$inc`）用 Mongo 原子算符而非多文档事务。未读是"最新值"语义，短暂不一致可被离线补推自愈。

#### `message` 集合（每条消息一文档，附件内嵌）

```js
{
  _id: MessageId,            // BSON 声明为 _id（仿 FileChunkRecord 类映射）
  sessionId: Guid,           // 复合索引 (sessionId, sentTime)
  senderId: Guid,
  receiverId?: Guid,         // 私有会话设置（离线补推依据）
  messageType: "text|image|video|audio|file|location|link|expression",
  status: "pending|sent|read|recalled",
  sentTime: Date,
  deliveredTime?: Date, readTime?: Date,
  isRecalled: bool, isEncrypted: bool, isForwarded: bool,
  originalMessageId?: Guid, replyToMessageId?: Guid,
  content?: string,                                      // text / caption
  media?: { uri, thumbnailUri?, fileSize, durationSeconds, fileName, mimeType }, // 媒体类
  location?: { latitude, longitude, placeName },
  link?: { url, title?, description? },
  expression?: { code },
  attachments?: [{ attachmentId, fileId,                 // FileDev content_id（弱引用）
                   fileName, fileType, fileSize, fileUri, thumbnailUri?, mimeType }]
}
```
索引：`(sessionId, sentTime desc)`（历史分页）、`(receiverId, status)`（补推）、`(senderId, sentTime)`、文本索引 `content`（搜索，能力弱于 PG）。

#### `chat_session` 集合（每会话一文档，`participants[]` 承载遍历态）

```js
{
  _id: SessionId,
  type: "private|group",
  name?: string, groupId?: Guid,
  creatorId: Guid,
  participants: [
    { userId: Guid,               // 多键索引
      unreadCount: 3,             // $inc 维护（热路径）
      lastReadId?: Guid, lastReadAt?: Date,
      pinned?: bool }
  ],
  lastMessage?: { id, senderId, sentTime, content },   // 替代 LastMessageId/Content/Time
  createdAt: Date, dismissedAt?: Date, isDismissed: bool,
  muted?: bool,                    // 镜像实体（当前置于会话级）
  __v: n                           // 乐观锁
}
```
索引：`participants.userId` 多键、`type`。

#### 写操作映射（每条 = 单文档单原子）
| 操作 | Mongo 命令 | 一致性 |
|---|---|---|
| 发送文本 | `insert message` + `$inc session.participants[].unreadCount`（跳过发送者） | 2 次单文档原子，未读可自愈 |
| 标记已读 | `session.$set { lastReadId, unreadCount:0 }` | 单文档原子 |
| 读历史 | `find (sessionId, sentTime desc, skip/limit)` | 索引 |
| 补推 | `find (receiverId, status)` | 索引 |
| 撤回 | `message.$set { status:"recalled" }` | 单文档原子 |

> **未读两种实现**：维护 `unreadCount`（写放大、读 O(1)，当前实体正是维护式，Mirror 即可）或只存 `lastReadId` + 读时聚合（读放大）。热路径建议前者 + 周期对账。

---

## 5. 关键决策

### D1：阶段策略 —— 先冷后热，阶段一先行 ✅（推荐）

阶段一（冷数据下沉）零拆事务、零弃事件，可作为可行性验证。

### D2：事务边界（阶段二启动前必须决策）

**现状**：`Message` + `FileAttachment` + `ChatSession.UpdateLastMessage` 在一个 PG 事务内完成。
**阶段二**把 `Message`/`ChatSession` 迁 Mongo 后，一次"发送"会跨存储涉及：Mongo（message 文档 + 会话计数/lastMessage）与可能仍在 PG 的 `FileAttachment`/群成员/好友。

| 选项 | 方案 | 一致性 | 复杂度 | 适用场景 |
|---|---|---|---|---|
| **D2-1 单文档聚合**（推荐基线） | 消息与会话计数尽量落在**单个会话文档**，用 Mongo **文档级原子性**（`$inc`/`$set` 同文档内原子） | 单文档强一致 | 低 | 未读/已读/最后消息这类"同一会话内"写 |
| D2-2 Mongo 多文档事务 | Mongo 4.0+/4.2+ 事务，把 `message` + `chatSession` 放进同一事务 | 跨文档强一致（**仅限 Mongo 内**） | 中 | **前提**：Mongo 必须为**副本集**；若单点不可用 |
| D2-3 事件驱动最终一致 | 消息先落 Mongo 单文档；会话计数/通知经 **RabbitMQ EventBus**（项目已有）异步消费 | 最终一致 | 中 | 跨 Mongo+PG 且非强一致诉求的动作 |
| D2-4 放弃全迁，保持现状 | 热路径仍 PG 单事务，Mongo 仅冷读 | 强一致（现状） | 低 | 若阶段一收益不足，退回阶段一方案 |

**决策建议**：
- 写路径以 **D2-1（多写在单个会话文档内用文档级原子）** 为基线，避免绝大多数跨文档事务。
- 跨 Mongo+PG 的动作（群成员变更 + 群会话、附件仍 PG 时）用 **D2-3** 事件最终一致，不用分布式事务。
- **不默认采用 D2-2**——多文档事务要求副本集，且收益有限；除非已确认 Mongo 部署为副本集并做过性能评估。

#### D2-3 可行性评估（已核实）

**基础设施成熟**：RabbitMQ EventBus（`Notcomd.EventBus`）已在 Message 注册；`IntegrationEvent` 基类自带 `Id` 支持消费端幂等去重；`JsonIntegrationEventHandler` + `[EventBusName]` 机制与 Message 既有集成事件先例（`RegisterByUser`）一致；框架内置 `OutboxPublisher`（PG 内 outbox 表 + 后台定时扫描）。

**边界**：发送命令串行 `Create → Add → SaveEntities(PG) → 缓存失效 → PushDeliver`。消息本体落 Mongo 是**强一致**诉求（发送者要立刻看到自己发的消息）**不能异步**；可异步的仅是"PG 侧副作用"（会话 lastMessage、未读数、留 PG 的 `FileAttachment`）。

**落地模式**：消息先写 Mongo（同步强一致），再在同一 PG 事务写 `FileAttachment + outbox`，由 `OutboxPublisher` 定时发布；事件消费方更新会话 lastMessage/未读。**可靠投递归 outbox，幂等归事件 Id**。

| 项 | 结论 |
|---|---|
| 消息本体异步化 | ❌ 不可（强一致，需 Mongo 单文档同步写） |
| PG 侧副作用异步化 | ✅ 可（会话/未读/留 PG 的附件经事件最终一致） |
| 可靠发布 | ⚠️ 依赖 Outbox——**Message 当前未注册 `OutboxPublisher`**，启用前须先挂载 |
| 消费幂等 | ✅ 事件 `Id` 已具备，消费者需去重 |
| 暴露窗口 | Mongo 已写但 PG 事务失败 → "幽灵消息"；以会话 `SentTime` 收敛 + 定时对账缓解 |

**可行性结论**：可行，但**前提**是 ①消息本体由 D2-1 单文档同步写保住；②仅 PG 侧副作用入事件；③启用 Outbox；④消费幂等。若会话+消息同在 Mongo（D2-1 聚合），主发送链路无需事件，D2-3 仅在"群/好友/Tweet 仍留 PG"的边界才真正需要。

#### D2-3 使能前置：Message 启用 Outbox 的最小改动（对标 Identity 先例）

| # | 改动 | 位置 | 备注 |
|---|---|---|---|
| 1 | `OnModelCreating` 加 `ApplyConfiguration(new OutboxMessageTypeConfiguration())` | `MessageDbContext` | 前提：Message.Infrastructure 引用 Eventbus 项目（`Notcomd.EventBus.Outbox`） |
| 2 | 生成迁移建 `OutboxMessages` 表（`SentAt`、`Status+CreatedAt` 索引） | Message.Infrastructure/Migrations | 对标 Identity 迁移 |
| 3 | `builder.Services.AddOutbox<MessageDbContext>(...)` | Message Web.API/Program.cs | outbox 与 `FileAttachment`/会话**同源库**——关键前提 |
| 4 | 发送命令在 `SaveEntitiesAsync` 同 UoW 内调 `IOutboxStore.StoreAsync` | 发送 Handler | `StoreAsync` 只 `Add` 不 `SaveChanges` → 与附件**同事务**提交 |

两个必注意点：
- **事件类型注册**：`OutboxPublisher` 靠 `EventBusSubscriptionInfo.EventTypes` 反序列化；Message 有"发布但不消费"的事件须显式注册类型，否则被当"未找到类型"丢弃（对标 Identity Program.cs L116-119）。
- **`StoreAsync` 与 `SaveEntitiesAsync` 必须共用同一 MessageDbContext 作用域**，否则无法与附件同事务——落地最易踩的坑。

**结论**：改动小（配置 + 迁移 + 1 处注册 + 发送 Handler 注入 Store），已有 Identity 同款模板可照抄。

### D3：领域事件（阶段二启动前必须决策）

**现状**：`Message` 聚合根在 `SaveEntitiesAsync` 经 EF `ChangeTracker` 派发 `MessageSentEvent` 等。

**已核实的关键事实**：消息领域事件的 Handler 目前**基本都是日志占位**——`MessageSentEventHandler`、`MessageReadEventHandler` 等注入 `IConnectionManager`/`IHubContext` 但实际仅 `LogInformation + await Task.CompletedTask`（[MessageSentEventHandler.cs](file:///f:/NotBlog/Message.Web.API/Application/DomainEventHandlers/MessageSentEventHandler.cs#L20-L29)、[MessageReadEventHandler.cs](file:///f:/NotBlog/Message.Web.API/Application/DomainEventHandlers/MessageReadEventHandler.cs#L20-L29)）。真正的实时推送已在命令层 `PushDeliverAsync` 完成。**即消息领域事件体系当前对核心业务几乎惰性。**

| 选项 | 方案 | 保留语义 | 改动量 | 适用场景 |
|---|---|---|---|---|
| **D3-A 保存后继发** | Mongo 仓储保存后手动收集 `AggregateRoot.DomainEvents` 并调 `INotMediator`（模拟 `SaveEntitiesAsync` 等价点） | 完全保留 | 中 | 想延续 DDD 事件流、未来要接事件消费者 |
| **D3-B 降级纯仓储**（推荐） | 对标 `FileChunkRecord`，不再经事件系统；命令层显式触发下游（推送本来已如此） | 事件仅保留日志 | 低 | **现状事件近惰性，保留投入产出比低** |
| D3-C Outbox + EventBus 出库 | 事件写入 Mongo outbox 集合，后台发布到 RabbitMQ | 完整 + 可靠投递 | 高 | 需要**跨服务可靠事件**（被外部实时消费） |

**决策建议**：
- 推荐 **D3-B（降级为日志型事件）**：因为现状事件体系几乎不驱动业务，保留的收益低于成本。
- 若未来确实需要外部消费消息事件（通知、审计、跨服务联动），再上 **D3-C Outbox + EventBus** 一劳永逸，而不是 D3-A 的进程内手动派发。
- 结论不影响阶段一；也说明**阶段二全迁时，领域事件不是真正的堵点**（相比 D2 事务边界），降低了对本次迁移的悲观预期。

### D4：附件归属

`Message` 的 `Attachments` 内嵌进消息文档（阶段二）。`FileDev` 的文件本体/元数据仍在 FileDev，Message 只保留 `content_id/content_type` 弱引用（已有先例），**不重复存文件元数据**。

### D5：搜索

阶段一**不迁搜索**；阶段二若需全文，评估 Mongo Atlas Search 文本索引 vs 保留 `search` 数据在 PG/独立索引，不默认迁移。

---

## 6. 存量数据迁移方案

对标 FileDev 分片切换的约束（**在途数据一次性丢失需接受**，但聊天数据不可随意丢，方案必须保守）：

1. **阶段一冷数据**：可接受短暂只读窗口。流程：全量导出归档消息 → Mongo 批量 `InsertMany`（幂等键去重）→ 双读校验计数 → 切换冷读路由 → 保留 PG 归档表一段时间作为回滚。
2. **阶段二**：需要**追补窗口**。用 `SentTime` 水位线双写：切换期间新消息同时写 PG 与 Mongo（`message` 文档 + 会话计数），追平后切换。任何时刻 PG 都是权威，Mongo 落后可重放。

**回滚策略**：PG 旧表保留至少一个版本周期；Mongo 失败时一键切回 PG 读路径；未读计数 Redis 缓存语义不变，可随时重建。

---

## 7. 风险清单与缓解

| # | 风险 | 等级 | 缓解 |
|---|---|---|---|
| R1 | 跨存储一致性（拆 PG 单事务） | **高** | 阶段一不拆；阶段二用会话文档原子更新 + 事件最终一致 |
| R2 | 领域事件派遣机制失效 | 高 | D3：保存点手动派发，或降级纯仓储 |
| R3 | 跨库 join（群/好友/Tweet 仍在 PG） | 高 | 反范式内嵌 + 减少 `$lookup`；明确边界 |
| R4 | Mongo 全文搜索弱于 PG | 中 | D5：不默认迁移搜索 |
| R5 | 双库部署/运维复杂度 | 中 | 复用 FileDev 双模式注册；部署文档标注 Mongo 依赖 |
| R6 | 存量数据一次性丢失 | 中 | 水位线双写 + 追补；PG 保留期回滚 |
| R7 | 回归面大（21 消息方法 + 18 会话方法） | 高 | 分阶段；每阶段承载小改后全链路回归 |

---

## 8. 部署与 AppHost

- `NotBlog.AppHost` 需为 Message 新增 Mongo 资源（若阶段一）。参照 FileDev 已新增的 Mongo 资源；相关 csproj 加 `Aspire.Hosting.MongoDB` / `Aspire.MongoDB.Driver` 包。
- 单机：`Message.Web.API/appsettings.json` 加 `MongoDb:ConnectionString`（缺省回落本地），`Program.cs` 单机分支注册 `IMongoClient` + 集合。
- 部署文档：新增 MongoDB 依赖必须显式标注。

---

## 9. 结论与建议

1. **当前 PG + Redis（未读已缓存）+ 补推方案足以支撑中等规模**；Mongo 的收益主要在极高并发/超海量历史。
2. **不建议一次性全迁**（同时触发拆事务+弃事件+重写查询+双库 join 四处重构，风险不可控）。
3. **建议路线**：先做**阶段一冷数据下沉**（低风险、验证价值），确有收益后再评估阶段二完整迁移；阶段二动工前必须先定 D2/D3（事务边界与领域事件）。
4. 本解析与 FileDev 分片迁移独立，可同步进行，但共用 Mongo 集群与双模式注册基建。

---

## 10. 待办 / 后续

- [ ] 确认阶段一归档阈值与冷读路由方案
- [ ] 确认 Mongo 集群/副本集可用性与部署声明
- [ ] 阶段二启动前完成 D2（事务边界）与 D3（领域事件）决策
- [ ] 对照 21 个消息仓储方法 + 18 个会话仓储方法逐一评估迁移语义