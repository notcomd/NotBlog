# FileDev 文件权限重构 · 技术开发文档

> 状态：设计草案（v1，待评审）
> 适用范围：FileDev 文件存储子域（f:\NotBlog\FileDev.Domain / FileDev.Infrastructure / FileDev.Web.API）及其上游文件消费方（Message / Markdown / Video）

---

## 1. 背景与目标

### 1.1 现状问题

FileDev 现有文件权限模型以 `FileIdentity`（**FilePublic / FilePrivate / FilePrivatePublic / FilePasswordProtected**）作为唯一可见性来源，叠加 `FileSource` 区分文件归属（UserRepository / ContentAttachment），并在下载/元数据查询链路里用「`file.UserId == request.UserId`」做私有文件归属校验。

存在两个结构性问题：

1. **权限判定过于复杂且职责泛化**：`FileIdentity` 一维承载了公开/私有/受限公开/密码保护四种语义，但业务上「文件随所属内容可见」这一最关键的关联关系没有建模——内容附件（图文推文 / Markdown 文章 / 视频）的可见性实际应取决于**所属业务内容本身**的可见性，而不是附件自己重复声明一份。
2. **无鉴权下载通道与权限诉求冲突**：`/files/{**path}` 端点（[FileDownloadApi.cs](file:///f:/NotBlog/FileDev.Web.API/APIs/FileDownloadApi.cs)）为了支撑 `<img src>` 直链而设计为**无鉴权**（靠 v7 GUID 不可枚举 + 泄露即有权，S3 预签名思路）。它无法覆盖「随内容继承权限的附件在受限/私有内容下禁止外泄」的诉求。

### 1.2 本次目标

将 FileDev 的文件体系从「一维 FileIdentity」拆分为**两类文件、各自独立且清晰的权限分配模型**：

- **第一类：内容附件**（由 Message 图文推文、Markdown 文章、Video 视频 引用的资源文件）
  - 权限**随所属内容自动继承**，不单独维护一套可见性状态。
  - 可见性 = 「该附件被哪个业务内容引用 + 该内容的公开性」共同决定。
- **第二类：个人文件**（通过 FileDev 直接上传的图片 / 视频 / 音频 / 压缩文件，归属用户文件仓库）
  - 保留**用户可显式公开 / 私密**的开关（沿用 `FileIdentity` 的公开/私密语义），并升级为**多级角色范围**（所有者 / 公开 / 好友 / 指定用户 / 群成员 / 仅私密）。

最终产出：一份可落地的技术开发文档，明确数据模型、接口/字段变更、鉴权流程与改造范围。

---

## 2. 术语与设计决策（已确认）

| 决策点 | 结论 |
|---|---|
| 两类文件权限模型 | 内容附件**随内容继承**；个人文件用 `FileIdentity` |
| 权限范围粒度 | 两类均采用**多级角色范围** |
| 个人文件公开开关 | 保留用户**显式公开/私密**，并支持后续修改 |
| 文档产出 | 完整技术开发文档 |

**核心设计原则**：内容附件的可见性**不再重复存储**一份独立权限，而是「引用它的业务内容」的可见性即文件可见性；个人文件则自带独立的多级访问范围。

---

## 3. 权限模型设计

### 3.1 两类文件概览

| 类别 | 文件来源 | 权限主体 | 可见性来源 | 典型文件 |
|---|---|---|---|---|
| 内容附件 | `FileSource.ContentAttachment` | 承载体=业务内容 | **随所属内容继承** | Message 图文/推文资源、Markdown 封面/图片、Video 视频/封面 |
| 个人文件 | `FileSource.UserRepository` | 承载体=所有者用户 | **独立多级范围** | 用户自行上传的图片/视频/音频/压缩文件 |

### 3.2 访问范围模型（个人文件）

为向**多级角色范围**演进，建议引入一个 `FileVisibility` 概念作为个人文件的访问范围维度，与现有 `FileIdentity` 语义映射如下（**向后兼容**，不删既有枚举）：

| FileVisibility | 中文 | 谁可读 | 与现有 FileIdentity 映射 |
|---|---|---|---|
| `Owner` | 仅所有者 | 只有文件所有者 | `FilePrivate` |
| `Friends` | 好友可见 | 所有者 + 对方已添加为好友 | （新增） |
| `Whitelist` | 指定用户 | 所有者 + 显式白名单用户（支持逐个授权） | `FilePasswordProtected` 可沉淀为「白名单访问」的演进形态 |
| `Group` | 群/圈子成员 | 所有者 + 指定群/圈子成员 | （新增） |
| `Public` | 公开 | 所有人（无需登录） | `FilePublic` / `FilePrivatePublic` |

> 说明：当前代码未建模「好友/群/圈子」关系，本档将其作为**目标态**（推荐演进），并明确标注为实现依赖；若暂不引入关系模型，可先落地 `Owner / Whitelist / Public` 三档（见 §3.4 分期）。

### 3.3 内容附件权限（随内容继承）

内容附件不新增独立权限字段，权限判定改为**上行关联**：

```
请求访问附件 -> 找到引用该附件的业务内容 -> 判定业务内容对「请求者」是否可见
  可见? -> 允许读
  不可见 -> 拒绝
```

- 附件 → 内容：经 `ContentAttachmentRef`（[ContentAttachmentRef.cs](file:///f:/NotBlog/FileDev.Domain/Entities/ContentAttachmentRef.cs)，字段 `ContentId` + `ContentType`）上行定位承载体内容聚合根。
- 内容 → 可见性：由上游业务域（Message / Markdown / Video）各自提供「内容对指定用户是否可见」的判定服务（见 §5.3）。
- 公开内容 → 附件公开；私有/受限内容 → 附件仅限相应可见用户群。

### 3.4 分期落地

- **P1（本次主推，零关系模型依赖）**：区分两类文件；
  - 内容附件走「随内容继承」；
  - 个人文件落地 `Owner / Whitelist / Public`（白名单可先以「附件 token / 访问口令」承载，即现有 `FilePasswordProtected` 演进）；
  - `/files` 直链按「公开 or 内容公开 or 所有者 or 白名单」放行。
- **P2（需好友/群关系模型）**：个人文件补齐 `Friends / Group` 档位；范围判定下沉为可复用的授权服务。

---

## 4. 数据模型变更

### 4.1 NotFile（核心文件记录）

位置：`f:\NotBlog\FileDev.Domain\Entities\NotFile.cs`

保留既有字段（`FileIdentity`、`FileSource`、`FileType`、`UserId`、`FileUri`），**新增/调整**：

```csharp
// 建议新增：个人文件的多级访问范围（P1 先支持 Owner/Whitelist/Public）
public FileVisibility Visibility { get; private set; } = FileVisibility.Public;

// 建议新增：白名单访问的用户 ID 集合（Visibility == Whitelist 时有效）
private List<Guid> _whitelistUserIds = [];
public IReadOnlyList<Guid> WhitelistUserIds => _whitelistUserIds;

// 可选演进：群/圈子作用域 ID（Visibility == Group 时有效，P2）
public Guid? ScopeId { get; private set; }
```

- `FileSource == ContentAttachment` 时，`Visibility` 字段不参与鉴权（以内容可见性为准）；此时可强制其值为 `Owner` 作为占位，避免脏数据。
- 提供 `ChangeVisibility(...)` / `GrantUser(...)` / `RevokeUser(...)` 领域方法，支持用户修改公开/私密（对应 [UpdateFileInfoCommandHandler.cs](file:///f:/NotBlog/FileDev.Web.API/Application/Commands/UpdateFileInfoCommandHandler.cs) 的已存在「仅所有者可改」约束）。

### 4.2 ContentAttachmentRef

位置：`f:\NotBlog\FileDev.Domain\Entities\ContentAttachmentRef.cs`

已具备 `ContentId + ContentType + FileUri + SourceFileId`，**无需结构性改动**；需补充能力：
- 按 `FileUri`（或 `SourceFileId`）反查「引用它的业务内容集合」的查询方法（用于内容附件鉴权上行定位）。当前 `IContentAttachmentService`（[接口](file:///f:/NotBlog/FileDev.Domain/IServices/IContentAttachmentService.cs)）仅有 `GetByContentIdAsync`，需新增 `GetByFileAsync(ContentType, Uri/SourceFileId)`。

### 4.3 新增枚举与授权判据

- `f:\NotBlog\FileDev.Domain\Enum\FileVisibility.cs`（新增）：`Owner / Friends / Whitelist / Group / Public`。
- 可由 `FileIdentity` 派生兼容值（见 §3.2 映射表），`FileResult`（[FileResult.cs](file:///f:/NotBlog/FileDev.Domain/Entities/FileResult.cs)）的 `IsPublic()/IsPrivate()` 相应扩展。

---

## 5. 鉴权流程与接口变更

### 5.1 统一授权判定入口（推荐）

在 `FileDev.Domain.IServices` 新增：

```csharp
public enum FileAccessDecision { Allowed, Denied, NotApplicable }

public interface IFileAccessEvaluator
{
    Task<FileAccessDecision> CanReadAsync(Guid fileId, Guid? requesterUserId,
        Guid? sessionUserId, CancellationToken ct = default);
}
```

判定逻辑：
1. `fileName` 载入 `NotFile`；
2. 若 `Source == ContentAttachment` → 上行到引用内容，交给内容可见性判定（§5.3），若**无引用**则视为孤立附件，回退个人文件规则或拒绝；
3. 若 `Source == UserRepository` → 按 `FileVisibility` 判定（Owner / Whitelist / Public；P2 扩展 Friends/Group）。

### 5.2 接口 / 链路改造点（代码位置）

| 现状 | 位置 | 改造 |
|---|---|---|
| 下载（私有文件按 owner 校验） | [DownloadFileQueryHandler.cs](file:///f:/NotBlog/FileDev.Web.API/Application/Queries/DownloadFileQueryHandler.cs#L24) | 改用 `IFileAccessEvaluator.CanReadAsync` 统一判定 |
| 元数据查询（私有文件按 owner 校验） | [GetFileInfoQueryHandler.cs](file:///f:/NotBlog/FileDev.Web.API/Application/Queries/GetFileInfoQueryHandler.cs#L22) | 同上 |
| 更新文件信息（仅所有者） | [UpdateFileInfoCommandHandler.cs](file:///f:/NotBlog/FileDev.Web.API/Application/Commands/UpdateFileInfoCommandHandler.cs) | 保留「仅所有者可改」；新增可改 `Visibility` |
| **无鉴权下载 `/files/{**path}`** | [FileDownloadApi.cs](file:///f:/NotBlog/FileDev.Web.API/APIs/FileDownloadApi.cs) | 需支持两类放行：公开个人文件 / 附件且所属内容公开；对非公开内容附件与私有个人文件**拒绝**（见 §5.4 预签名/临时 token 方案） |
| 上传链路（区分两类） | [UploadFileCommandHandler.cs](file:///f:/NotBlog/FileDev.Web.API/Application/Commands/UploadFileCommandHandler.cs)、`StreamUploadCommandHandler.cs` | 上传时按 `contentId` 是否为空设置 `FileSource`；据此决定是否登记 `ContentAttachmentRef` 与初始 `Visibility` |

### 5.3 内容可见性判定服务（上游对接）

FileDev 不直接感知 Message/Markdown/Video 内容模型，定义**上行委托接口**，由各消费方实现或注册：

```csharp
public interface IContentVisibilityProvider
{
    ContentType ContentType { get; }
    Task<bool> IsVisibleToAsync(string contentId, Guid? requesterUserId, Guid? sessionUserId,
        CancellationToken ct = default);
}
```

- Message 图文推文：公开推文对所有人可见；私聊/群聊按会话成员可见。
- Markdown 文章：公开文章可见；私有文档按作者/授权可见。
- Video 视频：按视频发布者 + 公开/私有可见。
- 各 provider 可由 Web.API 组装时按 `ContentType` 注册，FileDev 经 `ContentAttachmentRef.ContentType` 路由调用。

### 5.4 无鉴权直链的取舍

`<img src>` / `<video>` 无法带 Authorization 头。**保留 `enableRangeProcessing`**（视频拖动/图片渐进）的前提下，公开文件的直链 `CacheControl` 策略维持；为此类访问引入**临时签名 token**：
- 公开个人文件与公开内容附件 → 允许直链（维持现状）。
- 受限/私有附件 → 下载前由接口签发**短期一次性 token**（签名 URL，含 fileId+expires），`/files` 校验 token 后放行。该机制可复用现有「预签名 URL」思路（文档注释已提及），作为 P1 的补充项。

---

## 6. 影响面与回归

### 6.1 受影响模块

- **FileDev.Domain**：新增 `FileVisibility`、`IFileAccessEvaluator`、`IContentVisibilityProvider`；`NotFile` 增字段与方法；`ContentAttachmentRef` 增反查查询。
- **FileDev.Infrastructure**：仓储补充按 FileUri/SourceFileId 反查内容引用；`NotFileRepository` 支持白名单集合持久化。
- **FileDev.Web.API**：`DownloadFileQueryHandler`/`GetFileInfoQueryHandler`/`UpdateFileInfoCommandHandler`/`FileDownloadApi` 接入统一授权；`UploadFileCommandHandler`/`StreamUploadCommandHandler` 设置 `FileSource` 与初始 `Visibility`；gRPC 映射补充 `FileVisibility`（参考 [FileStorageServiceGRPC.cs](file:///f:/NotBlog/FileDev.Web.API/Grpc/FileStorageServiceGRPC.cs) 既有 enum 映射）。
- **上游**：Message / Markdown / Video 各自提供 `IContentVisibilityProvider` 实现或在 Web.API 内注册。

### 6.2 回归要点

- 现有已上传个人文件默认保持可见（`Visibility` 默认 `Public` 与旧 `FilePublic` 对齐，或按原 `FileIdentity` 迁移映射）。
- 内容附件历史数据：因其 `Source == ContentAttachment` 且已有 `ContentAttachmentRef`，鉴权切到「随内容继承」后需回归验证公开拓展媒体渲染、私聊图片等。
- `/files` 直链对公开附件仍需可访问（不得误伤现有 `<img>` 渲染）。

---

## 7. 非目标 / 明确不做（当前阶段）

- 不重建好友 / 群 / 圈子关系模型（P2），`Friends/Group` 档位仅在关系就绪后启用。
- 不修改 `/files/{**path}` 的 Range/缓存既有行为（仅新增鉴权分支）。
- 不做多租户 `IObjectStorage` 层权限（FileBox 层不承载业务 ACL，权限停留在 FileDev 域模型层）。

---

## 8. 待确认 / 下一步

1. 确认 `FilePrivatePublic`（受限公开）与 `FilePasswordProtected`（密码保护）在「多级范围」中的目标归属（本档将其分别折抵为 `Whitelist` 白名单 / 临时访问口令）。
2. 确认好友/群关系模型是否已存在，决定 P1 是否含 `Friends/Group` 档位。
3. 确认上游内容可见性判定的宿主（是否由 Message/Markdown/Video 各自实现 `IContentVisibilityProvider` 并注册到 FileDev）。
4. 确认 `/files` 签名 token 的过期时间与一次性策略（默认建议 5 分钟、一次性、签名 = HMAC(fileId+expires)）。