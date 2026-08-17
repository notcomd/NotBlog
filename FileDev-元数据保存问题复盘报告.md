# FileDev 文件元数据保存失败问题复盘报告

> 日期：2026-08-15
> 现象：**文件可以上传成功（物理文件落盘、接口返回成功），但元数据（NotFile / FileAttachment）无法保存（数据库 0 记录）**

## 一、现象与影响

- 调用链：Message（上传接口 / SignalR 分片 / 会话文件消息）→ gRPC `FileStorageServiceGRPC` → 命令 → **元数据应写入 FileDev 的 `NotFile` 表**（Message 侧再写 `FileAttachments`）；
- 实证结果：
  - FileDev `NotFile` 表：**0 记录**；`FileChunkRecord`：**0**；`ClientRequest`（幂等）：**0**；
  - Message `FileAttachments` 表：**0**；`Messages` 表：**0**；
  - 物理磁盘 `FileStorage` 目录：**25 个残留文件**（8/14 有 20 个、8/15 有 5 个）——上传尝试全部失败且补偿删除未生效；
  - gRPC 返回 `Success=true + FileId`（命令"成功"执行完）。

## 二、调用链梳理（Message → FileDev）

```
Message.Web.API                      FileDev.Web.API
┌──────────────┐   gRPC(HTTP/2)   ┌──────────────────────────────────────┐
│ FileStorage   │ ───────────────▶ │ FileStorageServiceGRPC（薄适配器）    │
│ GrpcClient    │                  │   └─ I SendAsync(命令)     │
└──────────────┘                  │        └─ 管道(应有)：                 │
                                  │             LoggerBehavior            │
                                  │           → TransactionBehavior       │
                                  │              ├─ BeginTransaction      │
                                  │              ├─ 命令 Handler：         │
                                  │              │  ① 物理写盘(存储层)      │
                                  │              │  ② CreateFileAsync      │
                                  │              │    = InsertFileAsync    │
                                  │              │      （仅加入跟踪器！）   │
                                  │              └─ CommitTransactionAsync │
                                  │                 = SaveChanges(提交)    │
                                  └──────────────────────────────────────┘
```

**关键设计**：`UploadFileCommandHandler` 的注释明确写着"**事务由 TransactionBehavior 统一处理**"——命令 Handler 只做 `InsertFileAsync`（把实体加入 EF ChangeTracker），**从不调用 SaveChanges**；提交动作全部依赖 `TransactionBehavior` 管道。

## 三、根因（实证定位）

**`TransactionBehavior` / `LoggerBehavior` 这两个管道从未在 DI 中注册，命令执行后没有任何事务、没有任何 SaveChanges 提交。**

证据链：

1. **代码**：`FileDev.Web.API` 定义了 `TransactionBehavior` / `LoggerBehavior`（`IPipelineBehavior` 实现），但全仓库 grep **没有任何注册代码**（如 `AddScoped(typeof(IPipelineBehavior<,>), ...)`）；
2. **NotMediator 包行为**（反射 + 行为测试）：`AddNotMediator` **只注册 `INotMediator`（Singleton）**，`pipeline 注册数: 0`；包 README 明确写着"**管道需要手动注册**"——FileDev 漏掉了这一步；
3. **现象自洽**：
   - 物理文件写入成功（存储层独立于数据库事务）✓
   - 命令"执行成功"（没有异常、没有提交）→ gRPC 返回成功 ✓
   - `NotFile` 实体只存在于 ChangeTracker，随请求 scope 释放而**丢弃** → 数据库 0 记录 ✓
   - 无异常 → `UploadFileCommandHandler` 的"元数据失败补偿删物理文件"逻辑**永不触发** → 文件残留 ✓
   - 分片/幂等记录同样 0 → **所有依赖管道的命令都没落库**（唯一 1 条 `NotFileGroup` 来自显式调用 `SaveEntitiesAsync` 的命令）✓
4. **排除法复现**：直连数据库 + 真实仓储/实体，INSERT、事件处理器、`AddFile` 全部正常——代码逻辑本身通；差异只在真实运行环境的 DI 配置。

## 四、次要问题（同根因链）

**上传事件的文件组关联丢失**：`UploadNotFileEvent` 由 Mediator **异步后台**（Channel）分发，且 handler 由**根容器**解析（与请求事务的 DbContext 非同一实例）——handler 内 `rootGroup.AddFile(...)` 的修改不在任何事务里，**从未落库**（实测根组 `FileIds` 始终为 `[]`）。

## 五、修复（已实施并验证）

| 文件 | 修改 |
|---|---|
| `FileDev.Web.API/Program.cs` | 注册管道：`AddScoped(typeof(IPipelineBehavior<,>), LoggerBehavior<,>)` + `TransactionBehavior<,>`（顺序：日志 → 事务） |
| `FileDev.Web.API/Application/DomainEventHandlers/UploadNotFileEventHandler.cs` | `AddFile` 后显式 `SaveEntitiesAsync`（事件异步分发，修改不在请求事务内，必须自行保存） |

**验证**：
- `FileDev.Web.API` 构建通过（0 警告 0 错误）；
- 实证：模拟真实 DI（`AddNotMediator` + 本次注册的管道 + 真实命令 Handler + 真实 DbContext），Handler 只 `InsertFileAsync` 不保存 → 命令执行后 **`NotFile` 成功落库 1 条**（验证数据已清理）。

## 六、遗留问题与建议

1. **重启 FileDev.Web.API 后修复生效**（当前运行的是旧代码）；重启后建议实测一次上传，并确认 `NotFile` 落库、根组 `FileIds` 更新；
2. **残留物理文件**：`FileDev.Web.API\bin\Debug\net10.0\FileStorage` 下 25 个孤儿文件（8/14~8/15 失败上传的），确认无误后可清理；
3. **同源隐患检查**：
   - `Identity` / `Markdown` / `Video` 同样定义了 `TransactionBehavior` 且未见注册代码——但 Identity 有落库数据（User 2 条、Roles 5 条），说明其 handler 走的是显式保存或另有机制；**Markdown 的 `MarkDown`/`MarkReview` 表为 0 记录**，建议按本报告同样排查其保存路径；
   - **Message 模块不受影响**：其命令 Handler 均为显式 `SaveEntitiesAsync`（与 FileDev 的"依赖管道保存"风格不同）；
4. **深层架构风险**：NotMediator 的事件分发为异步后台 + 根容器解析 Scoped handler（Singleton 化 DbContext）——并发上传时后台任务共享同一 DbContext 存在并发冲突隐患；建议后续将领域事件的副作用改为"同事务同步执行"或"独立 DbContext 工厂"模式；
5. **建议**：为 `NotFileService.CreateFileAsync` 等"必须落库"的写路径增加防御性显式保存（或校验），避免再次出现"静默不落库"。
