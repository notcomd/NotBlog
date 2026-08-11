# 用户经验/等级系统与社区会话限制设计文档

> 日期：2026-08-09 ｜ 范围：Message（经验/等级/签到/投币/限制）、Identity（集成事件消费联动） ｜ 基线：feat/message-phase1（ab79922d）

## ✅ 实施状态（2026-08-09）

全部落地并验证：经验公式口径 A（用户确认）；签到 POST /api/user-info/sign-in（+250，每日一次，唯一索引兜底）；投币 +200 经验（同事务）；社区创建数量 ≤ 等级；群聊 MaxMembers 强制 10×等级+20；群聊会话 AddParticipant 同上限校验；RegisterByUserIntegrationEvent 消费创建 UserInfo（JsonIntegrationEventHandler + AddEventBus 接线，Infrastructure 内重复注册已移除）；MessageSchema.sql 重生成（19 表）并应用本地库。构建 0 警告 0 错误，Message.Tests 168/168。

## 1. 现状诊断

| 项 | 现状 |
|---|---|
| UserInfo 实体 | ✅ 已有（ab79922d）：UserId 主键、Level（默认 1）、Coins、BackgroundCoverUrl；**无经验值**，注释明示"升级规则后续按活跃度/经验值接入" |
| UserInfo API | ✅ 已有：GET /api/user-info/me、PUT /me/background、POST /me/coins/{add,consume} |
| RegisterByUserIntegrationEvent | ⚠️ **发布侧已实现**（Identity：RegisterByUserCommandHandler 经 Outbox 发布 + Program.cs EventTypes 声明；OAuthService/OAuthApis 注册同样发布）；**Message 消费侧缺失**——UserInfo 目前无创建入口（仅各命令 upsert 兜底） |
| MessageSchema.sql | ⚠️ 缺 `UserInfos` 表（ab79922d 未同步脚本） |
| 签到 | ❌ 无任何实现 |
| 投币 → 经验 | ❌ CoinTweetCommandHandler 仅记录互动+CoinCount，无经验发放 |
| 社区创建限制 | ❌ CreateCircleCommandHandler 无数量限制 |
| 会话人数限制 | ❌ CreateGroupCommandHandler 直接采用请求 MaxMembers（默认 500）；AddSessionParticipantCommandHandler 无人数校验 |

## 2. 需求与设计原则

- 经验值只增不减（签到/投币累积），等级由经验驱动自动升级，上限 9 级
- 等级挂钩：**创建社区的个数上限**、**群聊（Group）人数上限**、**群聊会话参与者上限**，均按**创建者**等级计算
- 所有经验/等级判定在 Message 侧完成（UserInfo 属 Message 域）；Identity 只负责注册事件发布，不改动
- 数据库变更走 MessageSchema.sql（R-10 事实源）+ 本地库执行，无 EF Migrations

## 3. 经验与等级

### 3.1 公式（⚠️ 待确认，见 3.2）

```
升级所需经验 = 500 × (5 × n) = 2500n   （n = 当前等级）
```

| 等级 n | 升到 n+1 所需经验 | 累计经验 |
|---|---|---|
| 1 | 2500 | 0（初始） |
| 2 | 5000 | 2500 |
| 3 | 7500 | 7500 |
| 4 | 10000 | 15000 |
| 5 | 12500 | 25000 |
| 6 | 15000 | 37500 |
| 7 | 17500 | 52500 |
| 8 | 20000 | 70000 |
| 9 | —（满级） | 90000 |

- 经验**每级清零**（升级后剩余经验保留带入下一级，游戏常见语义）
- 升级在 `AddExperience` 领域方法内循环完成（一次 +250/+200 最多升 1 级，循环防御性保留）

### 3.2 歧义确认

"500×(5×n)，n 是等级"有两种实现口径，**默认按 A 实施**：
- **A（推荐，字面贴合）**：从 n 级升到 n+1 级需要 2500n 经验（上表）
- **B**：达到 n 级需**累计** 2500n 经验（1 级即需 2500，语义自相矛盾，需修正为 2500×(n-1)）

## 4. 功能设计

### 4.1 UserInfo 实体扩展（Message.Domain/Entities/User/UserInfo.cs）

- 新增 `Experience`（long，累计当前级经验，升级清零）
- 新增 `LevelUpThreshold(level) => 500 * 5 * level`（静态）
- 新增 `AddExperience(long amount)`：累加 + 循环升级（`while (Level < 9 && Experience >= LevelUpThreshold(Level)) { Experience -= LevelUpThreshold(Level); Level++; }`），同时触发升级日志由调用方记录
- `SetLevel` 保留（管理员/补偿场景）
- DTO：`UserInfoDto` 增加 `Experience`、`MaxLevel=9` 常量；GetMyUserInfoQuery 默认返回 Experience=0

### 4.2 签到（新）

- 新实体 `UserSignIn`（Entities/User/）：`Id`(Version7)、`UserId`、`SignInDate`（DateOnly，UTC 自然日）、`CreateTime`；唯一约束 `(UserId, SignInDate)` 防重复签到
- 新端点 `POST /api/user-info/sign-in`：
  - 今日已签 → `400 今天已签到`（幂等语义：重复请求不重复发经验）
  - 未签 → 创建记录 + `UserInfo.AddExperience(250)`（UserInfo 不存在则 upsert 创建）
  - 返回：`{ Experience, Level, Coins }` 或升级提示
- 查询（可选，随 GET /me 一并返回今日是否已签）：`GET /api/user-info/me` 增加 `SignedInToday` 字段（一次 IsSignedInAsync 查询）

### 4.3 投币 +200 经验

- `CoinTweetCommandHandler`：投币成功后（同事务）`UserInfo.AddExperience(200)`（upsert）
- 仅投币者本人获得；重复投币仍加经验（投币行为本身重复计数——与现有 CoinTweet 允许重复投币的口径一致；如需限制每日投币次数另行立项）

### 4.4 社区创建数量限制

- `CreateCircleCommandHandler` 注入 `IUserInfoRepository`：
  - 等级 = `GetByUserIdAsync(userId)?.Level ?? 1`（无资料按 1 级）
  - 已创建圈子数 = `(await circleRepository.GetByOwnerAsync(userId)).Count()`（Active 过滤已有）
  - `count >= level` → `InvalidOperationException($"当前等级 {level} 最多可创建 {level} 个社区")`（API 映射 400）
- 不限制圈子内人数（维持现有 MaxMembers 默认 500 逻辑）

### 4.5 会话人数限制（群聊）

- `CreateGroupCommandHandler`：注入 `IUserInfoRepository`，`MaxMembers` **强制** = `10 × level + 20`（1 级 30 … 9 级 110），忽略请求传入值（前端按 GET /me 的等级提示上限）
- `Group.AddMember` 已有 `_members.Count >= MaxMembers` 校验 ✓ 自动生效（加人/拉人路径全覆盖）
- `AddSessionParticipantCommandHandler`：对 `SessionType.Group` 会话，注入 `IUserInfoRepository`，按**会话创建者**（ChatSession.CreatorId）等级校验参与者数 ≤ `10×level+20`，超限 400（防绕过群成员管理的直加路径）

| 等级 | 社区数上限 | 群聊人数上限 |
|---|---|---|
| 1 | 1 | 30 |
| 2 | 2 | 40 |
| 3 | 3 | 50 |
| 4 | 4 | 60 |
| 5 | 5 | 70 |
| 6 | 6 | 80 |
| 7 | 7 | 90 |
| 8 | 8 | 100 |
| 9 | 9 | 110 |

### 4.6 RegisterByUserIntegrationEvent 消费（Message 侧新建）

- 新文件 `Message.Web.API/Application/IntegrationEvents/EventHandlers/RegisterByUserIntegrationEventHandler.cs`：
  - record 副本 `RegisterByUserIntegrationEvent(Guid UserId)` + `RegisterTime`（字段与 Identity 发布侧一致，跨服务不共享程序集）
  - `[EventBusName("RegisterByUserIntegrationEvent")]` 于 handler 类（订阅 key 与 Identity 发布 key 对齐——Identity 事件类型无 EventBusName，routing key = 类型名）
  - 继承 `JsonIntegrationEventHandler<RegisterByUserIntegrationEvent>`
  - 逻辑：`GetByUserIdAsync` 不存在 → `UserInfo.Create(userId)` + Add + Save（幂等：已存在跳过；失败记日志不抛出，避免总线消费循环）
- `Message.Web.API/Program.cs`：加 `builder.Services.AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly())`（复用现有 `EventBus` 配置节；Release 分支已有 AddRabbitMQClient）
- GlobalUsings：补 `Notcomd.EventBus.Core`（JsonIntegrationEventHandler/EventBusName/IntegrationEvent）

### 4.7 数据库

- `MessageSchema.sql` 追加：`UserInfos`（若缺失：UserId uuid PK、Level int、Experience bigint、Coins bigint、BackgroundCoverUrl varchar(2048)、CreateTime/UpdateTime timestamptz）+ `UserSignIns`（Id uuid PK、UserId uuid、SignInDate date、CreateTime timestamptz、唯一索引 (UserId, SignInDate)、索引 (UserId)）
- 实施后重新生成脚本并应用到本地 messagepostgres（保持"与 EntityConfig 严格对照"）

## 5. 端点全表（增量）

| 方法 | 路由 | 说明 |
|---|---|---|
| POST | `/api/user-info/sign-in` | 签到 +250 经验（每日一次，重复 400） |
| GET | `/api/user-info/me` | 增字段：Experience、SignedInToday |

## 6. 涉及文件

| 层 | 文件 |
|---|---|
| Domain | UserInfo.cs（+Experience/AddExperience）、UserSignIn.cs（新）、IUserInfoRepository.cs（+IsSignedInAsync） |
| Infrastructure | UserInfoConfiguration.cs（+Experience 列）、UserSignInConfiguration.cs（新）、UserInfoRepository.cs（+IsSignedInAsync）、UserSignInRepository.cs（新）、ServiceCollectionExtensions（注册）、MessageDbContext（+DbSet）、MessageSchema.sql（+2 表） |
| Web.API | UserInfoApi.cs（+sign-in）、GetMyUserInfoQueryHandler/UserInfoDto（+Experience/SignedInToday）、CoinTweetCommandHandler（+200 经验）、CreateCircleCommandHandler（数量限制）、CreateGroupCommandHandler（MaxMembers 强制）、AddSessionParticipantCommandHandler（群聊人数校验）、RegisterByUserIntegrationEventHandler.cs（新）、Program.cs（+AddEventBus）、GlobalUsings.cs |
| 验证 | build 0 警告 0 错误；Message.Tests 全绿（CoinTweet/CreateCircle 测试构造若受影响同步） |

## 7. 实施顺序

1. Domain：UserInfo 经验/升级 + UserSignIn 实体 + 仓储接口
2. Infrastructure：配置/仓储/注册/DbSet + MessageSchema.sql 重新生成并应用本地库
3. Web.API：签到端点 + DTO + 三处限制挂钩 + 投币经验
4. 集成事件消费：handler + AddEventBus + GlobalUsings
5. 构建 + 测试 + 冒烟（签到两次第二次 400；1 级用户创建第 2 个圈子 400；群聊上限 30 人）
6. 提交推送

## 8. 兼容与风险

- 全部加性；UserInfos 已存在库中则 SQL 用 IF NOT EXISTS / ALTER 补列（本地库当前无 UserInfos 表，直接 CREATE）
- AddEventBus 使 Message 开始消费总线消息：RabbitMQ 需在线，否则订阅重试（与 Markdown 同行为）
- 等级为 1 的存量用户（无 UserInfo）创建限制立即生效（1 社区/30 人群）
- 前端需按 GET /me 的 Level 展示可创建数/人数上限提示
