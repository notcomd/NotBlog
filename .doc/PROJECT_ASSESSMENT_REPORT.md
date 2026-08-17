# NotBlog 项目全面评估报告

> 评估日期：2026-08-02 ｜ 评估方式：全仓库源码逐项目静态审查 + 解决方案构建验证
> 审查范围：NotBlog.sln 全部 25 个项目（8 个公共组件 + 6 大业务模块 + 网关/宿主 + 测试）

---

## 目录

1. [评估概览与总体结论](#一评估概览与总体结论)
2. [总体评分表](#二总体评分表)
3. [公共基础组件（NotBlog_Commons）](#三公共基础组件notblog_commons)
4. [Identity 身份认证项目群](#四identity-身份认证项目群)
5. [Message 实时聊天项目群](#五message-实时聊天项目群)
6. [Markdown 博客项目群](#六markdown-博客项目群)
7. [FileDev 文件存储项目群](#七filedev-文件存储项目群)
8. [Video 视频项目群](#八video-视频项目群)
9. [网关与宿主](#九网关与宿主notblog_yarp--notblogapphost)
10. [跨项目重大问题清单（安全红线）](#十跨项目重大问题清单安全红线)
11. [改进路线图](#十一改进路线图)
12. [附录：审查方法与数据](#十二附录审查方法与数据)

---

## 一、评估概览与总体结论

### 1.1 项目定位

NotBlog 是一个**多模块 DDD 架构的博客/社区后端系统**（.NET 10），以解决方案形式组织为 6 大业务模块 + 公共组件：

| 分组 | 项目 | 定位 |
|---|---|---|
| NotBlog_Commons | Token.JWT、EventBus、NotEmail、CacheMemory、DomainCommons、DomainInfrastructure、CommonsInitializer、ServiceDefaults | 公共基础设施 |
| NotBlog_Identity | Identity.Domain / Infrastructure / Web.API | 用户认证、角色、权限、OAuth |
| NotBlog_Message | Message.Domain / Infrastructure / Web.API / Tests | 即时聊天 + 朋友圈 Tweet |
| NotBlog_Mark | Markdown.Domain / Infrastructure / Web.API | 博客 Markdown 内容 |
| NotBlog_File | FileDev.Domain / Infrastructure / Web.API | 文件存储（分片/gRPC） |
| NotBlog_Video | Video.Domain / Infrastructure / Web.API | 视频内容 |
| 其他 | NotBlog_Yarp（网关）、NotBlog.AppHost（Aspire 宿主）、Message.Tests | 基础设施/测试 |

### 1.2 构建状态

```
dotnet build NotBlog.sln  →  0 个错误，134 个警告（约 50 秒）
```

- **可以编译**，但警告数量多（134 个），大量 `CS8618`（非空字段未初始化）、`CS9113`（未使用参数）、`CS8603/8604/8625`（空引用）。
- 构建成功 ≠ 可运行：多个模块存在**启动即失败**或**运行时必然报错**的缺陷（见各章节"阻断性问题"）。

### 1.3 总体结论

| 维度 | 评价 |
|---|---|
| **架构设计** | 优秀。DDD 分层、CQRS、领域事件、集成事件、Aspire 编排的思路完整且贯彻得较好 |
| **功能完成度** | 中等偏下。各模块有较完整的骨架，但普遍存在"实体/接口已定义、链路未打通"的现象 |
| **安全性** | 差。存在**整站可越权、身份可伪造、密钥明文入库**等上线阻断级漏洞 |
| **代码质量** | 中下。大量未使用参数、拼写错误、僵尸代码、缓存与 DB 不一致 |
| **文档** | 不均衡。CacheMemory/Message 文档较好，其余模块几乎无文档 |
| **测试** | 薄弱。仅 Message 有 106 个测试用例，其余 5 大业务模块零测试 |

**综合评级：不可直接上线（需先修复安全红线 + 链路断裂项）**

---

## 二、总体评分表

评分维度：完成度、代码质量、安全性、文档完善度，综合百分制。

| # | 项目 | 完成度 | 代码质量 | 安全性 | 文档 | **综合分** | 状态 |
|---|---|---|---|---|---|---|---|
| 1 | DomainCommons | 95 | 90 | 90 | 60 | **90** | ✅ 可用 |
| 2 | CacheMemory | 90 | 80 | 85 | 95 | **88** | ✅ 可用 |
| 3 | NotBlog.ServiceDefaults | 95 | 95 | 90 | 50 | **90** | ✅ 可用 |
| 4 | JWToken | 85 | 70 | 60 | 30 | **75** | ⚠️ 有隐患 |
| 5 | DomainInfrastructure | 80 | 65 | 70 | 20 | **70** | ⚠️ 需修复 |
| 6 | NotEmail | 80 | 70 | 60 | 20 | **70** | ⚠️ 接入错位 |
| 7 | CommonsInitializer | 70 | 60 | 60 | 20 | **65** | ⚠️ 空壳较多 |
| 8 | EventBus | 70 | 55 | 50 | 30 | **62** | ⚠️ 需修复 |
| 9 | **Identity** | 35 | 45 | 20 | 40 | **40** | 🔴 不可用 |
| 10 | **Message** | 62 | 55 | 25 | 70 | **58** | 🔴 不可用 |
| 11 | **Markdown** | 38 | 50 | 30 | 10 | **48** | 🔴 不可用 |
| 12 | **FileDev** | 55 | 50 | 30 | 10 | **55** | 🔴 不可用 |
| 13 | **Video** | 55 | 50 | 20 | 5 | **52** | 🔴 不可用 |
| 14 | NotBlog_Yarp（网关） | 65 | 70 | 50 | 50 | **65** | ⚠️ 需修复 |
| 15 | NotBlog.AppHost | 40 | 40 | 50 | 10 | **40** | 🔴 不可用 |

**项目整体综合分：约 55 / 100**

> 分级说明：
> - 🟢 **≥80**：可独立使用
> - 🟡 **60-79**：功能可用但存在安全/可靠性隐患，需修复后使用
> - 🔴 **<60**：不可直接上线，存在阻断性缺陷

---

## 三、公共基础组件（NotBlog_Commons）

整体评价：**8 个组件中 3 个优秀、3 个可用、2 个需修复**。作为全站地基，质量尚可，但存在 3 个必须处理的全局问题。

### 3.1 全局安全问题（影响所有下游）

| # | 级别 | 问题 | 位置 |
|---|---|---|---|
| C-1 | 🔴高 | **JWT 签名私钥硬编码，且 5 个服务共用同一把密钥**（`<REDACTED>`）。一处泄露全站沦陷，且无法单独轮换 | `Identity.Web.API\appsettings.json:22`、`Message.Web.API\appsettings.json:12`、`FileDev.Web.API\appsettings.json:5`、`Video.Web.API\appsettings.json:29`、`NotBlog_Yarp\appsettings.json:12` |
| C-2 | 🔴高 | **GitHub OAuth ClientSecret 明文提交**（`<REDACTED>`） | `Identity.Web.API\appsettings.json:41` |
| C-3 | 🔴高 | **SMTP 密码 / 各库 PostgreSQL 密码明文入库** | `Identity.Web.API\appsettings.json:17,58`、`Markdown.Web.API\appsettings.json:10`、`NotBlog.AppHost\appsettings.json:11-15` |
| C-4 | 🟡中 | 生产代码无条件信任任意服务器证书（gRPC） | `Identity.Web.API\Program.cs:37-42` |

### 3.2 逐组件评分

| 组件 | 综合分 | 主要问题 |
|---|---|---|
| **CacheMemory** | **88** | 全站最完整、文档最佳（USAGE.md + DEVELOPMENT.md）。隐患：①异步路径 sync-over-async 阻塞线程池（`RedisCacheService.cs:1344`）；②断线重建连接时旧 ConnectionMultiplexer 泄漏（`CacheMemoryConnection.cs:80`）；③`StringSetMany` 静默忽略过期时间（`:253`）；④分布式锁文档宣称可重入但实现不可重入 |
| **DomainCommons** | **90** | 简洁正确。仅 `StringExtension.Cut` 无 null 检查（`:39`） |
| **ServiceDefaults** | **90** | 标准 Aspire 模板。健康检查端点仅 Development 暴露（安全默认） |
| **JWToken** | **75** | 签发/验证链路可用（PBKDF2-SHA256 密码哈希方案合格）。隐患：①RefreshToken 无过期校验/无存储/可无限重放（`JwtTokenService.cs:105-130`）；②黑名单为进程内 ConcurrentDictionary，多实例失效且未接入 JwtBearer（`:84-101`）；③TokenDecryptionKey 与签名密钥同一把；④算法配置 RS256 会**静默降级 HS256**（`:152-157`）；⑤PBKDF2 10 万次迭代低于 OWASP 建议的 60 万次 |
| **DomainInfrastructure** | **70** | ①`EFCoreInitializerHelper` 反射注册使所有 DbContext 共用同一连接串（`:26-66`）；②UnitOfWorkFilter 与 DbContext 覆写的 SaveChanges **双重提交**（`UnitOfWorkFilter.cs:27-32`）；③领域事件按字符串反射查找属性，改名即失效 |
| **NotEmail** | **70** | 库完整，但 Identity 接入配置完全错位（见 Identity 章节 E-10）。465 端口用 StartTls 必失败（`SmtpEmailSender.cs:45`）；IMAP 附件无大小限制 |
| **CommonsInitializer** | **65** | `UseNotBlogPipeline()` 是空实现（只 return app），调用方误以为已配置（`ApplicationBuilderExtension.cs:16-24`）；连接串键名三套并存易配错 |
| **EventBus** | **62** | ①**Outbox 从未被任何服务启用**（`AddOutbox` 零调用，表不存在）（`ServicesCollectionExtensions.cs:212-221`）；②**失败消息永不重试**（Failed 状态被永久搁置）（`EfCoreOutboxStore.cs:25-34`）；③发布通道断线不重建，永久失败（`RabbitMqEventBus.cs:156-182`）；④消费者启动失败不重启（`:188-192`）；⑤`EventBusNameAttribute` 自定义事件名与发布 routingKey 不一致，**自定义名事件永远路由不到消费者**（`:87`）；⑥事件总线与请求总线 Dispose 时共享连接被双杀 |

### 3.3 改进建议（按优先级）

1. **密钥/密码全部移入用户机密或环境变量**（C-1/C-2/C-3）。
2. EventBus：失败消息进入重试队列；发布通道断线重建；至少在一个服务启用 Outbox 并落表。
3. JWToken：重写 RefreshToken（服务端存储 + 单次使用）；黑名单接入 JwtBearer 或改 Redis；删除算法降级逻辑。
4. DomainInfrastructure：消除双重 SaveChanges。

---

## 四、Identity 身份认证项目群

**综合分：40 / 100 ｜ 完成度 35% ｜ 安全分 20 ｜ 状态：🔴 不可用（上线阻断）**

### 4.1 完成度明细

| 模块 | 完成度 | 状态说明 |
|---|---|---|
| 邮箱密码注册/登录 | 80% | 可运行，但邮件发送配置断裂导致验证码邮件发不出（E-10） |
| 登录失败锁定（5 次锁 15 分钟） | 100% | 已实现 `UserAccessFail` |
| 角色/角色组/权限 CRUD | 90% | 命令+API+软删除齐全 |
| PermissionChecker | 70% | 角色直连权限 + 角色组继承 + DataScope 判定已实现 |
| GitHub OAuth 登录/注册 | 70% | 主流程可用；通用 OAuthService 路径不可用（E-7） |
| 头像上传（gRPC 调 FileDev） | 90% | 校验齐全 |
| 手机短信验证码 | 0% | `SmsCodeSend.SendPhoneCodeAsync` 抛 `NotImplementedException` |
| 用户信息查询 | 0% | `GetUserInfoAsync` 抛 `NotImplementedException` |
| 密码修改 | 10% | Command 存在但端点未映射；无旧密码/验证码校验 |
| 令牌刷新 | 10% | **损坏**：生成的 refresh token 与刷新逻辑不兼容（E-6） |
| Token 吊销 | 30% | 黑名单未接入 JwtBearer，吊销无效 |
| Google/Microsoft OAuth | 30% | 半成品：重定向 URI 无白名单校验；创建的 OAuth 用户不落库（E-7） |
| 账号绑定/解绑 | 20% | 仅打日志 TODO |
| 邮箱确认 | 0% | 无已验证邮箱流程 |
| 用户管理 | 10% | `GetUserAllAsync` 返回空 |

### 4.2 阻断性问题（🔴 高）

| # | 问题 | 位置 | 影响 |
|---|---|---|---|
| E-1 | **认证/授权中间件完全缺失**：`UseNotBlogPipeline()` 是空实现，Identity 是全解决方案唯一未调用 `UseAuthentication/UseAuthorization` 的服务 | `Program.cs:101`、`CommonsInitializer\ApplicationBuilderExtension.cs:16-24` | **权限/角色管理端点完全公开**，任何人可匿名创建 ADMIN 角色、删除权限、赋权；而 `RequireAuthorization` 的端点（OAuth 绑定、头像上传）恒 401 |
| E-2 | **OAuth 回调无 state 校验（登录 CSRF）**：生成 state 后不存储、回调不校验 | `OAuthService.cs:30-42,131-176`、`GithubAuthApi.cs:26-67` | 攻击者可诱导已登录用户完成攻击者账号绑定 |
| E-3 | **appsettings 硬编码真实凭据**：GitHub ClientSecret、SMTP 密码、Postgres 密码、JWT PrivateKey 全部明文提交，`.gitignore` 未覆盖 appsettings 与 .env | `appsettings.json:41,58,17,22` | 密钥泄露=全站沦陷 |
| E-4 | **改密流程无认证、无旧密码、无验证码**，仅凭 Email 即可改密 | `ChangeByPasswordCommandHandler.cs:17-19` | 一旦端点映射即任意账号接管 |
| E-5 | **Token 吊销无效**：黑名单在进程内且未接入 JwtBearer | `JwtTokenService.cs:84-101` | 吊销的 token 依然有效 |
| E-6 | **RefreshToken 机制自相矛盾**：生成 32 字节单段串，刷新逻辑却要求两段且 Base64 反序列化 ClaimData | `JwtTokenService.cs:196-204 vs 105-130` | 刷新令牌 100% 失败 |
| E-7 | **通用 OAuth 创建的用户永不落库**（`AddOneByUserAsync` 后无 SaveChanges）；GitHub ID 为数字无法被 `Guid.Parse` | `OAuthService.cs:67-88,51-57` | Google/Microsoft 流程死链；GitHub 走通用流程必抛异常 |
| E-8 | **网关权限校验端点不存在**：网关调 `/api/permission/check`、`/api/permission/datascope/{userId}`，Identity 未实现 | `NotBlog_Yarp\Permission\HttpPermissionServiceClient.cs:66-109` | 生产模式（HTTP 权限模式）下网关全站 403 |
| E-9 | **JWT 配置异常**：`ExpireSeconds: 7`（AccessToken 仅 7 秒过期） | `appsettings.json:23` | 令牌几乎立即失效，认证链路实际不可用 |
| E-10 | **邮件配置完全错位**：Program 绑定 `NotEmail` 节，配置里是 `EmailOptions` 节且键名不符（`Port` vs `SmtpPort`）；`FromEmail=null` 时 SmtpSender 直接 NRE | `Program.cs:9-10`、`appsettings.json:12-17` | 验证码邮件发不出，注册链路断裂 |

### 4.3 中危问题（🟡）

| # | 问题 | 位置 |
|---|---|---|
| E-11 | 验证码明文写入日志；命令对象整体入日志（含明文密码） | `GenerateCodeCommandHandler.cs:22`、`IdentifiedCommandHandler.cs:37-42` |
| E-12 | 登录无 IP 级限流，账号可枚举；`AccessFaildCount++` 非原子可并发绕过 | `UserService.cs:27` |
| E-13 | gRPC 客户端禁用 TLS 证书校验且未限定 DEBUG | `Program.cs:37-42` |
| E-14 | CORS 注册 AllowAll 策略 | `Program.cs:70-74` |
| E-15 | Claim 命名不一致：签发 `user_guid`，OAuth 端点查 `user_id` → OAuth 绑定恒 401 | `UserService.cs:204` vs `OAuthApis.cs:101,138` |
| E-16 | PBKDF2 10 万次迭代低于 OWASP 建议 | `HashH256Tool.cs:26` |

### 4.4 代码质量要点

- **约 20 处 `NotImplementedException` 半成品**（SmsCodeSend、RoleGroupService、UserRoleService、用户查询等）。
- **恒真/空实现**：`CreateByRoleCommandHandler` 查完直接 `return true`；`GithubAuthApi.LinkGithubByUserAsync` 恒 true；`UserRepository.AddByLoginHistoryAsync` 空实现。
- `IdentityDbContext.SavaChangesAsync` 拼写错误且恒返回 0，丢弃真实返回值。
- 幂等机制失效：`IdentifiedCommand.Id` 每次 `Guid.CreateVersion7()` 新生成，重复请求永不命中。
- 锁定状态双份冗余（`UserSafety.LockOutEnd` 与 `UserAccessFail.LockOutEnd`），登录只走后者。

### 4.5 文档

- `PERMISSION_DESIGN.md` 设计质量高，但与实现系统性偏差：文档要求独立 `NotBlog.Permission` 项目 + `[RequiredPermission]` 特性 → 实际分拆在网关与 PermissionChecker；文档要求 gRPC CheckPermission → 实际 HTTP 且端点未实现；文档要求 JWT 携带 data_scope → 实际未签发。

---

## 五、Message 实时聊天项目群

**综合分：58 / 100 ｜ 完成度 62% ｜ 安全分 25 ｜ 状态：🔴 不可用**

### 5.1 完成度明细

| 模块 | 完成度 | 说明 |
|---|---|---|
| 私聊 | 85% | 发送/历史/已读/撤回/转发链路可用；但 REST 路径无参与者校验 |
| 群聊 | 78% | 创建/成员/禁言/封禁/转让/解散齐全；**命令层零权限校验** |
| 消息撤回 | 70% | 2 分钟时限已实现；无"仅发送者可撤回"校验 |
| 消息转发 | 72% | 8 种类型转发完整；无源会话成员校验；`MessageForward` 实体未被使用 |
| 文件上传下载 | 72% | 分片/断点续传/秒传/下载计数完整；无所有权校验 |
| Session | 78% | 创建/置顶/静音/解散齐全；命令无成员资格校验 |
| 好友 | 85% | 权限卫生最好的模块 |
| Tweet/朋友圈 | 80% | 发布/点赞/收藏/置顶/分享/金币/草稿/时间线齐全；**敏感词/图片审核是日志占位** |
| 举报审核 | 75% | Admin 判定依赖可伪造的 `X-User-Roles` 头 |
| SignalR 实时推送 | 72% | 连接管理/会话群组规范；离线补推实际失效 |
| 离线消息 | 30% | `ReceiverId` 在生产代码中**从未被调用**，离线/未读恒为空 |

### 5.2 阻断性问题（🔴 高）

| # | 问题 | 位置 | 影响 |
|---|---|---|---|
| M-1 | **认证完全可绕过（全站最严重漏洞）**：`UserContextMiddleware` 无条件信任客户端 `X-User-Id`/`X-User-Roles` 头，且全项目无任何 `[Authorize]` | `Middleware\UserContextMiddleware.cs:11-19` | 携带 `X-User-Id: 任意GUID` 即可冒充任意用户；带 `X-User-Roles: Admin` 即获得管理员。绕过网关直连端口（9092）即可利用 |
| M-2 | **群组命令零权限校验**：11 个命令全部不校验调用者身份，`Group.HasPermission` 权限模型从未被调用 | `Commands\Groups\*`（如 `DismissGroupCommand.cs:16-28`） | 任意用户可解散任意群、踢人、改管理员、转让群主、封禁成员 |
| M-3 | **Session 命令零成员校验（IDOR）** | `SetSessionPinCommand.cs:17-33`、`DismissSessionCommand` 等 | 任意用户可置顶/解散/改成员任意会话 |
| M-4 | **消息/会话读取无成员过滤（隐私泄露）** | `GetSessionMessagesQuery.cs:21-34`、`GetMessageQuery`、`SearchMessagesQuery`、`SessionsApi.cs:164-181` | 任意登录用户可读取任意会话全部聊天记录 |
| M-5 | **REST 发送消息无参与者校验**（仅查会话存在） | `SendMessageCommand.cs:198-203` | 可向任意私聊会话注入消息（Hub 路径有 `IsParticipant` 校验，两路径不一致） |
| M-6 | **撤回无发送者校验** | `Message.cs:210-220` | 任意用户可撤回他人 2 分钟内的消息 |
| M-7 | **文件 IDOR**：下载/预览/删除无所有权校验 | `FilesApi.cs:197-269` | 任意用户可下载/删除任意文件 |
| M-8 | JWT 私钥硬编码入库（同 C-1）；`ExpireSeconds: 7` | `appsettings.json:12-13` | — |
| M-9 | CORS 全开放 + AllowCredentials | `ServiceCollectionExtensions.cs:123-129` | 任意网站可携带凭证跨域调用 |
| M-10 | gRPC 跳过证书校验（无条件启用） | `ServiceCollectionExtensions.cs:95-100` | 中间人风险 |

### 5.3 中危问题（🟡）

| # | 问题 | 位置 |
|---|---|---|
| M-11 | Admin 判定可伪造（依赖 `X-User-Roles`） | `AuditApi.cs:80,128,160` |
| M-12 | 敏感信息回显：异常 Message 直接回传客户端 | `ExceptionHandlingMiddleware.cs:38-42`、`MessageHub.cs:702` |
| M-13 | XSS：消息/推文/评论内容无转义清理 | `TextContent.cs:7-15` |
| M-14 | 内容安全形同虚设：敏感词/图片审核仅打日志照常发布 | `CreateTweetCommand.cs:39-68` |
| M-15 | Tweet 可见性未过滤（Private/Followers 可被他人读取） | `GetUserTweetsQuery.cs:25-27` |
| M-16 | 转发越权：不校验源/目标会话成员 | `ForwardMessageCommand.cs:48-53` |

### 5.4 代码质量要点

- **🔴 领域事件处理器可能引发运行时 DI 崩溃**：6 个 Handler 注入 `IHubContext<Hub>`（未映射的泛型 Hub），应用只注册了 `IHubContext<MessageHub, IMessageClient>` → 每次发消息/撤回/上下线可能抛 DI 解析异常（`MessageSentEventHandler.cs` 等）。
- 数据库双重注册：`Program.cs:15`（PostgreSQL）+ `ServiceCollectionExtensions.cs:19-26`（SQL Server）。
- 框架版本混杂：Infrastructure 引用 `Microsoft.AspNetCore.Mvc.Core 2.3.11`、`SignalR 1.2.11` 等陈旧包。
- Redis Key 冲突：`RedisConnectionManager` 与 `UserStatusCacheService` 写同一组 Key 但 JSON Schema 不同，互相覆盖。
- 4 个 Redis 缓存服务注册但零使用（未读计数完全靠 EF 查询）。
- 分页 bug：`TotalCount = groups.Count()` 返回当前页条数而非总数（`GroupsApi.cs:548` 等 5 处）。
- SignalR `Clients.User` 默认读 `NameIdentifier` Claim，但 JWT 主 Claim 是 `sub` → 推送大概率落空。
- 错误码语义混乱：大量 API catch 后返回 HTTP 200 携带错误。
- `Message.FileSize` 用 `double` 存字节数，精度损失。
- `AllowedHosts: "*"`。

### 5.5 测试（全仓库唯一）

- **13 个测试文件、106 个用例**（NUnit），分布：MessageHubTests 19、FileStorageGrpcClientTests 14、MessageDeliveryServiceTests 11、领域测试 25、命令 30、查询 7。
- **明显缺口**：无任何 API 层测试；无群组权限/越权测试；无撤回权限、Session 越权、文件所有权、X-User-Id 伪造测试——**安全漏洞全部未被测试覆盖**。

### 5.6 文档

- `Tweet功能集成开发文档.md`、`list.md`、根 `Message.md`（实时聊天方案）文档质量较高，但**与代码严重背离**：文档承诺"服务内仅做资源所有权确认"，实际 Groups/Sessions/Files 所有权校验全部缺失；文档标注"已完成"的实体（MessageReadReceipt、OfflineMessage、UserOfflineStorage、MessageEncryption、重构后的 User）在代码中**均不存在**。

---

## 六、Markdown 博客项目群

**综合分：48 / 100 ｜ 完成度 38% ｜ 状态：🔴 不可用（启动即挂）**

### 6.1 完成度明细

| 模块 | 完成度 | 说明 |
|---|---|---|
| 文章 CRUD | 80% | 5 个端点齐全、所有权校验到位；但表映射错误（D-3）、无迁移、列表返回全量正文 |
| 历史版本 | 55% | 查询/删除有；**还原无 API**；历史端点匿名暴露 |
| 评论 | 50% | 增删改查齐；`ReviewAuth` 参数无效；图片无 EF 配置会致模型构建失败 |
| 审核流 | 0-10% | 无任何"审核/审批"概念落地；`MarkStatus` 枚举（17 种状态）从未使用 |
| 阅读历史（MarkHistory） | 0% | 纯僵尸代码 |
| 引用统计（MarkQuote） | 30% | 仅持久化展示，无计数 API |

### 6.2 阻断性问题（🔴 高）

| # | 问题 | 位置 | 影响 |
|---|---|---|---|
| D-1 | **私有文章/评论/历史匿名越权读取（IDOR）** | `MarkdownApis.cs:177-188,318-353,426-449` | 知道 GUID 即可匿名读取 `PrivateMark` 文章、评论及全部历史版本 |
| D-2 | **历史版本删除越权**：无所有权校验 | `MarkdownApis.cs:454-468` | 任意登录用户可删除任意文档历史 |
| D-3 | **表名/序列名复制粘贴错误**：`MarkDown` 被映射到表 `"NotFileGroup"`、序列 `"NotFileGroupGuid"`，与 FileDev 的 `NotFileGroup` 冲突 | `MarkDownEntityConfiguration.cs:8-9` | 共用库时互相覆盖数据 |
| D-4 | **当前用户解析失效**：先取 `ClaimTypes.Email`（邮箱字符串非 GUID），再取 `sub`（Identity 从未签发）；Identity 只发 `NameIdentifier` | `CurrentUserService.cs:14-24` | **`GetUserId()` 恒抛异常，所有需认证端点全部不可用** |
| D-5 | **ReviewImage 无 EF 配置** | `CreateMarkReviewRequest.cs:18` | 首次建模型即抛"ReviewImage 需要主键"异常 |
| D-6 | **无迁移、无 EnsureCreated** | `Program.cs` | `markdown_db` 建不出任何表，模块开箱即挂 |

### 6.3 中危问题（🟡）

| # | 问题 | 位置 |
|---|---|---|
| D-7 | JWT 校验参数过弱：`ValidateAudience=false`、`ValidateIssuerSigningKey=false` | `Program.cs:17-24` |
| D-8 | 分页 `take` 无上限、`Content` 无 MaxLength | `MarkdownApis.cs:164-172`、`CreateMarkdownRequest.cs:19` |
| D-9 | 幂等键服务端生成（每次新 Guid）、失败不清理 → 幂等形同虚设 | `MarkdownApis.cs:133-138` 等、`RequestManagement.cs:14-27` |
| D-10 | MD5 作内容哈希（碰撞风险） | `CreateMarkdownCommandHandler.cs:55-61` |
| D-11 | `CreateDb.cs:3` 明文口令 `<REDACTED>` | — |

### 6.4 僵尸代码检测

- `MarkHistory` 实体+配置完整但 DbContext 无 DbSet、仓储无方法、无迁移 → **完全未接线**。
- `MarkdownCreatedDomainEvent` 及其 Handler：事件从未被触发。
- `MarkReview` 5 个领域事件全部无发布方。
- `MarkStatus` 枚举全仓库 0 引用。
- `MarkDown.GetStatistics()`、`RestoreFromHistory()`、`OldMarkDown.ToCurrentFormat()`（注释"使用反射或其他方式"未实现）均无调用方。
- 2 处失效的 csproj 残留配置（`Compile Remove` 不存在的目录）。

### 6.5 评分说明

加分项：领域建模意识强、文章/评论所有权校验正确（`UserId == owner`）、DDD 分层完整。扣分项：D-3/D-4/D-5/D-6 四个阻断性缺陷、僵尸代码占比高、无 Query 层、无测试、无文档。

---

## 七、FileDev 文件存储项目群

**综合分：55 / 100 ｜ 完成度 55% ｜ 状态：🔴 不可用**

### 7.1 完成度明细

| 模块 | 完成度 | 说明 |
|---|---|---|
| 分片上传（init/upload/status/merge/cancel） | 80% | 流程完整；合并后文件**不可下载**（F-1）；分片无归属校验 |
| 断点续传 | 70% | 已实现；取消不清理临时文件 |
| 秒传/去重 | 40% | 仅查重返回 URI，无文件复用逻辑 |
| 流式上传 | 80% | 大小校验在全文读入内存之后 |
| 文件组 | 50% | 实体/Handler 齐全，但唯一入口 `FileStrongApi` **未注册到管道（死代码）** |
| 文件访问控制 | 15% | `FileAccessMiddleware` 未注册 DI 且 Attribute 零使用 |
| 图片专用（gRPC） | 90% | 含魔数校验 |
| 配额/过期清理/物理删除/HTTP 下载 | 0% | 配置存在但无任何调用方 |

### 7.2 阻断性问题（🔴 高）

| # | 问题 | 位置 | 影响 |
|---|---|---|---|
| F-1 | **分片合并后文件不可下载**：合并文件写到 `FileStorage/{fileKey}`，元数据 URI 却是 `/files/{userId}/{guid}{ext}`，路径错位 | `MergeChunksCommandHandler.cs:25-37` | HTTP 分片上传链路断裂 |
| F-2 | **gRPC 流程元数据不落库**：直接 `AddAsync` 无 SaveChanges，文件落盘但 DB 无记录 | `NotFileService.cs:25`、`FileStorageServiceGRPC.cs:256-259,676-680` | gRPC 上传的文件查不到 |
| F-3 | **HTTP 端点基本无身份认证**：全项目无任何 `[Authorize]`；分片写入/状态查询/取消可匿名调用；fileKey 格式可预测（`UserId_时间戳_文件名`） | `FileChunkApis.cs:68-96,98-121` | 任意人可写分片、枚举/取消任意上传任务 |
| F-4 | **gRPC 服务完全无认证，私有文件校验可伪造**：DownloadFile 的私有校验基于**客户端传入的 request.UserId** | `FileStorageServiceGRPC.cs:307-312,732-737` | 传任意 userId 即可绕过 |
| F-5 | **无大小限制 → 内存/磁盘 DoS**：分片/流式上传带 `DisableRequestSizeLimitAttribute` 绕过 Kestrel 1GB 限制；下载 `File.ReadAllBytes` 整文件入内存；gRPC 单消息上限 1GB | `FileChunkApis.cs:15,18`、`NotFileStorageService.cs:169` | 并发大文件打爆内存/磁盘 |
| F-6 | **配额与清理机制完全缺失**：`UserStorageQuota`、`GetExpiredRecordsAsync` 均无调用方，过期分片永不清理 | `NotFileStorageOptions.cs:43`、`FileChunkRepository.cs:44-53` | 磁盘无限增长 |
| F-7 | **分片越权/覆盖/孤儿文件**：合并不校验 `record.UserId == request.UserId`；分片序号校验在**写盘之后** | `MergeChunksCommandHandler.cs:16-21`、`UploadChunkCommandHandler.cs:16-23` | 任何登录用户可把他人分片合并进自己账号；越界索引批量制造孤儿文件 |

### 7.3 中危问题（🟡）

| # | 问题 | 位置 |
|---|---|---|
| F-8 | FileAccess 访问控制失效：Attribute 零处使用 + 中间件未注册 DI | `FileAccessMiddleware.cs`、`Program.cs:31` |
| F-9 | 文件类型白名单可绕过：仅校验扩展名不看 Content-Type/魔数；`/dedup/check` 匿名可查任意文件 MD5 并返回 URI（私有文件存在性泄露） | `FileCheckTypeMiddleware.cs:70`、`StreamUploadApis.cs:70-88` |
| F-10 | 密钥/口令明文（同 C-1/C-3）；RabbitMQ guest/guest | `appsettings.json:5,19-20` |
| F-11 | 服务端绝对路径泄露：存储层错误消息带服务器路径并原样返回客户端 | `NotFileStorageService.cs:78,220,270` |
| F-12 | 允许 .html/.htm/.svg 上传且 gRPC 对 .html 返回 text/html → **存储型 XSS 隐患** | `appsettings.json:22-29`、`FileStorageServiceGRPC.cs:977-978` |

### 7.4 代码质量要点

- `NotFileBuilder.WithFileType` 赋值但 `Build()` 不传 → **FileType 列不持久化**。
- 哈希算法不一致：SaveAsync 返回 SHA256，MergeChunksAsync 返回 MD5 → 秒传查重不可靠。
- 软删除不删物理文件（`FileDeletedEventHandler.cs:13-16` 空实现）。
- 幂等键错误：`CreateNotFileGroupCommand` 的 idProvider="Guid"、commandId=""，同一命令永远"重复"。
- 大量死代码：`FileStrongApi`（未映射）、`FileStorageGrpcService.cs`（Controller 未注册）、`FileStorageService` 包装类、`FileServicesDi`。
- 文件组创建的同级重名校验只查根级（固定传 null）。
- 拼写错误：`NotFileExeption`、`FileAccessAttrubit`。

---

## 八、Video 视频项目群

**综合分：52 / 100 ｜ 完成度 55% ｜ 状态：🔴 不可用**

### 8.1 完成度明细

| 模块 | 完成度 | 说明 |
|---|---|---|
| 视频查询/列表 | 70% | 无删除端点；返回全部含已删/私有视频 |
| 视频上传（HTTP+gRPC） | 75-80% | gRPC 无认证；全文件内存拷贝 |
| 视频更新 | 40% | 作者校验逻辑错误（永远 403）；仓储更新无 WHERE（**全表覆盖**） |
| 视频删除 | 30% | Command 存在但无端点暴露，不可达 |
| 审核（VideoReview） | 85% | 缓存命中时修改不落库 |
| 弹幕 | 55% | 持久化损坏（无 WHERE）；无长度/内容过滤 |
| 收藏 | 60% | 无归属校验；分页 Bug |
| 观看历史/统计 | 50-70% | 无限流、双重计数、Task.Run 逃逸作用域 |
| 点赞 | 60% | 缓存实体未跟踪不落库；评论点赞用户固定 Guid.Empty |
| 权限控制（私有/定时/授权） | 10% | **所有端点均未实施** |
| 设计文档 | 0% | `Notcomd.VideoServer.Options.cs` 是空壳且被 `<Compile Remove>` 排除编译 |

### 8.2 阻断性问题（🔴 高）

| # | 问题 | 位置 | 影响 |
|---|---|---|---|
| V-1 | **仓储无 WHERE 全表更新（数据破坏）**：所有 `ExecuteUpdateAsync` 均无过滤条件 | `VideoRepository.cs:63-68,70-75,78-83,97-106,127-141` | 任何一次点赞/弹幕/结束观看会把**全表所有视频**的字段覆盖为同一条数据 |
| V-2 | **视频访问权限完全缺失**：私有/定时视频可被任意访问；列表返回全部视频含已删记录 | `VideoStreamEndpoints.cs:39-40`、`VideoEndpoints.cs:55-94` | 私有/付费视频泄露 |
| V-3 | **上传身份客户端可控**：直接取客户端传入的任意 Guid 作为作者；FileDevClient 把 userId 拼进 query 传给无认证的文件服务端点 | `AddVideoEndpoints.cs:69`、`FileDevClient.cs:31` | 冒充任意用户上传任意文件 |
| V-4 | **流接口刷量 + 作用域逃逸**：每个 Range 请求 Task.Run fire-and-forget 递增计数（触发 V-1 全表覆盖）；Task.Run 内使用请求作用域 DbContext → ObjectDisposedException | `VideoStreamEndpoints.cs:70-81` | 一次观看多次计数 + 日志刷屏 |
| V-5 | **JWT 认证整体未接入**：无 AddAuthentication/UseAuthentication，`RequireAuthorization()` 空转 | `Program.cs:39` | 全部接口匿名可访问 |
| V-6 | **gRPC 上传无认证 + 内存 DoS**：500MB 文件整体读入 byte[] 再 ByteString 二次拷贝，峰值约 1.5GB | `AddVideoEndpoints.cs:48-54`、`UploadVideoViaGrpcCommandHandler.cs:27-47` | 并发上传打爆内存 |
| V-7 | **更新接口作者校验逻辑错误**：拿视频 GUID 对比用户 GUID 恒不等 → 永远 403；`VideoFileUri` 被赋值为封面 | `VideoEndpoints.cs:111-121` | 更新功能不可用 |

### 8.3 中危问题（🟡）

| # | 问题 | 位置 |
|---|---|---|
| V-8 | **缓存命中时写操作不落库**：从 Redis 反序列化实体修改，未被 DbContext 跟踪 → 点赞/评论/更新静默丢失 | `LikeVideoCommandHandler.cs:24-54` 等 5 处 |
| V-9 | 观看历史无校验无限流：任意 UserGuid/VideoGuid 可写，可无限刷库 | `RecordVideoWatchCommandHandler.cs:18-40` |
| V-10 | 收藏无归属校验：任意用户可改任意收藏夹 | `AddVideoToCollectionCommandHandler.cs:20-33` |
| V-11 | 幂等机制形同虚设：key 每次现场生成 | `IdempotencyBehavior.cs:31` |
| V-12 | 弹幕/评论 XSS：弹幕文本无长度/清洗；富文本正则可被 `onclick =` 绕过；`a` 标签允许任意 `javascript:` href | `VideoBarrageEndpoints.cs:32-88`、`ReviewContent.cs:150-167` |

### 8.4 代码质量要点

- `Video.Domain.csproj:17` 的 `<Compile Remove>` 把设计文档文件排除出编译。
- 双次 SaveChanges（handler 内 + TransactionBehavior）。
- fire-and-forget：`VideoService.cs` 多处 `_ = SetVideoListAsync(...)` 异常被吞。
- 分页 bug：`VideoCollectionRepository.cs:53` `Skip(page).Take(pageSize)` 算法错误。
- 拼写错误：`VideioHositoli.cs`、`SavaChangesAsync`。
- csproj 硬编码本机绝对路径引用 `C:\Program Files\dotnet\...9.0.13\Microsoft.AspNetCore.Mvc.Core.dll`（目标框架却是 net10.0）。
- **无测试、无文档**。

---

## 九、网关与宿主（NotBlog_Yarp / NotBlog.AppHost）

### 9.1 网关 NotBlog_Yarp —— **65 / 100**

**架构评价**：路由/认证/Header 注入/权限中间件骨架完整，代码组织清晰，防伪造 Header 处理正确（每请求先删除 `X-User-Id/X-User-Roles/X-Data-Scope` 再注入，`UserContextTransform.cs:34-86`）。

**主要问题**：

| # | 级别 | 问题 | 位置 |
|---|---|---|---|
| G-1 | 🔴高 | **权限校验 HTTP 端点与 Identity 实际路由不匹配**：网关调 `/api/permission/check` 等，Identity 未实现 → 生产模式所有已映射路由恒 403 | `HttpPermissionServiceClient.cs:76,121,168` vs `PermissionApi.cs:14-42` |
| G-2 | 🔴高 | **未映射路径 fail-open 放行**：comments/reports/audit/friends/groups/files/messages 及 **全部 7 个 video 路由**无权限映射、无认证要求 | `PermissionFilterMiddleware.cs:108-113`、`appsettings.json:29-45` |
| G-3 | 🔴高 | 下游信任 `X-User-Id` 头且无 JWT 兜底（Message 的 UserContextMiddleware 在 UseAuthentication 之前运行） | `Message.Web.API\Middleware\UserContextMiddleware.cs:11-19` |
| G-4 | 🟡中 | 集群地址硬编码 `localhost:909x/9005`，未接入 Aspire 服务发现 | `appsettings.json:210` |
| G-5 | 🟡中 | 密钥硬编码（同 C-1）；`jwtSection["SecretKey"]!` 未判空 | `Program.cs:14` |
| G-6 | 🟡中 | 网关权限检查 HTTP 调用无弹性策略，Identity 抖动 → 全站 403 连锁 | — |
| G-7 | 🟡中 | `/MessageHub`（SignalR）未入网关路由，客户端直连 Message 端口 | — |

### 9.2 宿主 NotBlog.AppHost —— **40 / 100**

| # | 级别 | 问题 | 位置 |
|---|---|---|---|
| A-1 | 🔴高 | **RELEASE 分支编译错误**：`#else` 分支后引用的 `identity/notfile/...` 变量未定义；且 `#else` 与 `#endif` 后重复 AddProject 同名资源 | `AppHost.cs:22-69` |
| A-2 | 🔴高 | **RabbitMQ 资源被注释**，但 Identity 无条件 `AddRabbitMQClient("EventBus")` → Aspire 下 Identity 启动即失败 | `AppHost.cs:20-21` |
| A-3 | 🔴高 | **连接串名大面积不匹配**：Message 用 `"PostgresSQL"` vs AppHost 给 `"MessagePostgres"`；Video 用 `"CacheMemory"` vs AppHost 给 `"Redis"`；FileDev 从不读取 `NotFilePostgres` | `Message.Web.API\Program.cs:15`、`Video.Web.API\Program.cs:19` |
| A-4 | 🟡中 | 网关未引用 ServiceDefaults，未接入可观测性/健康检查 | — |
| A-5 | 🟡中 | `CreateDb.cs` 创建 `BlogFileDev`，FileDev 实际连接 `BlogFileStrong`，脚本已失效 | `CreateDb.cs:3,7` |

---

## 十、跨项目重大问题清单（安全红线）

按影响面排序，**上线前必须全部处理**：

### 🔴 P0 —— 上线阻断（安全）

| 编号 | 问题 | 涉及项目 |
|---|---|---|
| P0-1 | 认证/授权中间件缺失或可绕过：Message 信任 `X-User-Id` 头、Identity 无 UseAuthentication、Video/Markdown 认证链断裂、FileDev 端点无认证 | Message / Identity / Video / Markdown / FileDev |
| P0-2 | JWT 私钥硬编码且全站共用一把密钥；GitHub ClientSecret、SMTP/DB 密码明文入库 | 全站 |
| P0-3 | 群组/Session/文件/会话读取类 IDOR：所有权校验普遍缺失 | Message / FileDev / Markdown / Video |
| P0-4 | OAuth 登录 CSRF（无 state 校验） | Identity |
| P0-5 | 仓储 ExecuteUpdate 无 WHERE 导致全表数据覆盖 | Video |
| P0-6 | 网关节点的权限校验契约断裂 → 生产模式全站 403 | NotBlog_Yarp / Identity |

### 🔴 P0 —— 上线阻断（功能/可靠性）

| 编号 | 问题 | 涉及项目 |
|---|---|---|
| P0-7 | Markdown 表映射错误（映射到 NotFileGroup）、无迁移、CurrentUser 解析恒失败 | Markdown |
| P0-8 | FileDev 分片合并后不可下载；gRPC 元数据不落库 | FileDev |
| P0-9 | Identity RefreshToken 自相矛盾（100% 失败）；ExpireSeconds=7 | Identity |
| P0-10 | EventBus Outbox 未启用、失败消息永不重试、发布通道断线不重建 | EventBus |
| P0-11 | AppHost RELEASE 编译失败；RabbitMQ 缺失；连接串命名错位 → Aspire 模式无法启动 | AppHost |
| P0-12 | Message 领域事件处理器 DI 崩溃风险（IHubContext<Hub> 未注册） | Message |

### 🟡 P1 —— 重要（发布前应处理）

1. 缓存与 DB 不一致：Video 缓存实体写操作不落库、Markdown/Message 幂等机制形同虚设。
2. 敏感信息泄露：验证码/密码入日志、异常 Message 回传、绝对路径泄露、允许 .html/.svg 上传（存储型 XSS）。
3. CORS 全开放、gRPC 跳过证书校验。
4. XSS：Message/Video/Markdown 内容均无转义清理。
5. 资源耗尽：上传/下载无大小限制、无配额、无过期清理、弹幕/评论无长度限制。
6. 网关 fail-open 策略 + 权限映射缺失（video 7 路由、message 7 路由）。

### 🟢 P2 —— 一般（质量改进）

1. 约 20+ 处 `NotImplementedException` 半成品清理或实现。
2. 大量僵尸代码（未映射端点、未使用实体、未发布事件）。
3. 拼写错误（SavaChangesAsync、NotFileExeption、VideioHositoli 等）与命名混乱。
4. 双重 SaveChanges、事务语义稀释、连接串键名三套并存。
5. 无测试的 5 大模块补测试；134 个编译警告清零。

---

## 十一、改进路线图

### 阶段一：安全红线修复（优先级最高）

| 任务 | 详情 | 涉及 |
|---|---|---|
| 1.1 认证体系统一 | ①启用 Identity 的 UseAuthentication/UseAuthorization 并为全部管理端点加授权；②Message 删除对 X-User-Id/X-User-Roles 的无条件信任，改为 JWT 为主、网关头为辅助（或校验来源）；③Video/Markdown 接入与 Identity 一致的 JWT 校验；④FileDev 全部端点加 [Authorize] | 全站 |
| 1.2 密钥治理 | 所有密钥/口令移入环境变量/用户机密/密钥管理；轮换当前 JWT 私钥 | 全站 |
| 1.3 越权修复 | 群组/Session 调用 `Group.HasPermission`/`ChatSession.IsParticipant`；文件/会话查询加所有权过滤；撤回加发送者校验 | Message |
| 1.4 数据安全 | Video 仓储全部 ExecuteUpdate 补 WHERE；修复更新作者校验 | Video |
| 1.5 OAuth 安全 | state 参数生成+存储+回调校验；OAuth 用户落库；重定向 URI 白名单 | Identity |

### 阶段二：链路打通（功能性）

| 任务 | 详情 | 涉及 |
|---|---|---|
| 2.1 Markdown 修复 | 表映射改回 MarkDown；补迁移/EnsureCreated；CurrentUserService 改读 NameIdentifier；ReviewImage 补 EF 配置 | Markdown |
| 2.2 FileDev 修复 | 合并路径对齐；gRPC SaveChanges；秒传复用；文件组 API 注册 | FileDev |
| 2.3 Identity 修复 | RefreshToken 重写；ExpireSeconds 改 7200；邮件配置节对齐；补权限检查端点（或网关移除 HTTP 校验） | Identity / Yarp |
| 2.4 EventBus 启用 | 失败消息重试；通道断线重建；至少一个服务启用 Outbox | EventBus |
| 2.5 AppHost 修复 | 修复 RELEASE 分支；恢复 RabbitMQ 注册；统一连接串命名 | AppHost |
| 2.6 Message 修复 | 修复领域事件 Handler DI；解决数据库双注册；激活离线消息链路（SetReceiver） | Message |

### 阶段三：功能补全（差异化开发）

| 模块 | 优先补充（按序） |
|---|---|
| Identity | 用户信息查询、短信验证码、密码修改（带旧密码验证）、邮箱确认、Token 吊销接入 Redis |
| Message | 群组申请入群、消息加密落地（Encrypt 已有方法无调用方）、敏感词/图片审核真实接入、离线消息、未读计数 |
| FileDev | HTTP 下载/预览、配额强制、过期分片清理、物理删除、文件类型魔数校验 |
| Video | 删除端点、私有/定时视频权限、防盗链、敏感词、观看去重、HTTP 上传链路 |
| Markdown | Query 层、审核流（MarkStatus）、阅读历史接线、子评论 API、历史还原 API |

### 阶段四：质量提升与发布准备

1. **测试补强**：为 Identity/Markdown/FileDev/Video 建立测试项目（参考 Message.Tests 106 用例模式）；重点补授权/越权负向用例。
2. **代码清理**：清除 NotImplemented 半成品与僵尸代码；修复 134 个编译警告。
3. **文档对齐**：更新 Tweet 文档与 Message.md 使其与实现一致；为各模块补 README/迁移说明；消除 PERMISSION_DESIGN.md 与实现偏差。
4. **性能与容量**：上传/下载大小限制、速率限制、缓存失效策略（Video 列表缓存）、IMAP 附件大小限制、索引/分页优化。
5. **可观测性**：网关接入 ServiceDefaults；各服务健康检查接入 AppHost。

---

## 十二、附录：审查方法与数据

### 12.1 审查数据

| 指标 | 数值 |
|---|---|
| 解决方案项目数 | 25（22 编译 + 1 测试 + 2 虚拟分组） |
| 构建结果 | 0 错误 / 134 警告 |
| 测试项目 | 1（Message.Tests，106 用例） |
| 业务模块测试覆盖 | 5/6 模块零测试 |
| 硬编码密钥/口令 | ≥8 处（JWT×5 处同值、GitHub Secret、SMTP、DB×2、RabbitMQ） |
| 僵尸代码/半成品 | 估计 40+ 处 |

### 12.2 审查范围

- **公共组件（8 个）**：JWToken、EventBus、NotEmail、CacheMemory、DomainCommons、DomainInfrastructure、CommonsInitializer、ServiceDefaults —— 全部源文件逐文件精读。
- **Identity（3 项目）**：实体、仓储、服务、API、命令、迁移、配置全量；含 PERMISSION_DESIGN.md 一致性核对。
- **Message（4 项目）**：Domain/Infrastructure/Web.API/Test 全量；含 Tweet 集成文档、list.md 一致性核对。
- **Markdown / FileDev / Video（各 3 项目）**：全量源码 + 配置。
- **网关/宿主**：Yarp 路由/中间件/权限/Transform；AppHost 资源注册与连接串核对；CreateDb.cs。
- **集成验证**：构建验证 + 跨项目契约核对（JWT Claim、连接串名、权限端点、路由前缀、网关头名）。

### 12.3 免责声明

- 本报告基于静态代码审查，未进行运行时/渗透测试验证；部分"必然失败"结论来源于代码路径的确定性推导（如 CurrentUser 解析、仓储无 WHERE），但运行时行为请以实际验证为准。
- 评分为主观量化，用于横向对比优先级，不构成绝对度量。
