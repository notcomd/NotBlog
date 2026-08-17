# Message.Web.API API 重新设计方案

> 版本：v1.0（草案）→ **v1.1（已实施）**
> 范围：Message.Web.API 全部 REST API + SignalR Hub 通道；核心是**文件上传与消息附件统一接入 FileDev.Web.API 的 gRPC 文件服务**。

---

## 〇、实施状态（v1.1）

**已全部实施完毕**（本仓库 master 分支，构建 0 警告 0 错误，Message.Tests 156 用例全绿）。

| 阶段 | 内容 | 状态 |
|------|------|------|
| 一 | 上传通道重写：`POST /api/files/upload`、`/upload-image`（multipart + FileDev gRPC）、`chunk/upload` 改 multipart、FileRef DTO、MimeTypeMap | ✅ |
| 二 | 附件模型：FileAttachment.FileId、SendMessageRequest/Command 重构（fileId 体系）、gRPC 归属校验、附件资源端点（4 个）、AddAttachmentCommand、DeleteFile 级联、Hub 命令链路统一 + SendFileMessage、Tweet FileIds 同构 | ✅ |
| 三 | 下载/预览流式代理（客户端补 DownloadFileAsync/DownloadImageAsync + AsyncEnumerableStream）、旧 `/api/files/{id}*` 端点移除 | ✅ |
| 四 | 测试补强（+5 用例）、文档更新、全解决方案构建验证 | ✅ |

### 与设计草案的差异记录（实施中修正）

1. **FileId 索引为非唯一**：设计草案 §6.1 原定唯一索引，但转发/群聊共享同一 FileDev 文件时
   多条附件记录指向同一 FileId，唯一索引会冲突——改为普通索引。
2. **流式下载元数据来源**：FileDev 服务端把 FileName/FileSize/ContentType 放在**每个分片消息**
   而非响应头中，且 DownloadFile 无 ResponseHeaders 可用——客户端流式方法只转发二进制分片，
   HTTP 响应头（Content-Type/Content-Disposition）由 Message 侧附件记录提供（MimeType/FileName）。
3. **Hub 命令链路**：MessageHub 原复制了整套 Send* 私有实现（约 8.2KB 重复代码），已删除，
   CreateMessageAsync 统一走 SendMessageCommand；新增 `SendFileMessage(sessionId, fileId)`。
4. **媒体消息默认校验缩略图**：ThumbnailFileId 校验失败不阻断主流程（仅主文件强校验）。
5. **旧 JSON 上传接口**（POST /api/files）直接移除而非 410 过渡（内部项目，客户端同步升级）。

---

---

## 一、背景与目标

### 1.1 背景

Message.Web.API 是即时通讯服务（会话/消息/群组/好友/Tweet），其文件能力按设计约定必须经由
FileDev.Web.API 的 FileStorage gRPC 服务提供（服务名 `filedev-web-api`，见
`Message.Web.API/Grpc/FileStorageGrpcClient.cs`）。当前代码已具备完整的 gRPC 客户端封装
（小文件上传、图片上传、分片断点续传、进度回调），但 **REST API 层并未正确使用它**，
存在上传链路断裂、附件模型冗余、下载绕过权限等问题。

### 1.2 目标

1. **文件上传唯一入口**：Message 侧任何文件写入（REST / SignalR）都必须经由 FileDev gRPC，
   消除"客户端任意传 URL"的旁路。
2. **附件与消息强绑定**：发送带附件的消息时服务端校验文件归属并自动建立附件记录，
   客户端不再拼装 MediaUrl/FileName/FileSize/MimeType。
3. **下载/预览受控**：附件下载由 Message 服务端流式代理（权限校验 + 下载计数），
   不再 302 裸跳转 FileDev 直链。
4. **通道归一**：REST 与 SignalR 共享同一套 gRPC 客户端、同一组 DTO、同一条命令链路。

---

## 二、现状诊断

### 2.1 现有端点清单

| 组 | 端点 | 现状问题 |
|----|------|----------|
| Messages | `POST /api/messages` 等 8 个 | 请求体 `MediaUrl` 为任意字符串，无归属校验（IDOR 面） |
| Sessions | `POST /api/sessions` 等 11 个 | 基本健康，参与者接口与 Groups 有重叠 |
| Groups | `POST /api/groups` 等 18 个 | 基本健康 |
| Friends | `POST /api/friends/request` 等 14 个 | 基本健康 |
| Tweets | `POST /api/tweets` 等 18 个 | `MediaUrls` 同为任意 URL 列表 |
| Comments | 4 个 | 健康 |
| Reports | 2 个 | 健康 |
| Audit | 5 个 | 健康 |
| **Files** | **16 个** | **问题集中区，见下** |

### 2.2 Files 组现存问题（本次重点）

1. **`POST /api/files` 不走 gRPC**：仅接收 `{MessageId, FileName, FileType, FileSize, FileUrl}`
   记录元数据到 `FileAttachment` 表，**FileUrl 由客户端任意提供**，与"上传必须经 FileDev gRPC"
   的约定直接矛盾；且 REST 通道没有 multipart/form-data 真实上传能力。
2. **下载裸跳转**：`GET /api/files/{id}/download` 302 重定向到 FileDev 的 `FileUri`，
   - 绕过 Message 侧会话成员权限校验；
   - `DownloadCount` 计数依赖客户端"跳转前先调接口"的时序，实际不可靠；
   - 私有文件（FileIdentity=FilePrivate）的鉴权无法在重定向中传递。
3. **附件与消息双重存储**：`Message` 实体自带 `MediaUri/ThumbnailUri/FileName/FileSize/MimeType`，
   `FileAttachment` 表又存一份（MessageId 关联）。一条文件消息可同时存在两处，无一致性约束。
4. **上传与发消息时序割裂**：`MergeChunksAsync` 返回 FileId/FileUri 后，客户端需再调
   `SendMessage` 手工拼 MediaUrl；没有任何端点把"上传完成 → 发消息"串起来。
5. **REST 分片通道用 JSON+Base64 传二进制**（`ChunkData` 为 `byte[]`），性能与体积均不理想。
6. **琐碎查询泛滥**：`/exists`、`/size`、`/type`、`/download-count` 四个端点各自一次 DB/远程调用，
   应合并进附件详情 DTO。
7. **上传元数据接口无当前用户上下文**：`UploadFileCommand` 未携带 UserId，
   与 gRPC 服务端（FileDev 按 user_id 归属）无法对齐。

### 2.3 可复用的既有资产

- `IFileStorageGrpcClient`：10 个方法的完整封装（重试、断点续传、进度回调），**保留不动**；
- `MessageHub` 的 6 个 chunk 方法（`InitChunkUpload/UploadChunk/GetChunkStatus/MergeChunks/CancelChunkUpload/ResumeChunkUpload`）
  + `ChunkUploadProgressReporter` 进度推送，**保留不动**；
- `Dto/Response/UploadResponses.cs` 的 7 个结果 record，**保留**（微调）；
- `Commands/Files/*` 中 5 个 chunk 命令（已注入 `IFileStorageGrpcClient`），**保留**。

---

## 三、设计原则

1. **FileDev 是文件唯一事实源（source of truth）**：文件二进制、元数据、归属、配额都在 FileDev；
   Message 只保存"附件引用"（FileId + 必要冗余字段）。
2. **不信任客户端**：所有 gRPC 调用的 `user_id` 一律取自 JWT（`ICurrentUserService`），
   请求体中不再出现 userId/任意 URL。
3. **资源化路由**：附件是消息的子资源（`/api/messages/{messageId}/attachments`），
   `/api/files` 降级为纯上传通道（不绑定消息）。
4. **命令链路唯一**：REST 端点、SignalR Hub 方法最终都汇入同一组
   `Commands/Messages` 与 `Commands/Files` 处理程序，禁止在 API 层重复实现业务。
5. **兼容优先，废弃显式**：旧字段保留一个发布周期，标记 `[Obsolete]` 并在文档中声明弃用时间。

---

## 四、总体 API 结构（重新设计后）

### 4.1 消息与附件（`/api/messages`）

| 方法 | 路由 | 说明 | 变更 |
|------|------|------|------|
| POST | `/api/messages` | 发送消息（含附件消息，见 §5.2） | **请求体重构** |
| GET | `/api/messages/{id}` | 消息详情（含附件列表） | 响应扩展 |
| GET | `/api/messages/sessions/{sessionId}/messages` | 会话消息分页 | 路由微调（原 `/sessions/{sid}/messages` 保留别名） |
| DELETE | `/api/messages/{id}` | 撤回消息（级联删除附件引用） | 级联逻辑 |
| POST | `/api/messages/{id}/forward` | 转发（附件引用随消息复制） | 级联逻辑 |
| PUT | `/api/messages/{id}/read` | 标记已读 | 不变 |
| GET | `/api/messages/search` | 搜索 | 不变 |
| GET | `/api/messages/unread` | 未读列表 | 不变 |
| **GET** | **`/api/messages/{id}/attachments`** | **消息附件列表** | **迁移自 `/api/files/message/{messageId}`** |
| **POST** | **`/api/messages/{id}/attachments`** | **给已发送消息补附件（fileId 列表）** | **新增** |
| **GET** | **`/api/messages/attachments/{attachmentId}`** | **附件详情**（含 size/type/downloadCount/mimeType/缩略图） | **合并旧 4 个琐碎查询** |
| **GET** | **`/api/messages/attachments/{attachmentId}/download`** | **流式下载（gRPC 代理）** | **替代 302 跳转** |
| **GET** | **`/api/messages/attachments/{attachmentId}/preview`** | **图片预览（可 resize，流式）** | **替代 302 跳转** |
| **DELETE** | **`/api/messages/attachments/{attachmentId}`** | **删除附件（级联 gRPC DeleteFile）** | **迁移自 `/api/files/{id}`** |

### 4.2 文件上传通道（`/api/files`，纯上传，不绑定消息）

| 方法 | 路由 | 说明 | 变更 |
|------|------|------|------|
| POST | `/api/files/upload` | 小文件上传（≤10MB，**multipart/form-data**）→ `UploadFileAsync` | **重写**（原 `POST /` JSON 元数据接口废弃） |
| POST | `/api/files/upload-image` | 图片上传（multipart，格式校验+尺寸解析）→ `UploadImageAsync` | **新增**（能力已有未暴露） |
| POST | `/api/files/chunk/init` | 初始化分片 → `InitChunkUploadAsync` | 保留（请求体补 `MessageId?` 预绑定） |
| POST | `/api/files/chunk/upload` | 上传分片（multipart 或原始二进制 body）→ `UploadChunkAsync` | 请求体改 IFormFile |
| POST | `/api/files/chunk/status` | 分片状态查询 → `GetChunkStatusAsync` | 保留 |
| POST | `/api/files/chunk/merge` | 合并分片 → `MergeChunksAsync` | 保留（响应已含 FileId/FileUri） |
| POST | `/api/files/chunk/cancel` | 取消分片 → `CancelChunkUploadAsync` | 保留 |
| POST | `/api/files/chunk/resume` | 断点续传（服务端补齐缺失分片） | 保留（SignalR 通道主用） |

> 废弃端点：`POST /api/files`（JSON 元数据）、`GET /api/files/{id}`、
> `GET /api/files/{id}/download`、`GET /api/files/{id}/preview`、`GET /api/files/{id}/exists`、
> `GET /api/files/{id}/download-count`、`GET /api/files/{id}/size`、`GET /api/files/{id}/type`、
> `GET /api/files/message/{messageId}` —— 全部由 §4.1 新端点替代。

### 4.3 SignalR Hub（`/MessageHub`）

| 方法 | 变更 |
|------|------|
| `InitChunkUpload / UploadChunk / GetChunkStatus / MergeChunks / CancelChunkUpload / ResumeChunkUpload` | 保留（与 REST 共用同一 `IFileStorageGrpcClient`） |
| `SendMessage` | 请求体同步重构（支持 `fileId` 附件字段） |
| **`SendFileMessage(sessionId, fileId, caption?)`** | **新增**：上传完成后一键发文件消息，内部走同一 SendMessage 命令链路 |
| `UploadProgress`（服务端→客户端推送） | 保留 |

### 4.4 其余组（Sessions/Groups/Friends/Tweets/Comments/Reports/Audit）

- 路由与语义**保持不变**（本次不动）；仅 Tweets 的 `MediaUrls` 请求字段按 §5.2 同步改为
  `fileIds`（服务端解析 FileDev 元数据），与消息附件同构。
- Tweet 图片同样支持 `thumbnailFileId`。

---

## 五、文件上传与附件集成设计（核心）

### 5.1 上传流程（小文件 / 图片）

```
客户端                     Message.Web.API                     FileDev.Web.API
  │  POST /api/files/upload      │                                    │
  │  (multipart: file, desc)     │  UploadFileCommand                 │
  │ ────────────────────────────>│  user_id ← JWT (ICurrentUserService)│
  │                              │ ── gRPC UploadFile ────────────────>│
  │                              │ <── FileId/FileUri/Md5/Size ───────│
  │ <── FileRef { fileId, fileUri, fileName, fileSize, fileMd5,        │
  │              mimeType }      │                                    │
```

- 请求：`multipart/form-data`，字段 `file`（IFormFile）、`description?`、`isPublic?`（默认私有）。
- 响应：统一 `FileRef`（见 §7），可直接用于发消息/发 Tweet。
- 超限：>10MB 返回 `413 Payload Too Large`，提示走分片通道（`MaxMessageSizeMb` 与 FileDev 对齐）。
- 图片：`upload-image` 追加 `validateFormat=true` 语义（服务端 gRPC 已带 3840×2160 上限）。

### 5.2 发送带附件的消息（核心变更）

**请求体（`SendMessageRequest` 重构）**：

```jsonc
{
  "sessionId": "guid",
  "messageType": "MessageFile",          // Image / Video / Audio / File
  "fileId": "filedev-guid",              // ← 来自上传响应的 FileRef.fileId（必须）
  "thumbnailFileId": "guid?",            // 可选：图片/视频缩略图
  "caption": "可选说明",                  // Image/Video 用
  "duration": 12.5,                      // Video/Audio 用
  "content": "文本",                     // 仅 MessageText
  "replyToMessageId": "guid?",
  // ↓ 以下旧字段删除或 [Obsolete]：MediaUrl / ThumbnailUrl / FileName / FileSize / MimeType
}
```

**服务端命令链路（`SendFileMessageCommandHandler` 伪代码）**：

```
1. 校验 sessionId 存在且当前用户是参与者
2. fileId → gRPC GetFileInfo（FileDev）
   ├─ 不存在/已删除            → 404
   ├─ file.user_id != 当前用户  → 403（防 IDOR：不能发别人的文件）
   └─ 通过                      → 取 fileName/fileSize/fileMd5/fileUri/fileType
3. MimeType ← 扩展名映射（FileStorageGrpcClient 已有 ResolveFileType，补 MimeType 映射工具）
4. Message.CreateFileMessage(sessionId, sender, fileUri, fileName, fileSize, mimeType)
   （Message 冗余字段由服务端填充，客户端不再提供）
5. FileAttachment.Create(messageId, fileId, fileName, fileType, fileSize, fileUri, thumbnailUri?)
   → 附件记录与消息同事务落库（Message 表 + FileAttachment 表各一条）
6. 发领域事件 → SignalR 推送（现有 MessageDeliveryService 链路不变）
```

**要点**：
- `fileId` 是唯一入参，**杜绝任意 URL**；消息的 `MediaUri` 是 FileDev 返回的 `FileUri`。
- 附件记录在**发送命令内自动创建**，`POST /api/messages/{id}/attachments` 仅用于
  "消息已发送后补附件"的罕见场景（同一校验链路）。
- Tweet 同构：`CreateTweetRequest.MediaUrls` → `FileIds`，服务端批量 `GetFileInfo` 校验归属。

### 5.3 下载 / 预览（流式代理，替代 302）

```
客户端                    Message.Web.API                       FileDev.Web.API
  │ GET .../attachments/{id}/download   │                             │
  │ ───────────────────────────────────>│ 1. 查 FileAttachment        │
  │                                    │ 2. 校验：消息存在 + 当前用户  │
  │                                    │    是会话参与者              │
  │                                    │ 3. 记录 DownloadCount        │
  │                                    │ 4. gRPC DownloadFile(流式) ─>│
  │                                    │ <── chunk_data 流 ───────────│
  │ <── 200 流式响应（Content-Type/    │                             │
  │      Content-Disposition 来自 gRPC）│                             │
```

- 响应头：`Content-Type`、`Content-Disposition: attachment; filename=...` 取自 gRPC 响应。
- 预览：`/preview?w=&h=` 走 `DownloadImage`（FileDev 支持 resize）；非图片返回 415。
- 私有文件鉴权在 FileDev 侧再次兜底（DownloadFileRequest.user_id 由服务端填当前用户）。

### 5.4 删除 / 清理

- `DELETE /api/messages/attachments/{attachmentId}`：
  1. 校验附件所属消息的会话成员权限（或消息发送者本人）；
  2. 附件记录软删（IsDeleted=true，保留下载计数历史）；
  3. 调 gRPC `DeleteFile(fileId, userId)` 清理 FileDev 物理文件。
- 消息撤回（`DELETE /api/messages/{id}`）：级联软删其附件引用（**不**删 FileDev 文件，
  因为转发/群聊可能共享引用——转发时复制 fileId 引用即可，FileDev 侧文件只删一次）。
- 补充：转发消息（`POST /{id}/forward`）仅复制 Message 行 + 附件**引用**（同一 fileId），
  不重新上传。

### 5.5 SignalR 通道

- 6 个 chunk 方法保持现状（已注入 `IFileStorageGrpcClient`，`userId` 取自
  `Context.UserIdentifier` / JWT）。
- 新增 `SendFileMessage(sessionId, fileId, caption?)`：Hub 内直接 `mediator.Send` 到
  §5.2 的同一命令，**避免 Hub 与 REST 双实现**。
- 附件消息的实时推送复用现有 `IMessageClient` 契约。

---

## 六、数据模型调整

### 6.1 FileAttachment（Message.Domain）

| 字段 | 变更 |
|------|------|
| `AttachmentId` | 不变（PK） |
| `MessageId` | 不变（FK → Message） |
| **`FileId`（Guid，新增）** | **FileDev 侧文件 ID，唯一索引；与 FileAttachmentId 解耦** |
| `FileName / FileSize / FileUri` | 不变（由服务端从 FileDev 填充） |
| `FileType` | string → 改为 `FileType` 枚举（与 proto 对齐）或保留 string 但服务端映射 |
| `MimeType` | 新增（当前实体已声明但构造函数未传，补全） |
| `ThumbnailUri` | 不变（来自 thumbnailFileId 的 FileUri） |
| `DownloadCount / DownloadTime` | 不变 |
| `IsDeleted` | 不变（软删） |

> Message 实体 `MediaUri/FileName/FileSize/MimeType` 冗余字段**保留**（历史/列表页免联表），
> 但语义改为"只读镜像"，写入仅允许发生在消息创建命令内。

### 6.2 新 DTO（`Dto/Response/UploadResponses.cs` 增补）

```csharp
/// <summary>文件上传/合并后的统一引用（消息与 Tweet 共用）</summary>
public record FileRef(
    Guid FileId,
    Uri FileUri,
    string FileName,
    long FileSize,
    string FileMd5,
    string MimeType,
    int? Width = null,
    int? Height = null);

/// <summary>附件详情（合并旧 exists/size/type/download-count 查询）</summary>
public record FileAttachmentDto(
    Guid AttachmentId,
    Guid FileId,
    Guid MessageId,
    string FileName,
    string FileType,
    long FileSize,
    string MimeType,
    Uri FileUri,
    Uri? ThumbnailUri,
    int DownloadCount,
    DateTime UploadTime);
```

---

## 七、安全设计（对照 FIX_CHECKLIST S-02/S-05/S-08）

| 威胁 | 措施 |
|------|------|
| X-User-Id / X-User-Roles 伪造（S-02） | 维持现有 JWT 优先策略；附件相关新端点只信任 `ICurrentUserService`（JWT） |
| 附件 IDOR（S-05） | 附件访问强制校验"消息 → 会话 → 参与者"链；gRPC GetFileInfo 校验 file.user_id == 当前用户 |
| 任意 URL 注入 | `SendMessageRequest` 移除 MediaUrl 等自由字段，只接受 fileId；服务端统一从 FileDev 取元数据 |
| 配额绕过 | 上传大小限制（413）+ FileDev 侧配额（S-09 已有）双保险；分片通道 chunk 大小服务端核定 |
| 下载滥用 | 下载代理记录计数，不再暴露 FileDev 直链（302 移除） |
| 文件类型绕过 | upload-image 强制 FileDev 格式校验；upload 侧按扩展名推断 MimeType 白名单 |

---

## 八、实施步骤（分阶段，可独立合入）

### 阶段一：上传通道重写（FilesApi 前半）
- [ ] 1. `POST /api/files/upload`、`POST /api/files/upload-image`：multipart 接收 →
      `IFileStorageGrpcClient.UploadFileAsync/UploadImageAsync` → 返回 `FileRef`
      （新建 `Commands/Files/UploadFileViaGrpcCommand` 或在 API 层直接调客户端——建议命令，保持 CQRS 一致）
- [ ] 2. 移除 `UploadFileRequest`（JSON 元数据版）与 `UploadFileCommand` 旧语义
- [ ] 3. `chunk/upload` 改 multipart/IFormFile（DTO 保留字段，API 层组装 byte[]）
- [ ] 4. 新增 `MimeTypeMap` 工具（扩展名 → MIME，供 §5.2 使用）

### 阶段二：附件模型与消息链路（核心）
- [ ] 5. `FileAttachment` 增加 `FileId` 列 + 迁移（Message.Infrastructure/Migrations）
- [ ] 6. `SendMessageRequest` 重构（fileId/thumbnailFileId/caption/duration，废弃 URL 字段）
- [ ] 7. `SendFileMessageCommandHandler`：GetFileInfo 校验 → 填充 Message → 同事务建附件
- [ ] 8. 消息附件资源端点迁移：`GET /api/messages/{id}/attachments`、
      `GET /api/messages/attachments/{attachmentId}`（合并琐碎查询）、
      `DELETE /api/messages/attachments/{attachmentId}`（级联 gRPC DeleteFile）
- [ ] 9. `POST /api/messages/{id}/attachments`（补附件）
- [ ] 10. Tweet `MediaUrls` → `FileIds`（同构改造）

### 阶段三：下载/预览代理 + Hub
- [ ] 11. `GET /api/messages/attachments/{attachmentId}/download`：权限校验 →
      记录计数 → gRPC 流式转发（`FileStorageGrpcClient` 需补 `DownloadFileAsync`/`DownloadImageAsync`
      两个流式方法，当前接口缺失！）
- [ ] 12. `preview`（resize 参数透传）
- [ ] 13. Hub 新增 `SendFileMessage`；`SendMessage` 请求体同步
- [ ] 14. 删除旧 `/api/files/{id}*` 全部端点

### 阶段四：收尾
- [ ] 15. 测试补强（Message.Tests）：上传命令、附件校验（归属 403 / 不存在 404）、
      下载代理、转发引用复制
- [ ] 16. 文档：Message.md 更新 + 本方案定稿
- [ ] 17. 全解决方案构建 0 警告 0 错误 + `dotnet test`

> 依赖缺口：`IFileStorageGrpcClient` 目前**没有** DownloadFileAsync / DownloadImageAsync
> 流式下载方法（proto 有 DownloadFile/DownloadImage 服务端实现，客户端未封装）——阶段三必须先补。

---

## 九、兼容性与废弃时间表

| 旧端点/字段 | 处理 |
|-------------|------|
| `POST /api/files`（JSON 元数据） | v2 移除；过渡期返回 410 Gone + 指引新端点 |
| `GET /api/files/{id}/download`、`/preview` | v2 移除（302 → 代理） |
| `GET /api/files/{id}`、`/exists`、`/size`、`/type`、`/download-count` | v2 移除（并入附件详情） |
| `GET /api/files/message/{messageId}` | v2 移除（→ `/api/messages/{id}/attachments`） |
| `SendMessageRequest.MediaUrl/ThumbnailUrl/FileName/FileSize/MimeType` | 标记 `[Obsolete]`，v2 移除 |
| 会话消息路由 `/sessions/{sid}/messages` | 保留别名至 v2，主路由改为 `/messages/sessions/{sid}/messages` |
| 分片 REST 通道 | 保留（JSON→multipart 是渐进式，先并后删） |

---

## 十、风险与决策记录

1. **FileId 与 AttachmentId 解耦**：转发/群聊共享 FileDev 文件，但每条消息有自己的附件记录。
2. **Message 冗余字段保留**：列表页免 join FileAttachment；一致性靠"仅命令内写入"约束。
3. **删除不级联删 FileDev 文件**（撤回场景）：共享引用安全优先；孤文件由 FileDev 侧
   定期清理（ChunkCleanupBackgroundService 已有分片清理，物理文件清理可后续补）。
4. **下载代理增加 Message 出口带宽**：可接受（当前规模），后续可在网关层加 CDN/直链签名。

---

*关联文档：Message.md（服务设计）、messageprogem.md（开发计划）、FIX_CHECKLIST.md（S-02/S-05/S-08/S-09）、PROJECT_ASSESSMENT_REPORT.md（§5 Message 群组）*
