# FileDev 对象存储化改造设计文档（v0.2 自研内核版）

> **v0.2 修订说明**：按用户要求**不依托外部服务**（废弃 v0.1 的 MinIO/AWS SDK/云厂商方案），
> 在 FileDev 内**自研对象存储内核**（内容寻址 + 分块存储 + 清单索引 + 引用计数）。
> 目标不变：对上层（HTTP API / gRPC / Message / Markdown / 前端）**零契约改动**；
> 新增：零外部依赖、块级去重、随机读（Range）、数据可水平扩展。
>
> 适用范围：FileDev.Domain / FileDev.Infrastructure / FileDev.Web.API / NotBlog.AppHost

---

## 1. 现状诊断

| 层 | 现状 | 说明 |
|---|---|---|
| 存储后端 | `NotFileStorageService`（497 行）全部本地文件 IO（整文件写入/流式读/删除） | 核心改造点 |
| 抽象 | `INotFileStorageService` 接口已存在（上层已面向接口） | ✅ 良好基础 |
| 注册 | `ModuleInitializer.cs` 硬编码 `AddScoped<INotFileStorageService, NotFileStorageService>()` | 配置驱动化 |
| 分片上传 | 本地 `temp_chunks` + DB/Redis 记录（`FileChunkManager`） | 与内核统一 |
| 秒传 | 客户端 SHA256（`expected_md5`）+ 去重查询 | 保留，内核天然支持 |
| 下载 | 整文件流式返回，**无 HTTP Range** | 内核分块后天然支持 |
| 配额/安全 | PG 层配额 + 白名单 + 图片校验 + FileIdentity | 全部保留 |
| 部署 | 容器模式**无文件卷挂载**（重启丢文件） | 需补卷 |
| 现状缺陷 | **无去重**（同内容多份占用）；大文件随机读整文件 IO | 内核解决 |

---

## 2. 目标架构：自研对象存储内核

```
┌────────────────────────────── 上层（零改动） ──────────────────────────────┐
│ HTTP API（直传/分片/流式/图片/下载/文件组）      gRPC（Message/Markdown）   │
└──────────────────────────────────┬────────────────────────────────────────┘
                                   ▼
                    ┌────────────────────────────────────────┐
                    │   NotFileStorageService（业务层，重构保留） │
                    │   路径清洗/配额/白名单/哈希/图片校验/元数据   │
                    └────────────────────────────────────────┘
                                   ▼
                    ┌────────────────────────────────────────┐
                    │      IObjectStorageProvider（新抽象）      │
                    └────────────────────────────────────────┘
                                   ▼
                    ┌────────────────────────────────────────┐
                    │  ChunkedObjectStorageProvider（自研内核）  │
                    │  内容寻址分块 + 清单索引 + 引用计数 + Range │
                    └────────────────────────────────────────┘
                                   ▼
                          ┌────────────────────┐
                          │  本地磁盘（多盘可扩展） │
                          │  data/ {hash前2}/   │
                          └────────────────────┘
```

### 2.1 对象布局（混合布局，核心设计）

**对象 = 元数据（PG NotFile 表，已有）+ 内容（分块存储）**

```
StoragePath/
├── data/                          # 内容寻址数据区（块级去重）
│   ├── ab/                        # 哈希前 2 位分桶（目录扇出，避免单目录过万）
│   │   ├── abc123...def.blk       # 块文件：文件名 = 块内容 SHA256（内容寻址）
│   │   └── abc123...def.ref       # 块引用计数文件（被多少对象引用）
│   └── ...
├── manifests/                     # 大对象清单区（对象 → 块序列）
│   └── {userId:N}/{guid:N}.json   # [{"hash":"...", "size":4194304}, ...] + 对象总大小/校验和
└── (旧布局 FileStorage/ 保留，兼容读)  # v0.1 存量文件（双读兼容，见 D8）
```

**小对象（≤ 块大小，默认 4MB）**：单块，直接存 `data/{hash前2}/{hash}.blk`，**无需清单**——对象内容即一个块，读=读块文件。

**大对象（> 块大小）**：切分为 N 块存 `data/` + 写 `manifests/` 清单（块哈希序列 + 总大小 + 全文 SHA256）。
- 块索引在清单文件，**PG 零改动**（NotFile 表不加列；清单文件随对象生命周期管理）

### 2.2 核心操作流程

| 操作 | 流程 |
|---|---|
| **写（Save）** | 内容 → 切块（每块 4MB）→ 逐块写 `data/{h2}/{hash}.blk`（**已存在则跳过**=去重）→ 大对象写清单 → PG 元数据（FileMd5=全文哈希，已有）→ 块引用计数 +1 |
| **读（Get）** | 小对象：读块文件；大对象：读清单 → 定位目标块 → 流式拼接（或 Range 只读目标块） |
| **范围读（Range）** | 清单二分定位目标块 → 读块 → 块内偏移裁剪 → 返回范围（206） |
| **删（Delete）** | PG 元数据软删（现有逻辑）→ 块引用计数 -1 → 归零删块文件（+ 删清单） |
| **分片上传** | 与内核统一：分片落 `data/`（每片即一块，天然去重）→ merge = 按序收集块哈希写清单（大对象）或合并校验（小对象）——`temp_chunks` 目录可废弃 |
| **秒传** | 客户端 SHA256 → 查块是否存在（`data/{h2}/{hash}.blk`）→ 存在即秒传（覆盖现有 `/upload/check`） |

### 2.3 为什么这是"对象存储"

| 对象存储特性 | 自研内核对应 |
|---|---|
| 对象键寻址（bucket/key） | `fileRelativePath`（现有语义不变） |
| 内容寻址 / 块级去重（CAS） | 块文件名 = 内容 SHA256 |
| 分块存储 + 随机读 | 块 + 清单，Range 只读目标块 |
| 元数据与数据分离 | PG（元数据） + data/manifests（数据） |
| 水平扩展 | `data/` 可挂多盘/多目录（按 key 哈希路由，二期） |
| 引用计数（GC） | 每块 `.ref` 文件，归零回收 |

---

## 3. 关键决策

### D1：抽象层级 —— `IObjectStorageProvider`（与 v0.1 一致）

从 `NotFileStorageService` 提取存储原语：
```csharp
public interface IObjectStorageProvider
{
    Task WriteAsync(string key, Stream content, CancellationToken ct);              // 整对象写（内部切块）
    Task<Stream> ReadAsync(string key, long? offset, long? length, CancellationToken ct); // 范围读
    Task DeleteAsync(string key, CancellationToken ct);
    Task<bool> ExistsAsync(string key, CancellationToken ct);
    // 分片（内核统一：分片=块写入，无需 S3 Multipart 语义）
    Task WritePartAsync(string key, int partNumber, Stream content, CancellationToken ct);
    Task CompletePartsAsync(string key, int totalParts, string? expectedHash, CancellationToken ct);
    Task AbortPartsAsync(string key, CancellationToken ct);
}
```
`NotFileStorageService` 保留业务层：路径清洗（key 清洗）、配额、白名单/图片校验、哈希校验、元数据落库。

### D2：块大小与数据区布局

- **块大小默认 4MB**（与现有分片 5MB 解耦；块小→去重粒度细；可配置 `BlockSize`）
- 目录扇出：`data/{sha256 前 2 位}/`（256 桶，单桶上限可控）
- 写顺序保证一致性：**先块（数据）→ 再清单（大对象索引）→ 最后 PG 元数据**；读时 PG 有记录但清单/块缺失 → 404（元数据后写保证不出现"有元数据无数据"）
- 块文件写后**读回校验哈希**（与文件名比对，防写损坏）→ 失败重写/报错（写放大可接受，块小）

### D3：去重与引用计数

- 块文件幂等写：同哈希已存在 → 跳过写，引用计数 +1（`.ref` 原子自增：`File.ReadAllText+1` 后 `File.WriteAllText` 临时文件替换，或简单互斥锁）
- 删除对象 → 逐块 `ref -1` → 0 时删除 `.blk` + `.ref`
- 引用计数并发：按块哈希取进程内 `ConcurrentDictionary<string, SemaphoreSlim>` 互斥（仿 `FileChunkManager` 已有模式）
- 孤儿块兜底：后台任务扫描 `data/` 中 `.ref == 0` 或超时未完成写入的块（复用 ChunkCleanupBackgroundService 框架）

### D4：Range 下载（HTTP 206）

- `FileDownloadApi` 解析 `Range: bytes=start-end` → `provider.ReadAsync(key, offset, length)`：
  - 小对象：`FileStream.Seek` 直接范围读
  - 大对象：清单定位目标块（块内偏移裁剪），只打开目标块文件
- 返回 `206 Partial Content` + `Content-Range`；无 Range 头时整对象流式（沿用现有流式拼接，块间顺序读）
- 收益：视频拖动、断点续传下载

### D5：安全 —— 与现状一致

- 下载仍走代理（FileDev 唯一出口，FileIdentity 校验不变）
- 块文件名是内容哈希（不可猜测），`data/` 目录不对外暴露（无静态文件映射）
- 白名单/图片校验在业务层（写入前），不变

### D6：部署 —— 无新增容器，补数据卷

- **不引入 MinIO/任何外部服务**（v0.2 核心约束）
- AppHost：FileDev 容器挂数据卷（解决现状"容器模式重启丢文件"）：
  ```csharp
  var filedev = builder.AddProject<Projects.FileDev_Web_API>("filedev")
      .WithReference(notfileDb).WithReference(redis, connectionName: "CacheMemory").WithReference(rabbitmq)
      .WithVolume("filedev-storage", "/app/FileStorage");   // 数据卷持久化
  ```
- 单机 DEBUG：`NotFileStorage:StoragePath` 默认相对路径（与现状一致，零改动）

### D7：配置 —— `NotFileStorageOptions` 扩展

```jsonc
"NotFileStorage": {
  "StoragePath": "FileStorage",        // 根路径（含 data/ 与 manifests/ 子区）
  "BlockSize": 4194304,                // 块大小（默认 4MB）
  "DataDirectory": "data",             // 数据区子目录
  "ManifestDirectory": "manifests",    // 清单区子目录
  // ... 其余现有字段（分片大小/配额/白名单/哈希/过期时间）不变
}
```

### D8：兼容与迁移 —— 双读兼容，零停机

- **旧布局（Flat 文件 `FileStorage/{userId}/{guid}.ext`）保留可读**：`ReadAsync` 先查新布局（清单/块），miss 后回退旧路径直接读——**存量文件零迁移即可继续服务**
- 懒迁移（可选后台任务）：按 PG NotFile 表枚举存量 → 逐个写入新布局（切块+清单）→ 成功后旧文件删除/保留
- 写路径只写新布局；`Provider` 概念简化为 `LayoutVersion`（v1=Flat / v2=Chunked，默认 v2，v1 只读兼容）——**无需双实现切换，单实现内双读**
- 回滚：新布局出问题 → 配置切回 v1 只读旧文件（元数据不变）

---

## 4. 改造清单（按项目）

### FileDev.Domain
| 文件 | 改动 |
|---|---|
| `IServices/IObjectStorageProvider.cs` | **新建**：D1 原语接口 |
| `Options/NotFileStorageOptions.cs` | 加 `BlockSize`/`DataDirectory`/`ManifestDirectory`/`LayoutVersion` |
| `ValueObjects/ObjectManifest.cs` | **新建**：大对象清单模型（块哈希序列/大小/全文 SHA256） |

### FileDev.Infrastructure
| 文件 | 改动 |
|---|---|
| `Service/ChunkedObjectStorageProvider.cs` | **新建（核心）**：切块写/范围读/删除/引用计数/清单读写/旧布局双读回退 |
| `Service/NotFileStorageService.cs` | 重构：业务层（配额/白名单/哈希/图片校验/元数据）调 provider；分片编排改调 `WritePart/CompleteParts/AbortParts` |
| `Service/FileChunkManager.cs` | 简化：分片状态仍用 DB/Redis（幂等），但落盘走内核（每片=一块）；`temp_chunks` 目录废弃 |
| `ModuleInitializer.cs` | 注册 `ChunkedObjectStorageProvider`（单实现，无需条件切换） |
| `Background/ChunkCleanupBackgroundService.cs` | 扩展：孤儿块/超时块回收（引用计数为 0 的 `.blk`） |
| `Migration/`（可选） | 懒迁移后台任务（D8） |

### FileDev.Web.API
| 文件 | 改动 |
|---|---|
| `APIs/FileDownloadApi.cs` | Range 解析 → 206（D4） |
| `APIs/FileChunkApis.cs` | init/merge 语义对齐内核（merge=CompleteParts，含哈希校验） |
| `appsettings.json` | 新配置节字段（D7） |

### NotBlog.AppHost
| 改动 |
|---|
| FileDev 容器补数据卷挂载（D6）；**无新增容器/服务** |

### 测试
| 类型 | 内容 |
|---|---|
| 单元 | 块切分/清单读写/引用计数/范围定位（纯逻辑，不落盘） |
| 集成 | 临时目录内核全链路：小/大对象写读、去重（同内容二次写不增块）、Range、删除回收、旧布局双读回退 |
| 兼容 | 现有 FileDev 测试与全仓回归（HTTP/gRPC 契约不变） |

---

## 5. 分阶段实施

| 阶段 | 内容 | 验收标准 |
|---|---|---|
| **P1 抽象抽取** | `IObjectStorageProvider` + 提取现有 IO 为临时实现（行为不变） | 现有测试全绿；直传/分片/下载/秒传回归通过 |
| **P2 内核** | `ChunkedObjectStorageProvider`：切块/内容寻址/清单/引用计数 + 写路径切换 | 小/大对象写读通过；同内容去重（块数不增）；删除引用计数归零回收 |
| **P3 能力补齐** | Range 下载 206 + 分片与内核统一（废弃 temp_chunks）+ 孤儿块清理 | 视频拖动/断点下载；分片上传走内核且去重；清理任务回收孤儿块 |
| **P4 兼容收尾** | 旧布局双读回退 + 懒迁移任务 + AppHost 数据卷 | 存量文件可读（双读）；迁移后哈希一致；容器重启数据保留 |

**建议顺序**：P1 → P2 → P3 各一个本地提交（每阶段独立验证）；P4 视部署需要。

---

## 6. 兼容与风险

### 兼容性保证
- HTTP/gRPC 契约零改动（Message/Markdown/前端不受影响）
- PG 元数据表**零改动**（清单在文件系统，不加表不加列）
- object key（fileRelativePath）语义不变
- 存量 Flat 文件**双读兼容**，零迁移窗口即可上线；懒迁移可选

### 风险与对策
| 风险 | 对策 |
|---|---|
| 块引用计数并发竞态 | 按块哈希进程内信号量互斥（仿 FileChunkManager 模式）；`.ref` 写用临时文件+原子替换 |
| 清单与数据不一致（写中断） | 写顺序：块 → 清单 → 元数据；孤儿块由后台任务回收；元数据后写保证无"空元数据" |
| 单机磁盘容量/性能 | 数据区可挂多盘（data 目录配置化，二期按 key 路由）；块小便于局部读 |
| 写放大（读回校验） | 仅新块校验（已存在跳过）；块 4MB 校验成本可控 |
| 大对象清单文件丢失 | 清单可重建：扫描对象全部块（块哈希可枚举 data/）+ 与 PG FileMd5 比对（懒修复，二期） |
| 删除后孤儿块 | 引用计数 + 后台扫描兜底（.ref 缺失视为 0 可回收，双保险） |

---

## 7. 验收与后续

- **P1-P3 完成即达"自研对象存储"标准**：内容寻址去重 + 分块 + 随机读 + 元数据 + 配额 + 安全全链路，零外部依赖
- 后续可选：多盘路由（shard）、版本管理（清单快照）、生命周期策略（冷数据压缩/迁移）、统计报表（去重率/容量）
