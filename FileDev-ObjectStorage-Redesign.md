# FileDev 对象存储化改造设计文档（v0.1）

> 目标：将 FileDev 从"本地磁盘文件仓库"升级为 **S3 兼容对象存储服务**（MinIO 本地开发 / 云厂商生产），
> 对上层（HTTP API / gRPC / Message / Markdown / 前端）**零契约改动**。
>
> 适用范围：FileDev.Domain / FileDev.Infrastructure / FileDev.Web.API / NotBlog.AppHost

---

## 1. 现状诊断

| 层 | 现状 | 与对象存储化的关系 |
|---|---|---|
| 存储后端 | `NotFileStorageService`（497 行）全部**本地文件 IO**（`File.WriteAllBytes`/`FileStream`/`File.Delete`） | 核心改造点 |
| 抽象 | `INotFileStorageService` 接口已存在（保存/删除/读/流式读/分片/合并/清理） | ✅ 良好基础，上层已面向接口 |
| 注册 | `ModuleInitializer.cs` 硬编码 `AddScoped<INotFileStorageService, NotFileStorageService>()` | 需改为配置驱动 |
| `StorageType` | 枚举 13 种云厂商，但**全仓仅 1 处引用**（DTO 默认值 `Local`，实现不读取） | 需落地生效 |
| 分片 | 本地 `temp_chunks` 目录 + DB/Redis 记录（`FileChunkManager`，信号量防并发） | 需换 S3 Multipart |
| 秒传 | 客户端 SHA256（`expected_md5`）+ 去重查询 `/upload/check` | 保留（S3 无服务端校验） |
| 下载 | `GET /files/{**path}` 流式返回，**无 HTTP Range** | 需补 Range（视频拖动必需） |
| 配额 | PG 元数据层统计（50GB/用户） | 保留（S3 无配额概念） |
| 安全 | 扩展名白名单 + 图片真实性校验 + FileIdentity 权限 + 路径穿越防护 | 全部保留 |
| 部署 | 容器模式**无文件卷挂载**（StoragePath 在容器内，重启即失） | 对象存储天然解决 |
| 上层契约 | HTTP（上传/下载/分片/图片/文件组）+ gRPC（570 行）+ 集成事件 | 目标：零改动 |

---

## 2. 目标架构

```
┌────────────────────────────── 上层（零改动） ──────────────────────────────┐
│ HTTP API（直传/分片/流式/图片/下载/文件组）      gRPC（Message/Markdown）   │
└──────────────────────────────────┬────────────────────────────────────────┘
                                   ▼
                    ┌──────────────────────────────┐
                    │  NotFileStorageService（业务层，重构保留） │
                    │  路径安全/配额/白名单/哈希/图片校验/元数据  │
                    └──────────────────────────────┘
                                   ▼
                    ┌──────────────────────────────┐
                    │   IObjectStorageProvider（新抽象）  │
                    └──────────┬───────────┬───────┘
                               ▼           ▼
                   LocalObjectStorage  S3ObjectStorage
                   （现有 IO 提取）    （AWS SDK / MinIO / 云厂商 S3 兼容）
                   Storage:Provider=Local   Storage:Provider=S3
```

**核心原则**：
1. **业务层（校验/配额/元数据）与存储原语（IO）解耦**——现有 `NotFileStorageService` 拆两层
2. **object key = 现有 `fileRelativePath`**（`FileStorage/{userId}/{guid}.ext`）——**键零迁移**
3. PG 元数据表**零改动**（NotFile/NotFileGroup/FileChunkRecord 结构不变，分片 UploadId 走 Redis/客户端携带）
4. 配置切换即换后端，Local/S3 双实现并存，可回滚

---

## 3. 关键决策

### D1：抽象层级 —— 新增 `IObjectStorageProvider`（推荐 ✅）

- **不直接新写 `S3NotFileStorageService : INotFileStorageService`**（业务逻辑复制会双份维护）
- 从 `NotFileStorageService` 中**提取存储原语**到 `IObjectStorageProvider`：
  ```csharp
  public interface IObjectStorageProvider
  {
      Task WriteAsync(string key, Stream content, CancellationToken ct);          // 整文件写
      Task<Stream> ReadAsync(string key, long? offset, long? length, CancellationToken ct); // 范围读
      Task DeleteAsync(string key, CancellationToken ct);
      Task<bool> ExistsAsync(string key, CancellationToken ct);
      // S3 Multipart（Local 实现用本地分片目录模拟）
      Task<string> BeginMultipartAsync(string key, CancellationToken ct);          // 返回 uploadId
      Task UploadPartAsync(string key, string uploadId, int partNumber, Stream content, CancellationToken ct);
      Task CompleteMultipartAsync(string key, string uploadId, CancellationToken ct);
      Task AbortMultipartAsync(string key, string uploadId, CancellationToken ct);
      Task<string> CreatePreSignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct); // 可选
  }
  ```
- `NotFileStorageService` 保留：路径安全（GetSafeFullPathAsync 逻辑改为 key 清洗）、配额检查、
  白名单/图片校验、哈希校验、分片编排（调 provider）、DB 元数据落库

### D2：S3 客户端 —— AWS SDK for .NET（`AWSSDK.S3`）✅

- S3 协议事实标准：MinIO / 阿里云 OSS（兼容模式）/ 腾讯云 COS / AWS / 华为 OBS 均提供 S3 兼容端点
- `ForcePathStyle = true`（MinIO/自建必需；云厂商按需）
- 单 NuGet 依赖，社区成熟

### D3：分片上传 —— S3 Multipart Upload 原生

| 阶段 | Local 现状 | S3 改造后 |
|---|---|---|
| init | 建 `temp_chunks/{fileKey}/` 目录 + DB 记录 | `InitiateMultipartUpload` → 返回 `UploadId`（Redis 存 24h，客户端携带） |
| upload | 写本地分片文件 + DB/Redis 记录已传分片 | `UploadPart`（返回 ETag，客户端/服务端收集） |
| merge | 本地流式拼接 | `CompleteMultipartUpload`（PartNumber+ETag 列表） |
| cancel/过期 | 删目录 + ChunkCleanupBackgroundService | `AbortMultipartUpload`（清理服务同职责，调 provider） |

- 断点续传：`ListParts(uploadId)` 查询已传分片（替代本地状态查询 `/chunk/status`，接口保持）
- **分片大小注意**：S3 规定 ≥5MB（最后一片除外）——与现状 5MB 分片恰好一致 ✅
- `FileChunkRecord` 表**不加列**：UploadId 存 Redis（TTL=分片过期时间 24h）+ 客户端请求携带

### D4：下载 —— Range 支持（HTTP 206）

- `FileDownloadApi` 解析 `Range: bytes=start-end` → `provider.ReadAsync(key, offset, length)` → `206 Partial Content` + `Content-Range`
- S3 侧 `GetObjectAsync` 带 `ByteRange`；Local 侧 `FileStream` Seek
- 收益：视频拖动播放、大文件分块下载（现状缺失，对象存储化顺带补齐）

### D5：私有文件访问 —— 保持代理下载（推荐 ✅），预签名可选

- 现状：`GET /files/{**path}` 无鉴权（GUID 不可枚举），PRIVATE 文件下载走 gRPC 服务端校验
- 对象存储化后**保持代理下载**（FileDev 是唯一出口，FileIdentity 校验逻辑不变）——零安全模型变化
- 可选增强（二期）：PUBLIC 文件可配 CDN/预签名直链（`CreatePreSignedUrlAsync`），FileDev 返回 302

### D6：开发环境 —— AppHost 集成 MinIO 容器

```csharp
// AppHost.cs（新增）
var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithVolume("minio-data", "/data")              // 数据持久化（解决现状容器无卷问题）
    .WithEndpoint(port: 9000, targetPort: 9000, name: "s3")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin");
var filedev = builder.AddProject<Projects.FileDev_Web_API>("filedev")
    .WithReference(notfileDb)
    .WithReference(redis, connectionName: "CacheMemory")
    .WithReference(rabbitmq)
    .WithEnvironment("Storage__Provider", "S3")                 // 容器模式默认 S3
    .WithEnvironment("Storage__S3__Endpoint", "http://minio:9000")
    .WithEnvironment("Storage__S3__AccessKey", "minioadmin")
    .WithEnvironment("Storage__S3__SecretKey", "minioadmin")
    .WithEnvironment("Storage__S3__Bucket", "notblog");
```
- 单机 DEBUG（手动配置节）默认 `Provider=Local` 零改动

### D7：配置 —— `NotFileStorageOptions` 扩展

```jsonc
// appsettings.json 新增节（沿用 NotFileStorage 现有节，扩展字段）
"NotFileStorage": {
  "Provider": "Local",                    // Local | S3（默认 Local，兼容现状）
  "StoragePath": "FileStorage",           // Local 专用（保留）
  "TempPath": "temp_chunks",              // Local 专用（保留）
  "S3": {
    "Endpoint": "",                       // 空=AWS 默认端点（云厂商模式）
    "AccessKey": "",
    "SecretKey": "",
    "Region": "us-east-1",
    "Bucket": "notblog",
    "ForcePathStyle": true,               // MinIO/自建必填 true；云厂商 false
    "PreSignedExpiryMinutes": 60          // 可选预签名有效期
  },
  // ... 其余现有字段（分片大小/配额/白名单/哈希）不变
}
```

### D8：数据迁移 —— 一次性迁移工具（Local → S3）

- 实现：`FileDev.Infrastructure/Migration/LocalToS3MigrationService`（BackgroundService，配置 `Migration:Enabled=true` 时启动）
- 流程：枚举 `StoragePath` 下现有文件 → 逐文件 `WriteAsync(key)` → 成功后本地删除（或保留待确认）→ 日志+断点续传（按目录清单记录进度）
- 校验：上传后与 PG `NotFile.FileMd5` 比对（哈希一致才标记完成）
- 生产切换窗口：迁移完成 → 改配置 `Provider=S3` → 重启。**回滚**：改回 Local（双实现并存）
- 小规模也可停机窗口内直接迁移

---

## 4. 改造清单（按项目）

### FileDev.Domain
| 文件 | 改动 |
|---|---|
| `IServices/IObjectStorageProvider.cs` | **新建**：存储原语接口（D1） |
| `Options/NotFileStorageOptions.cs` | 加 `Provider` 枚举 + `S3Options` 子配置（Endpoint/AK/SK/Region/Bucket/ForcePathStyle/预签名过期） |
| `Enum/StorageType.cs` | 保留（映射 provider 类型：Local=0 现用，其余云厂商映射到 S3 兼容实现，加注释） |

### FileDev.Infrastructure
| 文件 | 改动 |
|---|---|
| `Service/LocalObjectStorageProvider.cs` | **新建**：从 `NotFileStorageService` 提取现有本地 IO（写/读/删/范围读/本地分片模拟） |
| `Service/S3ObjectStorageProvider.cs` | **新建**：AWS SDK 实现（Put/Get+Range/Delete/Multipart/预签名） |
| `Service/NotFileStorageService.cs` | 重构：业务层（路径清洗/配额/白名单/哈希/图片校验/元数据）调 provider；分片编排改调 `BeginMultipart/UploadPart/Complete/Abort` |
| `Service/FileChunkManager.cs` | UploadId 生命周期管理（Redis 存 uploadId+TTL）；`MarkChunkUploaded` 逻辑保留（幂等） |
| `ModuleInitializer.cs` | 按配置条件注册：`Local`→LocalProvider，`S3`→S3Provider（`AddScoped<IObjectStorageProvider, Xxx>()`） |
| `Migration/LocalToS3MigrationService.cs` | **新建**：迁移工具（D8） |

### FileDev.Web.API
| 文件 | 改动 |
|---|---|
| `APIs/FileDownloadApi.cs` | 加 HTTP Range 解析 → `provider.ReadAsync(offset, length)` → 206 |
| `APIs/FileChunkApis.cs` | init 返回 uploadId（S3 模式）；merge 收 PartNumber/ETag 列表；cancel 调 Abort |
| `Background/ChunkCleanupBackgroundService.cs` | S3 模式调 `AbortMultipartUpload`（本地模式保持删目录） |
| `Program.cs` / `appsettings.json` | 配置节 + 无代码改动（DI 在 Infrastructure） |

### NotBlog.AppHost
| 改动 |
|---|
| 新增 MinIO 容器（D6）+ FileDev `WithReference` + 环境变量注入 |

### 测试
| 类型 | 内容 |
|---|---|
| 单元 | provider 接口 mock；业务层（配额/白名单/哈希）不依赖真实后端 |
| 集成 | 本地 MinIO 容器：上传/分片/合并/下载 Range/删除/秒传 全链路 |
| 兼容 | Local provider 回归（现有测试零改动） |

---

## 5. 分阶段实施

| 阶段 | 内容 | 验收标准 |
|---|---|---|
| **P1 抽象抽取** | `IObjectStorageProvider` + `LocalObjectStorageProvider`（提取现有 IO）+ `NotFileStorageService` 重构为业务层 | 行为不变；现有测试全绿；分片/下载/秒传功能回归通过 |
| **P2 S3 后端** | `S3ObjectStorageProvider`（整文件读写删）+ 配置驱动 DI + AppHost MinIO 容器 | 本地起 MinIO：直传/下载/删除/流式读通过；`Provider=Local` 回归兼容 |
| **P3 分片与 Range** | S3 Multipart（init/upload/merge/cancel）+ 断点续传（ListParts）+ 下载 Range 206 | 大文件（>5MB）分片上传/断点续传/拖动下载通过；分片过期清理生效 |
| **P4 迁移与部署** | 迁移工具 + 云厂商接入指南（OSS/COS/AWS 配置差异）+ 预签名可选 | 存量数据迁移完成且哈希一致；生产切换与回滚流程验证 |

**建议顺序**：P1 → P2 → P3 各一个本地提交（每阶段可独立验证）；P4 视部署需要。

---

## 6. 兼容与风险

### 兼容性保证
- HTTP/gRPC 契约零改动（Message/Markdown/前端不受影响）
- PG 元数据表零改动（UploadId 走 Redis + 客户端携带，不加列）
- object key = 现有相对路径，无键迁移
- Local 模式完全保留（开发/单机/回滚）

### 风险与对策
| 风险 | 对策 |
|---|---|
| S3 分片最小 5MB 限制 | 与现状 5MB 分片一致；小文件直传不分片（现有逻辑已判断 totalChunks==1） |
| MinIO 与云厂商行为差异（端点样式/签名版本） | `ForcePathStyle` 配置化；集成测试覆盖 MinIO，云厂商按官方文档核对 |
| 大文件下载内存 | 全程流式（GetObjectStream/FileStream），禁止整读入内存 |
| 网络故障/超时 | AWS SDK 内置重试；`NotFileStorageResponse` 错误映射保持（上层已处理） |
| 迁移中断 | 迁移工具按清单断点续传 + 哈希校验，可重跑 |
| 预签名 URL 泄露（若启用） | 短过期（默认 60min）+ 仅 PUBLIC 文件启用 |

---

## 7. 验收与后续

- **P1-P3 完成即达"对象存储服务"标准**：S3 协议后端 + 分片 + Range + 元数据 + 配额 + 安全校验全链路
- 后续可选：多桶/多租户、CDN 加速、生命周期策略（冷热分层）、审计日志
