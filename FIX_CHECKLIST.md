# NotBlog 修复清单（FIX_CHECKLIST）

> 生成日期：2026-08-02 ｜ 依据：[PROJECT_ASSESSMENT_REPORT.md](./PROJECT_ASSESSMENT_REPORT.md)
> 用途：将评估报告中的全部问题点转化为可执行、可跟踪、可验收的修复任务清单

---

## 目录

1. [文档说明](#文档说明)
2. [类别一：安全漏洞（S）](#类别一安全漏洞s)
3. [类别二：功能缺陷（F）](#类别二功能缺陷f)
4. [类别三：性能与稳定性（P）](#类别三性能与稳定性p)
5. [类别四：代码质量与架构（Q）](#类别四代码质量与架构q)
6. [类别五：测试与文档（T）](#类别五测试与文档t)
7. [修复路线图](#修复路线图)
8. [P0 红线对照表](#p0-红线对照表)
9. [编号索引](#编号索引)

---

## 文档说明

### 优先级定义

| 级别 | 含义 | 触发条件 |
|---|---|---|
| **P0** | 上线阻断 | 安全漏洞、数据破坏、启动失败、核心链路断裂，必须最先修复 |
| **P1** | 发布前应处理 | 重要安全隐患、影响主流程的功能缺失、文档与实现背离 |
| **P2** | 一般改进 | 代码质量、性能优化、清理工作 |

### 时间约定

- 单位为**人天**，按单人全职开发估算；多人协作可除以人数。
- 估算包含编码 + 自测，不含需求评审与跨模块联调。
- 各阶段合计与排期见"修复路线图"。

### 验收标准约定

- 每项验收标准均为**可客观验证**的条件，验收时逐条打勾；全部满足才视为该项完成。
- 验收方式示例：单元/集成测试通过、`grep` 扫描无命中、构建通过、运行时行为验证。

### 覆盖率对照

本清单覆盖评估报告全部问题编号：C-1~C-4、E-1~E-16、M-1~M-16、D-1~D-11、F-1~F-12、V-1~V-12、G-1~G-7、A-1~A-5，以及 P0-1~P0-12 与 P1/P2 汇总项。文末附"编号索引"逐项对照。

---

## 类别一：安全漏洞（S）

### S-01 【P0】JWT 密钥治理：移除全部硬编码密钥并支持轮换
- **模块**：全站（Identity/Message/FileDev/Video/Yarp）
- **报告编号**：C-1、E-3、M-8、F-10、G-5、D-11
- **问题**：JWT 签名私钥 `<REDACTED>` 硬编码于 5 个服务 appsettings.json 且全站共用一把；GitHub ClientSecret、SMTP 密码、Postgres 密码、RabbitMQ guest/guest 全部明文入库。
- **修复措施**：
  1. 全部 appsettings.json 移除密钥/密码，改用环境变量 / dotnet user-secrets / 密钥管理服务（每环境一套）。
  2. 为每个服务分配独立 JWT 签名密钥（或统一由密钥管理下发），支持版本化轮换。
  3. 更新 `.gitignore` 覆盖生产 appsettings 与 `.env`；清理历史提交中的密钥（`git filter-repo` 或强制轮换）。
  4. `CreateDb.cs` 口令一并外置。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] `grep -r "JWT私钥|GitHubClientSecret|SmtpPassword|DbPassword|Db口令" f:\NotBlog --exclude-dir=bin --exclude-dir=obj` 无命中
  - [ ] 各服务在仅有环境变量（无 appsettings 敏感项）下可启动并完成登录
  - [ ] `.gitignore` 已含 `*.env` 与敏感 appsettings
  - [ ] 轮换后旧 token 失效，新 token 正常

### S-02 【P0】Message 认证加固：消除 X-User-Id/X-User-Roles 伪造
- **模块**：Message.Web.API
- **报告编号**：M-1、M-11、G-3
- **问题**：`UserContextMiddleware` 无条件信任客户端 `X-User-Id`/`X-User-Roles` 头；全项目无任何 `[Authorize]`；Admin 判定依赖可伪造头。绕过网关直连端口即可完全冒充任意用户。
- **修复措施**：
  1. `UserContextMiddleware` 仅在 `HttpContext.User.Identity.IsAuthenticated` 时解析身份；`X-User-Id` 仅在已验证 JWT 的 `sub`/`NameIdentifier` Claim 与之一致时使用。
  2. 全部 API 端点与 MessageHub 方法加 `[Authorize]`，未认证一律 401。
  3. Admin/审核端点改为从 JWT Claim 判定，删除对 `X-User-Roles` 的依赖。
  4. `UseUserContext` 调整到 `UseAuthentication` 之后执行。
  5. 补负向测试：伪造头时身份不得生效。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] 无 token 请求受保护端点 → 401
  - [ ] 携带 `X-User-Id: <他人GUID>` 无 token → 401，无法冒充
  - [ ] 伪造 `X-User-Roles: Admin` 无法获得管理员权限
  - [ ] 所有 Message API/Hub 方法均有认证保护

### S-03 【P0】Identity 认证/授权中间件启用
- **模块**：Identity.Web.API / CommonsInitializer
- **报告编号**：E-1
- **问题**：`UseNotBlogPipeline()` 为空实现，Identity 未调用 `UseAuthentication/UseAuthorization`：角色/权限管理端点完全公开，而 `RequireAuthorization` 端点恒 401。
- **修复措施**：
  1. 在 `ApplicationBuilderExtension.UseNotBlogPipeline()` 实现标准管线：`UseCors → UseAuthentication → UseAuthorization`。
  2. Role/Permission/RoleGroup 管理端点加 `[Authorize]` + 角色策略（Root/Admin）。
  3. 统一 userId Claim 为 `NameIdentifier`（配合 S-12），使受保护端点可用。
  4. 全仓验证 Identity 管线已生效。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 匿名调用 `POST /api/identity/role` → 401/403
  - [ ] 非 Root/Admin 调用权限管理端点被拒
  - [ ] 带合法 token 调用 OAuth 绑定/头像上传端点成功
  - [ ] `UseNotBlogPipeline` 不再是空实现

### S-04 【P0】Message 群组命令权限校验
- **模块**：Message.Web.API（Commands/Groups）
- **报告编号**：M-2
- **问题**：11 个群组命令零权限校验，`Group.HasPermission` 权限模型从未被调用，任意用户可解散任意群、踢人、改管理员、转让群主。
- **修复措施**：
  1. 各群组命令 Handler 注入 `ICurrentUserService`，执行前调用 `Group.HasPermission(调用者, GroupPermission.Xxx)`。
  2. 权限规则：解散/转让仅 Owner；SetAdmin/RemoveMember/Ban 需 Owner 或 Admin；普通成员仅可退出。
  3. 为 DismissGroup、RemoveGroupMember、SetAdmin、TransferOwnership、UpdateGroupInfo、AddGroupMember 补齐校验。
  4. 补命令层负向测试（普通成员越权被拒）。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] 普通成员解散他人群 → 拒绝
  - [ ] 普通成员将他人设为管理员 → 拒绝
  - [ ] 非 Owner 转让群主 → 拒绝
  - [ ] Owner/Admin 合法操作不受影响
  - [ ] `HasPermission` 在全部 11 个群组命令中被调用

### S-05 【P0】Message Session/消息/文件 IDOR 修复
- **模块**：Message.Web.API
- **报告编号**：M-3、M-4、M-5、M-6、M-7、M-16
- **问题**：Session 命令无成员校验；会话/消息查询无成员过滤（可读任意会话记录）；REST 发送消息无参与者校验；撤回无发送者校验；文件下载/预览/删除无所有权校验；转发不校验成员资格。
- **修复措施**：
  1. Session 命令（置顶/静音/解散/增删参与者）执行前校验调用者 `IsParticipant`。
  2. 查询（GetSessionMessages/GetMessage/SearchMessages/GetSession/GetSessionParticipants）按调用者 id 过滤：仅返回调用者参与的会话。
  3. `SendMessageCommand` 复用 Hub 的 `IsParticipant` 校验，REST 与 Hub 行为一致。
  4. `Message.Recall` 增加 `recalledBy == SenderId` 校验。
  5. 文件下载/预览/删除校验调用者为上传者或会话成员。
  6. `ForwardMessageCommand` 校验调用者在源/目标会话均为成员。
  7. 补 IDOR 负向测试。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 非成员读取他人会话消息 → 404/403
  - [ ] 非成员向他人私聊会话发送消息 → 拒绝
  - [ ] 非发送者撤回他人消息 → 拒绝
  - [ ] 非所有者下载/删除他人文件 → 拒绝
  - [ ] 非成员转发消息 → 拒绝

### S-06 【P0】Video 数据安全：仓储补 WHERE + 缓存写落库
- **模块**：Video.Infrastructure / Video.Web.API
- **报告编号**：V-1、V-7、V-8
- **问题**：`VideoRepository.UpdateByQuote/Control/TimeSpace/Video` 及 DeleteByVideoControl 系列 `ExecuteUpdateAsync` 无过滤条件，一次点赞/弹幕/观看覆盖全表所有视频；更新接口作者校验错误（VideoGuid 对比 UserGuid 恒 403）；缓存反序列化实体未被 DbContext 跟踪导致写操作静默丢失。
- **修复措施**：
  1. 所有 `ExecuteUpdateAsync` 增加 `.Where(en => en.Id == xxx)` 或按 VideoGuid 过滤。
  2. 修复 `VideoEndpoints` 更新逻辑：校验 `Affiliated` 集合包含调用者；修正 VideoFileUri/VideoCover 参数错赋。
  3. 缓存命中时写操作改为从仓储按主键加载再修改，确保落库。
  4. 为各 Update 方法补按 id 的仓储方法及单测。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] 点赞视频 A 后视频 B 的 VideoQuote 不变（单测断言）
  - [ ] 更新接口：作者成功、非作者 403
  - [ ] VideoFileUri 与封面赋值正确
  - [ ] 缓存命中时点赞/评论/更新后 DB 可查到变更

### S-07 【P0】Video 认证接入与访问控制
- **模块**：Video.Web.API
- **报告编号**：V-2、V-5、V-3、V-10
- **问题**：JWT 认证整体未接入，`RequireAuthorization` 空转；私有/定时视频任意访问，列表返回全部含已删记录；上传身份由客户端传入 Guid 决定；收藏无归属校验。
- **修复措施**：
  1. `Program.cs` 接入 `AddAuthentication(JwtBearer)` + `UseAuthentication` + `UseAuthorization`，配置与 Identity 一致。
  2. 详情/播放/流接口实施 `AuthorVideo`（Private/Protected）与 `VideoProtectedTime` 校验；列表过滤已删除、非公开、未到期视频。
  3. 上传/更新/删除/点赞/收藏全部使用服务端解析的当前用户 id，禁止信任客户端传入的 AffiliatedAuthorizes/UserGuid。
  4. 收藏命令校验收藏夹归属。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 匿名访问任一 Video 端点 → 401
  - [ ] 私有/定时视频仅授权用户可访问，列表不含未公开视频
  - [ ] 客户端伪造 AffiliatedUserGuid 无效
  - [ ] 任意用户无法向他人收藏夹增删

### S-08 【P0】FileDev 认证与授权
- **模块**：FileDev.Web.API
- **报告编号**：F-3、F-4、F-7、F-8
- **问题**：HTTP 端点全无认证（分片写入/状态/取消可匿名调用，fileKey 可预测）；gRPC 服务无认证且私有文件校验基于客户端传入 userId（可伪造）；分片合并不校验归属；FileAccessAttribute/中间件失效。
- **修复措施**：
  1. 全部 HTTP API 端点加 `[Authorize]`。
  2. gRPC 服务从 `context.AuthContext`/请求元数据校验 JWT，私有文件校验改用服务端解析的调用者 id，删除对 request.UserId 的信任。
  3. `MergeChunksCommandHandler` 校验 `record.UserId == 调用者`；分片归属校验前置到写盘之前。
  4. 注册 `FileAccessMiddleware` 到 DI 并应用到下载/读取端点。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 匿名调用 /chunk/init|upload|status|cancel、/dedup/check、/stream/upload → 401
  - [ ] gRPC 无 token → 认证失败
  - [ ] 伪造 request.UserId 下载他人私有文件 → 拒绝
  - [ ] 用户 B 无法把用户 A 的分片合并到自己账号

### S-09 【P0】FileDev 资源限制与配额
- **模块**：FileDev.Web.API / FileDev.Infrastructure
- **报告编号**：F-5、F-6
- **问题**：上传带 `DisableRequestSizeLimitAttribute` 绕过 Kestrel 1GB 限制；下载整文件读入内存；gRPC 单消息上限 1GB；`UserStorageQuota`/`GetExpiredRecordsAsync` 无调用方，过期分片永不清理。
- **修复措施**：
  1. 移除 `DisableRequestSizeLimitAttribute`，改用 `[RequestSizeLimit]` + Kestrel `MaxRequestBodySize`。
  2. 流式上传先校验声明大小再读流；下载流式输出（FileStream + Results.File）。
  3. gRPC `MaxReceiveMessageSize` 下调或实现流式上传。
  4. 实现配额强制（写入前查询用量，超 `UserStorageQuota` 拒绝）。
  5. 实现过期分片清理后台任务；取消上传时清理临时分片。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 超限请求返回 413
  - [ ] 下载大文件内存平稳（profile 验证）
  - [ ] 超配额上传被拒
  - [ ] 过期/已取消上传的临时分片被清理

### S-10 【P0】Markdown 越权修复 + JWT 校验
- **模块**：Markdown.Web.API
- **报告编号**：D-1、D-2、D-7、D-4
- **问题**：私有文章/评论/历史可匿名越权读取；历史版本删除无所有权校验；JWT 校验参数过弱（ValidateAudience=false、ValidateIssuerSigningKey=false）；CurrentUserService 解析恒失败。
- **修复措施**：
  1. 私有文档读取/评论/历史接口增加所有者校验。
  2. 历史版本删除 Handler 增加 `OldMarkDown.MarkUserGuid == 调用者`。
  3. JWT 配置开启 ValidateAudience/ValidateIssuerSigningKey，与 Identity 一致。
  4. `CurrentUserService.GetUserId()` 读取 `NameIdentifier`（兼容 `sub`）。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] 匿名读取他人私有文章 → 404/403
  - [ ] 非所有者删除他人历史版本 → 拒绝
  - [ ] 非法 token（错 Audience/Issuer）被拒
  - [ ] 带合法 token 调用受保护 Markdown 端点成功

### S-11 【P0】OAuth CSRF 与用户落库
- **模块**：Identity.Web.API / Identity.Infrastructure
- **报告编号**：E-2、E-7
- **问题**：OAuth 回调无 state 校验（登录 CSRF）；通用 OAuth 创建的用户不落库；GitHub ID 为数字无法 Guid.Parse。
- **修复措施**：
  1. 生成 state 存 Redis（`oauth:state:{state}`，TTL 10 分钟），回调校验并删除。
  2. 重定向 URI 白名单校验。
  3. `OAuthService.AddOneByUserAsync` 后 `SaveChangesAsync`。
  4. Provider 用户 id 改字符串主键/复合唯一键，允许数字 id。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] 回调缺 state 或不匹配 → 拒绝
  - [ ] 重定向 URI 不在白名单 → 拒绝
  - [ ] Google/Microsoft 流程后 DB 可查到新用户
  - [ ] GitHub 登录不抛 FormatException

### S-12 【P0】Identity 令牌体系修复（RefreshToken/吊销/过期配置）
- **模块**：JWToken / Identity.Web.API
- **报告编号**：E-5、E-6、E-9
- **问题**：RefreshToken 生成与刷新逻辑自相矛盾（100% 失败）、无过期/存储/单次使用；吊销黑名单在进程内且未接入 JwtBearer；`ExpireSeconds: 7` 致 token 秒过期。
- **修复措施**：
  1. 重写 RefreshToken：服务端存储（DB/Redis），含过期、单次使用（刷新即轮换）、绑定用户。
  2. JwtBearer 挂接黑名单校验（Redis）。
  3. `ExpireSeconds` 改为合理值（7200），`RefreshTokenExpireSeconds` 实际生效。
  4. 修复 `FromUnixTimeSeconds(ExpireSeconds)` 误用。
- **预计时间**：2.5 人天
- **验收标准**：
  - [ ] 登录后 /refresh 返回新 access token（不再抛"无效格式"）
  - [ ] 同一 refresh token 二次使用 → 拒绝
  - [ ] 吊销后 token 立即失效
  - [ ] access token 有效期为 2 小时

### S-13 【P1】登录安全加固
- **模块**：Identity.Web.API
- **报告编号**：E-12、E-16、E-15
- **问题**：无 IP 级限流、账号可枚举；`AccessFaildCount++` 非原子可并发绕过；PBKDF2 10 万次迭代低于 OWASP 建议 60 万；Claim 命名不一致。
- **修复措施**：
  1. 登录端点 IP 级限流（令牌桶/固定窗口）。
  2. `AccessFaildCount` 原子化（Redis INCR 或乐观锁）；登录失败响应不区分邮箱是否存在。
  3. PBKDF2 迭代提升至 600K，带旧哈希升级路径。
  4. 统一 userId Claim 为 `NameIdentifier`。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] 同 IP 短时大量失败登录 → 429
  - [ ] 并发失败计数不丢失（压测）
  - [ ] 登录失败响应不区分邮箱是否存在
  - [ ] 新注册用户迭代 600K，旧用户下次登录自动升级

### S-14 【P1】幂等机制修复（全站）
- **模块**：Identity / Message / Markdown / FileDev / Video
- **报告编号**：D-9、V-11、Identity 幂等失效、FileDev 幂等键错误
- **问题**：`IdentifiedCommand.Id` 每次服务端 `Guid.CreateVersion7()` 现场生成，重复请求永不命中；FileDev idProvider 错误致同一命令永远"重复"；失败后幂等记录残留。
- **修复措施**：
  1. 幂等键改由客户端显式传入（`X-Idempotency-Key`），服务端读取校验。
  2. 幂等表唯一约束 + "插入或忽略"，失败时回滚幂等记录。
  3. 修正 FileDev IdentifiedCommandHandler 的 idProvider/commandId。
  4. 移除服务端自动生成幂等键。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 同幂等键重复提交 → 返回首次结果不重复执行
  - [ ] 不同幂等键正常执行
  - [ ] 命令失败后重试可成功（幂等记录被清理）
  - [ ] FileDev 文件组创建不再恒报"重复"

### S-15 【P1】CORS 收紧
- **模块**：Identity.Web.API / Message.Web.API
- **报告编号**：E-14、M-9
- **问题**：`AllowAll` 策略（AllowAnyOrigin + AllowCredentials）；Message `IsOriginAllowed(_ => true)`。
- **修复措施**：
  1. 改为白名单来源配置（CorsSettings/配置节）。
  2. 禁止 AllowAnyOrigin 与 AllowCredentials 共存。
  3. Identity 接入 `UseCors` 使策略生效。
- **预计时间**：0.5 人天
- **验收标准**：
  - [ ] 白名单外 origin 带凭证请求被拒
  - [ ] 白名单 origin 正常
  - [ ] 配置中无 AllowAnyOrigin 与 AllowCredentials 共存

### S-16 【P1】gRPC 证书校验与敏感信息治理
- **模块**：Identity / Message / FileDev / Video
- **报告编号**：C-4、E-13、M-10、E-11、M-12、F-11
- **问题**：gRPC 客户端无条件跳过证书校验；验证码/密码写入日志；异常 Message 直接回传客户端；存储错误消息带服务器绝对路径。
- **修复措施**：
  1. `DangerousAcceptAnyServerCertificateValidator` 仅 DEBUG/开发配置启用。
  2. 移除验证码/命令对象（含明文密码）日志；邮件日志脱敏。
  3. 全局异常中间件统一返回脱敏错误码。
  4. `NotFileStorageService` 异常剥离绝对路径。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] Production 构建下 gRPC 正常校验证书
  - [ ] 日志无验证码、明文密码、邮件全文
  - [ ] 客户端异常响应不含内部路径/堆栈
  - [ ] 文件存储错误无绝对路径

### S-17 【P1】XSS 与内容安全
- **模块**：Message / Video / FileDev / Markdown
- **报告编号**：M-13、M-14、M-15、V-12、F-12
- **问题**：消息/推文/评论/弹幕内容无转义清理；富文本正则可被 `onclick =`/`javascript:` 绕过；敏感词/图片审核仅打日志照常发布；Tweet 可见性未过滤；FileDev 允许 .html/.svg 上传且返回 text/html。
- **修复措施**：
  1. 引入统一 HTML 清洗库（Ganss.XSS/HtmlSanitizer），应用于全部用户生成内容。
  2. 弹幕/评论加长度上限与空值校验。
  3. 敏感词过滤接入真实实现（不再恒 Passed）；图片审核失败策略明确（拒绝或人工审核）。
  4. Tweet 查询按 Visibility 过滤。
  5. FileDev 白名单移除 .html/.htm/.svg（或强制 attachment 下载头）。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 注入 `<script>`/`onclick`/`javascript:` 内容存储后为净化结果
  - [ ] 超长弹幕/评论被拒
  - [ ] 命中敏感词 Tweet 被拒或进入审核
  - [ ] 他人 Private/Followers 推文不可见
  - [ ] .html/.svg 不可被浏览器直接渲染

### S-18 【P1】Video 观看/点赞防刷
- **模块**：Video.Web.API
- **报告编号**：V-4、V-9
- **问题**：流接口每个 Range 请求 fire-and-forget 递增计数（+结束观看=双重计数）；Task.Run 内使用请求作用域 DbContext；观看历史无校验无限流。
- **修复措施**：
  1. 移除 Task.Run，观看计数改请求内同步执行。
  2. 观看计数 Redis 去重（用户+视频+5 分钟窗口）。
  3. 观看历史校验视频存在性与用户身份；进行中记录不重复创建。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 连续 Range 请求 5 分钟只计 1 次观看
  - [ ] 无 ObjectDisposedException 日志
  - [ ] 伪造 UserGuid 上报被拒
  - [ ] 观看历史无刷库

### S-19 【P1】EventBus 可靠性
- **模块**：EventBus
- **报告编号**：P0-10、EventBus ①②③④⑤⑥
- **问题**：Outbox 从未启用；失败消息置 Failed 后永不重试；发布通道断线不重建；消费者启动失败不重启；自定义事件名与 routingKey 不一致致消息路由失败；事件/请求总线 Dispose 共享连接双杀。
- **修复措施**：
  1. `EfCoreOutboxStore`：Failed 进入指数退避重试队列（设最大次数）；加 FOR UPDATE SKIP LOCKED/分布式锁防并发扫描。
  2. `RabbitMqEventBus`：断线重建连接并重新获取发布 Channel；StartAsync 失败后循环重试。
  3. 发布 routingKey 与 `EventBusNameAttribute` 一致。
  4. 共享连接引用计数管理，最后一次 Dispose 才释放。
  5. 至少一个服务（建议 Identity）启用 `AddOutbox` 并落表。
- **预计时间**：3 人天
- **验收标准**：
  - [ ] RabbitMQ 重启后自动恢复发布/消费
  - [ ] 发送失败消息重试后成功投递
  - [ ] 自定义 EventBusName 事件可被订阅方收到
  - [ ] 销毁事件总线不影响请求总线
  - [ ] OutboxMessages 表已创建且消息投递后标记 Sent

### S-20 【P1】Identity 改密流程加固
- **模块**：Identity.Web.API
- **报告编号**：E-4
- **问题**：改密仅凭 Email + 新密码即可执行，无认证、无旧密码、无验证码。
- **修复措施**：
  1. 改密端点要求已认证用户。
  2. 必须携带旧密码（验证通过）或邮箱验证码。
  3. 改密后吊销该用户现有 token。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 未认证调用改密 → 401
  - [ ] 旧密码错误 → 拒绝
  - [ ] 改密后旧 token 失效
  - [ ] 无法仅凭 email 修改他人密码

---

## 类别二：功能缺陷（F）

### F-01 【P0】Markdown 建库与表映射修复
- **模块**：Markdown.Infrastructure / Markdown.Web.API
- **报告编号**：D-3、D-5、D-6
- **问题**：`MarkDown` 映射到表 `NotFileGroup`（与 FileDev 冲突）；ReviewImage 无 EF 配置致模型构建失败；无迁移、无 EnsureCreated，markdown_db 建不出表。
- **修复措施**：
  1. `MarkDownEntityConfiguration` 改回表 `"MarkDown"`、序列 `"MarkDownGuid"`。
  2. 为 ReviewImage 补 EF 配置（主键、关系）。
  3. 生成初始迁移并在 Program.cs 接入 `Migrate()`。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] `dotnet ef migrations list` 存在初始迁移
  - [ ] 启动后 markdown_db 创建 MarkDown/MarkReview/MarkHistory/ReviewImage 等表
  - [ ] 模型构建不抛"ReviewImage 需要主键"
  - [ ] 与 FileDev 共用库时表不冲突

### F-02 【P0】FileDev 分片链路修复（下载路径 + gRPC 落库）
- **模块**：FileDev.Web.API / FileDev.Infrastructure
- **报告编号**：F-1、F-2
- **问题**：合并文件写 `FileStorage/{fileKey}` 但元数据 URI 为 `/files/{userId}/{guid}{ext}`，下载解析路径错位必 404；gRPC 流程 AddAsync 后无 SaveChanges，元数据不落库。
- **修复措施**：
  1. 统一文件物理路径与 URI 映射规则。
  2. gRPC UploadFile/UploadImage/MergeChunks 落盘后执行 SaveChangesAsync。
  3. 端到端测试：HTTP 分片上传→合并→下载 200；gRPC 上传→GetFileInfo 可查。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] HTTP 分片上传后可按 URI 下载成功
  - [ ] gRPC 上传后 DB 可查到记录
  - [ ] 下载/元数据/删除全链路文件可寻址

### F-03 【P0】AppHost 修复（编译 + 资源 + 连接串）
- **模块**：NotBlog.AppHost / NotBlog_Yarp
- **报告编号**：A-1、A-2、A-3、G-4、A-5
- **问题**：RELEASE 分支编译失败（变量作用域）且与 DEBUG 重复 AddProject；RabbitMQ 资源被注释；连接串命名错位（PostgresSQL vs MessagePostgres、CacheMemory vs Redis）；FileDev 不消费 NotFilePostgres；CreateDb 库名不符。
- **修复措施**：
  1. 重构 AppHost：去掉 #if DEBUG 双轨，统一为单一路径（容器资源 + AddProject + WithReference）。
  2. 恢复 RabbitMQ 注册（命名 EventBus），或移除 Identity 的 AddRabbitMQClient（二者一致）。
  3. 统一连接串命名（PostgresSQL/MessagePostgres、Redis/CacheMemory 全局一致）。
  4. 网关接入服务发现（WithReference），删除硬编码 localhost:909x。
  5. 修正/删除 CreateDb.cs（统一走迁移）。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] `dotnet build NotBlog.AppHost -c Release` 通过
  - [ ] Aspire 下 5 服务 + 网关全部启动成功（健康检查通过）
  - [ ] Identity 连上 RabbitMQ、Message 连上 PostgreSQL、Video 连上 Redis
  - [ ] 网关经服务发现路由（非硬编码端口）

### F-04 【P0】Message 运行时稳定性修复
- **模块**：Message.Web.API / Message.Infrastructure
- **报告编号**：P0-12、数据库双注册、Redis Key 冲突、SignalR Clients.User、离线消息
- **问题**：6 个领域事件 Handler 注入未映射的 `IHubContext<Hub>` 致 DI 崩溃；DbContext 被 PostgreSQL 与 SQL Server 双重注册；Redis 在线状态 Key/Schema 冲突互相覆盖；`Clients.User` 读 NameIdentifier 而 JWT 主 Claim 是 sub 致推送落空；`ReceiverId` 从未被设置致离线消息恒空。
- **修复措施**：
  1. 事件 Handler 改注入 `IHubContext<MessageHub, IMessageClient>`。
  2. 删除 SQL Server 注册，统一 Npgsql。
  3. 统一 Redis 在线状态 Key 与 Schema。
  4. 配置 IUserIdProvider 或按 sub Claim 推送。
  5. 消息保存时设置 ReceiverId，激活离线/未读查询。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 发送/撤回/上下线不再抛 DI 异常
  - [ ] 启动日志无 DbContext 双提供程序警告
  - [ ] 在线状态读写一致
  - [ ] 离线用户上线后收到离线消息
  - [ ] SignalR 推送按用户成功送达

### F-05 【P0】Identity 邮件配置与注册链路修复
- **模块**：Identity.Web.API / NotEmail
- **报告编号**：E-10、NotEmail 465 端口
- **问题**：Program 绑定 `NotEmail` 节但配置是 `EmailOptions` 节且键名不符，FromEmail=null 时 SmtpSender NRE，验证码邮件发不出；465 端口固定 StartTls 必失败。
- **修复措施**：
  1. 统一配置节为 `NotEmail`，键名对齐 SmtpPort/UseSsl/Password/FromEmail。
  2. `SmtpEmailSender` 按端口自动选安全模式（465→SslOnConnect，587→StartTls），支持配置覆盖。
  3. 端到端验证注册邮件发送。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 注册请求发出验证码邮件（无 NRE）
  - [ ] 465 端口连接成功
  - [ ] 验证码可在 Redis 查询且与邮件一致

### F-06 【P1】Message 分页与错误码修复
- **模块**：Message.Web.API
- **报告编号**：分页 bug（GroupsApi.cs:548 等 5 处）、错误码语义、Message.FileSize double
- **问题**：`TotalCount = groups.Count()` 返回当前页条数；大量 API catch 后返回 HTTP 200 携带错误；FileSize 用 double 存字节。
- **修复措施**：
  1. 分页查询补 CountAsync 总计数，修正 5 处。
  2. 错误路径统一 HTTP 状态码（400/404/500）+ 标准错误体。
  3. FileSize 改 long。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 分页 TotalCount 为总记录数
  - [ ] 错误请求返回非 200 状态码
  - [ ] FileSize 为 long 无精度损失

### F-07 【P1】Identity 半成品功能补齐
- **模块**：Identity
- **报告编号**：短信 0%、用户查询 0%、密码修改 10%、账号绑定 20%、邮箱确认 0%、登录历史空实现
- **问题**：大量 NotImplementedException（SmsCodeSend、RoleGroupService、UserRoleService、GetUserInfoAsync、FindUserByVagueAsync、AddByLoginHistoryAsync 等）；LinkGithubByUserAsync 恒 true。
- **修复措施**（按序）：
  1. 实现 GetUserInfoAsync/FindUserByVagueAsync。
  2. AddByLoginHistoryAsync 落库登录历史。
  3. 映射密码修改端点（含 S-20 校验）。
  4. 实现账号绑定/解绑真实链路。
  5. 短信验证码接入第三方或配置开关禁用。
  6. 邮箱确认流程。
  7. 清理其余 NotImplementedException（显式降级或实现）。
- **预计时间**：3 人天
- **验收标准**：
  - [ ] 用户信息查询返回真实数据（不再 500）
  - [ ] 登录历史表有记录
  - [ ] 密码修改端点可用且安全
  - [ ] 绑定/解绑真实生效
  - [ ] NotImplementedException 降至可审计的少量（均有 TODO）

### F-08 【P1】Video 功能补全（删除/更新/上传链路）
- **模块**：Video.Web.API
- **报告编号**：V-7、删除无端点、HTTP 上传路径、FileDevClient 死代码
- **问题**：删除 Command 存在但无端点；更新接口作者校验错误；HTTP 上传路径未打通（FileDevClient 死代码）。
- **修复措施**：
  1. 暴露视频删除端点。
  2. 修复更新接口（S-06 已含）。
  3. 打通 HTTP 上传（FileDevClient 接入 FileDev）。
  4. 上传大小/类型校验前置。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] DELETE 视频端点可用且仅作者可操作
  - [ ] 更新视频成功且落库
  - [ ] HTTP 上传视频可被 FileDev 检索
  - [ ] 非允许类型/超大文件被拒

### F-09 【P1】FileDev 功能补全（下载/秒传/文件组/清理）
- **模块**：FileDev
- **报告编号**：秒传 40%、文件组 50%、断点续传 70%、物理删除 0%
- **问题**：秒传仅查重无文件复用；文件组 API 未注册（FileStrongApi 死代码）；取消不清理；软删除不删物理文件。
- **修复措施**：
  1. 秒传命中后复用原文件并绑定当前用户。
  2. 注册文件组 API；修复 CreateNotFileGroupCommand 幂等键。
  3. 取消/过期清理临时分片（并入 S-09）。
  4. 软删除触发物理删除。
  5. 统一哈希算法（SHA256）。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 秒传命中后用户立即获得文件访问权
  - [ ] 文件组 API 可调用
  - [ ] 取消上传后临时分片被清理
  - [ ] 软删除后物理文件最终被删
  - [ ] 查重哈希前后一致

### F-10 【P1】Markdown 功能补全（Query/审核/历史/统计）
- **模块**：Markdown
- **报告编号**：Query 缺失、审核流 0-10%、历史还原缺失、子评论缺失、MarkQuote 统计
- **问题**：无 Query 层；审核状态机未落地；历史还原无 API；子评论无端点；MarkQuote 无计数 API。
- **修复措施**（按序）：
  1. Query/QueryHandler：列表（分页/标签/用户，摘要投影）。
  2. 审核流：MarkStatus 状态机（草稿/待审/通过/驳回）。
  3. 历史还原 API（RestoreFromHistory）。
  4. 子评论 API（AddChildReview）。
  5. MarkQuote 计数并接线领域事件。
- **预计时间**：3 人天
- **验收标准**：
  - [ ] 列表/搜索/按用户查询 API 可用
  - [ ] 待审文章不对外可见；通过后可见
  - [ ] 历史还原后内容恢复
  - [ ] 子评论可创建且关联父评论
  - [ ] 浏览量/点赞随操作变化

### F-11 【P2】CacheMemory 功能缺陷修复
- **模块**：CacheMemory
- **报告编号**：CacheMemory 隐患③
- **问题**：`StringSetManyAsync/StringSetMany` 的 expiry 被静默忽略；分布式锁文档宣称可重入但实现不可重入。
- **修复措施**：
  1. StringSetMany 实现 expiry 透传。
  2. 实现可重入锁（owner 判断）或修正文档声明。
- **预计时间**：0.5 人天
- **验收标准**：
  - [ ] 批量写入 key 按 TTL 过期
  - [ ] 文档与实现一致

### F-12 【P2】网关功能补全
- **模块**：NotBlog_Yarp
- **报告编号**：G-1、G-2、G-7、G-6
- **问题**：权限 HTTP 端点与 Identity 契约断裂（恒 403）；未映射路径 fail-open；/MessageHub 未入网关；权限检查 HTTP 无弹性。
- **修复措施**：
  1. 对齐/实现权限检查端点，或改本地策略。
  2. 为 video 全部路由与 message 的 7 条未映射路由补权限映射；fail 策略配置化。
  3. /MessageHub 加入网关路由（WebSocket 代理）。
  4. 权限客户端加 AddStandardResilienceHandler。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 网关权限检查不再恒 403
  - [ ] video/message 全部路由有明确认证/权限策略
  - [ ] SignalR 经网关可连
  - [ ] Identity 短暂不可用时降级而非全站 403

---

## 类别三：性能与稳定性（P）

### P-01 【P1】CacheMemory 异步路径阻塞修复
- **模块**：CacheMemory
- **报告编号**：CacheMemory 隐患①
- **问题**：`GetDatabaseAsync` 内部走同步 `GetConnection()`，未连接时 `Thread.Sleep` 指数退避阻塞线程池。
- **修复措施**：
  1. 提供真正异步连接获取（ConnectAsync），异步路径不再同步阻塞。
  2. 或 GetDatabaseAsync 改 ValueTask，同步路径仅限初始化并注释。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 并发 1000 请求无阻塞超时
  - [ ] async 路径无 Thread.Sleep（评审）

### P-02 【P1】CacheMemory 连接重建泄漏修复
- **模块**：CacheMemory
- **报告编号**：CacheMemory 隐患②
- **问题**：断线重建不 Dispose 旧 ConnectionMultiplexer，事件回调重复登记。
- **修复措施**：
  1. 重建时 Dispose 旧实例并注销事件。
  2. 重建加锁防并发。
- **预计时间**：0.5 人天
- **验收标准**：
  - [ ] 模拟断线重连 N 次后仅 1 个活动连接
  - [ ] 事件回调不重复触发

### P-03 【P1】FileDev/Video 大文件内存优化
- **模块**：FileDev / Video
- **报告编号**：F-5（内存）、V-6
- **问题**：下载整文件 ReadAllBytes 入内存；视频上传 500MB 整体读入 byte[] 再 ByteString 二次拷贝（峰值约 1.5GB）；gRPC 单消息 1GB。
- **修复措施**：
  1. 下载流式化（FileStream + Results.File）。
  2. 视频上传改流式 gRPC（客户端流）或分片上传。
  3. gRPC 消息上限下调。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 下载 2GB 文件峰值内存 < 100MB
  - [ ] 上传 500MB 视频峰值内存 < 500MB
  - [ ] 并发 10 上传无 OOM

### P-04 【P2】Video 缓存与失效策略优化
- **模块**：Video
- **报告编号**：V-8、缓存失效（列表 5 分钟脏读）、VideoCacheService while 重建
- **问题**：写操作不触发列表缓存失效；计数重建 O(n) while 循环；回复缓存无法按视频批量失效。
- **修复措施**：
  1. 写操作统一触发 InvalidateVideoListsAsync。
  2. 计数改 Redis INCR/单次聚合 + 异步落库。
  3. 缓存 key 支持按视频维度批量删除回复缓存。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 写操作后列表缓存立即失效
  - [ ] 百万级计数重建性能可接受
  - [ ] 删视频时相关回复缓存被清

### P-05 【P2】全站资源限制与校验补全
- **模块**：Markdown / Video / NotEmail
- **报告编号**：D-8、V-12、IMAP 附件限制、Markdown 列表全量正文
- **问题**：Markdown take 无上限、Content 无 MaxLength；弹幕无长度限制；IMAP 附件全量载入内存；列表返回全量正文。
- **修复措施**：
  1. take 上限 100 + Content MaxLength + 列表摘要投影。
  2. 弹幕/评论长度上限。
  3. IMAP 附件大小限制。
  4. DTO 数据注解 + 统一校验。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 超大 take/超长 Content/弹幕被 400 拒
  - [ ] IMAP 超大附件不整载内存
  - [ ] 列表响应不含全量正文

### P-06 【P2】架构性能隐患
- **模块**：DomainInfrastructure / EventBus / JWToken
- **报告编号**：ReflectionHelper 全量程序集加载、NvidGenerator 取模偏差、包版本通配符
- **问题**：ReflectionHelper 对 BaseDirectory 全量 DLL LoadFile 且吞异常，启动开销大；NvidGenerator 取模 62 有偏差；各 csproj 包版本 `*` 不可复现构建。
- **修复措施**：
  1. 限制程序集扫描范围（引用关系/白名单）。
  2. NvidGenerator 改拒绝采样。
  3. csproj 固定包版本。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 启动时间下降（对比基准）
  - [ ] 生成 id 分布均匀（统计测试）
  - [ ] dotnet restore --locked-mode 可复现

---

## 类别四：代码质量与架构（Q）

### Q-01 【P1】僵尸代码与死代码清理
- **模块**：Markdown / FileDev / Video / Message / Identity
- **报告编号**：Markdown 僵尸代码、FileDev 死代码、Message 未用实体、Identity 死代码
- **问题**：大量实体/事件/端点已定义但未接线：MarkHistory 无 DbSet/仓储/API；集成事件处理器仅 Console.WriteLine；FileStrongApi 未注册；FileDevClient 死代码；MessageForward 从未使用。
- **修复措施**：
  1. 逐个确认后删除或接线（优先接线 MarkHistory、MessageForward）。
  2. 删除死代码文件（FileStorageGrpcService.cs Controller、FileServicesDi、IDServiceProvider）。
  3. 清理失效 csproj `<Compile Remove>`。
  4. Console.WriteLine 改正式日志或删除。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 删除后 dotnet build 0 错误
  - [ ] 无"定义但零引用"公开类（IDE 分析复核）
  - [ ] 无 Console.WriteLine 残留（除测试）

### Q-02 【P1】命名与拼写修复
- **模块**：全站
- **报告编号**：SavaChangesAsync、NotFileExeption、FileAccessAttrubit、VideioHositoli、MarkHositoryGuid
- **问题**：大量拼写错误与命名混乱（Save→Sava、Exception→Exeption、History→Hository 等）。
- **修复措施**：
  1. VS 全局重命名修正拼写，同步 EntityConfig/DbContext 字符串引用。
  2. 修正 SavaChangesAsync → SaveChangesAsync 并保留真实返回值。
  3. 统一命名风格。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 全仓无 Sava/Exeption/Hository/Attrubit/Videio 拼写
  - [ ] SaveChangesAsync 返回真实影响行数
  - [ ] dotnet build 0 错误

### Q-03 【P1】消除双重 SaveChanges 与事务语义稀释
- **模块**：DomainInfrastructure / Message / Video / Markdown / Identity / FileDev
- **报告编号**：DomainInfrastructure 双重 SaveChanges、各模块双 SaveChanges、TransactionBehavior
- **问题**：DbContext 覆写 SaveChangesAsync 分发领域事件，UnitOfWorkFilter 又提交一次；Handler 内各自 SaveChanges + TransactionBehavior 提交，事务语义被稀释；领域事件可能在第二次 Save 时重复分发。
- **修复措施**：
  1. 明确唯一提交入口：统一走 UnitOfWork/TransactionBehavior，Handler 内不再直接 SaveChanges。
  2. 或 DbContext 与 Filter 二选一分发领域事件。
  3. 事务内禁止多次 SaveEntitiesAsync（分批场景显式处理）。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] 单次请求仅 1 次 SaveChanges（日志/测试断言）
  - [ ] 领域事件不重复分发（发送消息仅触发 1 次 MessageSentEvent）
  - [ ] 失败回滚正确

### Q-04 【P1】DomainInfrastructure 连接串与反射隐患
- **模块**：DomainInfrastructure / CommonsInitializer
- **报告编号**：EFCoreInitializerHelper 共用连接串、领域事件字符串反射、连接串键名三套并存、UseNotBlogPipeline 空实现
- **问题**：反射注册使所有 DbContext 共用同一连接串；领域事件按字符串反射查找属性；连接串键名 DbContextConnect/DefaultDB:ConnStr/DbaseConnection 并存；空实现管线误导调用方。
- **修复措施**：
  1. EFCoreInitializerHelper 支持按 DbContext 指定连接串。
  2. 领域事件分发改用强类型接口（IDomainEvents）。
  3. 统一连接串键名并文档化。
  4. 实现 UseNotBlogPipeline（见 S-03）。
- **预计时间**：1.5 人天
- **验收标准**：
  - [ ] 各 DbContext 可指向独立数据库
  - [ ] 领域事件接口无字符串魔法
  - [ ] 连接串键名单一且文档一致
  - [ ] UseNotBlogPipeline 为有效管线

### Q-05 【P1】缓存服务接线与 Key 设计统一
- **模块**：Message.Infrastructure
- **报告编号**：Redis Key 冲突、4 个缓存服务零使用、SignalR Clients.User
- **问题**：UnreadCountCacheService/SessionCacheService/UserStatusCacheService/RedisCacheService 注册但零使用；在线状态 Key 冲突；未读计数依赖 EF 查询。
- **修复措施**：
  1. 统一在线状态 Key/Schema（见 F-04）。
  2. 未读计数接入 UnreadCountCacheService（写时更新、读时查询）。
  3. 明确各缓存服务职责并接线。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 4 个缓存服务均有调用方
  - [ ] 未读计数经缓存读写
  - [ ] Key/Schema 全局唯一

### Q-06 【P2】版本与依赖治理
- **模块**：全站
- **报告编号**：包版本通配符、框架版本混杂（Message Infrastructure SignalR 1.2.11/Mvc 2.3.11）、csproj 硬编码路径
- **问题**：包版本 `*` 不可复现；Message.Infrastructure 引用陈旧包；Video.csproj 硬编码本机 dotnet 绝对路径。
- **修复措施**：
  1. 固定全部包版本（Directory.Packages.props 集中管理）。
  2. Message.Infrastructure 升级统一到 net10.0 包。
  3. 删除 csproj 硬编码路径引用。
  4. 清理未使用包引用（Message.Web.API 的 Aspire.RabbitMQ.Client 等）。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] 无 `Version="*"`
  - [ ] dotnet restore --locked-mode 成功
  - [ ] 无本地绝对路径引用
  - [ ] 无未使用 PackageReference（IDE 检查）

### Q-07 【P2】代码卫生
- **模块**：全站
- **报告编号**：未使用参数（CS9113）、空引用警告（CS8603/8625）、锁定状态冗余、错误码语义、AllowedHosts *
- **问题**：约 134 个编译警告；UserSafety.LockOutEnd 与 UserAccessFail.LockOutEnd 冗余；HTTP 200 携带错误。
- **修复措施**：
  1. 逐模块清理 CS9113（未使用参数改为 `_` 或删参）。
  2. 修复 CS8618/CS8603/8625 空引用。
  3. 合并锁定状态字段，消除冗余。
  4. AllowedHosts 配置化。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] dotnet build 0 警告（或降至可接受阈值 <20）
  - [ ] 锁定逻辑仅单一来源
  - [ ] AllowedHosts 为配置值

---

## 类别五：测试与文档（T）

### T-01 【P1】Message 测试补强
- **模块**：Message.Tests
- **报告编号**：测试缺口（无 API 层/越权/伪造头测试）
- **问题**：现有 106 用例无 API 层测试、无越权/权限负向用例、无 X-User-Id 伪造测试。
- **修复措施**：
  1. API 层测试（WebApplicationFactory）覆盖 Groups/Files/Tweets/Sessions 端点。
  2. 越权负向用例：群组权限、Session 成员、文件所有权、撤回发送者。
  3. 认证用例：无 token→401；伪造 X-User-Id 无效。
  4. 对应 S-02/S-04/S-05 修复项建立回归测试。
- **预计时间**：3 人天
- **验收标准**：
  - [ ] 新增用例覆盖全部 S-02/S-04/S-05 验收点
  - [ ] dotnet test Message.Tests 全绿
  - [ ] 覆盖率报告：安全相关路径 ≥ 80%

### T-02 【P1】其余模块测试项目
- **模块**：Identity / Markdown / FileDev / Video
- **报告编号**：5/6 模块零测试
- **问题**：仅 Message 有测试，其余业务模块无任何测试项目。
- **修复措施**：
  1. 建立 Identity.Tests：认证/授权/令牌/OAuth 关键路径。
  2. 建立 FileDev.Tests：分片/合并/配额/清理（含 F-01/F-02 回归）。
  3. 建立 Video.Tests：仓储 WHERE 回归（S-06）、权限。
  4. 建立 Markdown.Tests：表映射/迁移/越权回归。
  5. 与各 P0/P1 修复项绑定验收。
- **预计时间**：4 人天（每模块 1 人天）
- **验收标准**：
  - [ ] 4 个测试项目可在 CI 运行
  - [ ] 各项目关键修复项有对应测试用例
  - [ ] dotnet test 全绿

### T-03 【P2】文档对齐与补全
- **模块**：全站
- **报告编号**：Message 文档背离、PERMISSION_DESIGN 偏差、Video/Markdown/FileDev 无文档
- **问题**：Tweet 文档承诺与实现不符（实体缺失、审核占位）；PERMISSION_DESIGN 与实现系统性偏差；Video 设计文档被排除编译；FileDev/Markdown 无文档。
- **修复措施**：
  1. 更新 Tweet 功能文档/list.md 至实际实现。
  2. 对齐 PERMISSION_DESIGN.md 与实现（或按实现重写）。
  3. 恢复 Video 设计文档并补内容。
  4. 各模块补 README（架构/配置/迁移说明）。
- **预计时间**：2 人天
- **验收标准**：
  - [ ] 文档声明与代码一致（抽查 5 处）
  - [ ] Video 设计文档可编译/可读
  - [ ] 各模块 README 包含连接串/迁移/启动说明

### T-04 【P2】CI 与质量门禁
- **模块**：仓库
- **报告编号**：无 CI、构建警告多、测试未自动化
- **问题**：无 CI 流水线，构建/测试/扫描未自动化。
- **修复措施**：
  1. 建立 CI（GitHub Actions/Azure Pipeline）：build + test + TreatWarningsAsErrors（可选）。
  2. 接入依赖漏洞扫描（dotnet list package --vulnerable）。
- **预计时间**：1 人天
- **验收标准**：
  - [ ] CI 构建 + 全部测试通过
  - [ ] 依赖漏洞扫描无高危
  - [ ] 提交自动触发

---

## 修复路线图

### 阶段排期（按单人全开发时间估算）

| 阶段 | 内容 | 涉及修复项 | 预估工期 | 目标成果 |
|---|---|---|---|---|
| **阶段一：安全红线** | 认证体系、密钥治理、越权、数据安全 | S-01~S-12（P0 全部） | **约 18 人天** | 无 P0 漏洞；认证/授权全站生效；数据不再可被破坏/伪造 |
| **阶段二：链路打通** | 建库、上传下载、消息稳定性、宿主启动 | F-01~F-05（P0 功能） | **约 8 人天** | 6 大模块均可启动、核心链路（注册/登录/上传/下载/发消息）端到端可用 |
| **阶段三：加固与补全** | 登录加固、幂等、XSS、内容安全、半成品功能 | S-13~S-20、F-06~F-12 | **约 17 人天** | P1 项全部完成；功能缺失补齐 |
| **阶段四：性能与质量** | 内存/阻塞/缓存优化、代码清理、命名 | P-01~P-06、Q-01~Q-07 | **约 12 人天** | 性能达标、构建 0 警告、无僵尸代码 |
| **阶段五：测试与文档** | 测试项目、CI、文档对齐 | T-01~T-04 | **约 10 人天** | 全模块测试覆盖、CI 门禁、文档一致 |
| **合计** | — | — | **约 65 人天**（单人大约 3 个月） | 具备可上线条件 |

### 资源分配建议

- **阶段一**：投入 1 人全时（安全是上线前提，不并行其他功能开发）；若 2 人，可将 S-04/S-05（Message 越权）与 S-07/S-08（Video/FileDev 认证）并行。
- **阶段二**：1 人全时按"先宿主（F-03）→ 再各模块链路"顺序；宿主是其他模块可启动的前提。
- **阶段三**：可拆 2 人并行（Identity 半成品 vs Message/Video 功能补全）。
- **阶段四/五**：适合 1 人串行完成；测试可外包给熟悉该模块的人。

### 建议执行顺序（按依赖）

```
阶段一 → 阶段二 → 阶段三 → 阶段四 → 阶段五
  ↑ S-01（密钥）最先做，其他修复依赖干净配置
  ↑ F-03（AppHost）在阶段二最先做，决定可运行环境
```

---

## P0 红线对照表

| P0 编号 | 问题 | 对应修复项 | 状态 |
|---|---|---|---|
| P0-1 | 认证/授权中间件缺失或可绕过 | S-02、S-03、S-07、S-08、S-10 | ☐ |
| P0-2 | JWT 私钥硬编码 + 凭据明文 | S-01 | ☐ |
| P0-3 | 群组/Session/文件/会话 IDOR | S-04、S-05、S-08、S-10 | ☐ |
| P0-4 | OAuth 登录 CSRF | S-11 | ☐ |
| P0-5 | Video ExecuteUpdate 全表覆盖 | S-06 | ☐ |
| P0-6 | 网关权限校验契约断裂 | F-12 | ☐ |
| P0-7 | Markdown 表映射/迁移/CurrentUser | F-01、S-10 | ☐ |
| P0-8 | FileDev 分片不可下载/gRPC 不落库 | F-02 | ☐ |
| P0-9 | Identity RefreshToken 矛盾/过期 7 秒 | S-12 | ☐ |
| P0-10 | EventBus Outbox/重试/断线 | S-19 | ☐ |
| P0-11 | AppHost 编译失败/资源缺失 | F-03 | ☐ |
| P0-12 | Message 事件 Handler DI 崩溃 | F-04 | ☐ |

---

## 编号索引

### 评估报告编号 → 修复项对照

| 报告编号 | 修复项 | 报告编号 | 修复项 |
|---|---|---|---|
| C-1 | S-01 | D-7 | S-10 |
| C-2 | S-01 | D-8 | P-05 |
| C-3 | S-01 | D-9 | S-14 |
| C-4 | S-16 | D-10 | Q-01（并入） |
| E-1 | S-03 | D-11 | S-01 |
| E-2 | S-11 | F-1 | F-02 |
| E-3 | S-01 | F-2 | F-02 |
| E-4 | S-20 | F-3 | S-08 |
| E-5 | S-12 | F-4 | S-08 |
| E-6 | S-12 | F-5 | S-09 / P-03 |
| E-7 | S-11 | F-6 | S-09 |
| E-8 | F-12 | F-7 | S-08 |
| E-9 | S-12 | F-8 | S-08 |
| E-10 | F-05 | F-9 | S-17 |
| E-11 | S-16 | F-10 | S-01 |
| E-12 | S-13 | F-11 | S-16 |
| E-13 | S-16 | F-12 | S-17 |
| E-14 | S-15 | V-1 | S-06 |
| E-15 | S-13 | V-2 | S-07 |
| E-16 | S-13 | V-3 | S-07 |
| M-1 | S-02 | V-4 | S-18 |
| M-2 | S-04 | V-5 | S-07 |
| M-3 | S-05 | V-6 | P-03 |
| M-4 | S-05 | V-7 | S-06 / F-08 |
| M-5 | S-05 | V-8 | S-06 / P-04 |
| M-6 | S-05 | V-9 | S-18 |
| M-7 | S-05 | V-10 | S-07 |
| M-8 | S-01 | V-11 | S-14 |
| M-9 | S-15 | V-12 | S-17 |
| M-10 | S-16 | G-1 | F-12 |
| M-11 | S-02 | G-2 | F-12 |
| M-12 | S-16 | G-3 | S-02 |
| M-13 | S-17 | G-4 | F-03 |
| M-14 | S-17 | G-5 | S-01 |
| M-15 | S-17 | G-6 | F-12 |
| M-16 | S-05 | G-7 | F-12 |
| D-1 | S-10 | A-1 | F-03 |
| D-2 | S-10 | A-2 | F-03 |
| D-3 | F-01 | A-3 | F-03 |
| D-4 | S-10 | A-4 | T-03（并入） |
| D-5 | F-01 | A-5 | F-03 |
| D-6 | F-01 | P0-1~P0-12 | 见 P0 红线对照表 |
