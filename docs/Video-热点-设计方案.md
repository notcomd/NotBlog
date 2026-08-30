# Video 服务：视频热点榜 —— 设计方案

> 版本：v1.0　日期：2026-08-29　状态：待评审
> 范围：Video 服务（热度计算公式 + Redis ZSet 热点榜 + 定时收敛 + 热点榜 API）
> 参照：`Markdown` 服务既有热点实现（`MarkdownHeatFormula` + `MarkdownHotBoardService` + `MarkdownHeatRebuildBackgroundService`），本方案逐条对齐其架构模式，并按视频内容特征调整公式参数
> 关联文档：`Video-互动计数与作者通知-实施文档.md`（Stars 收藏计数接入为热度输入的前提）、`Markdown-接口文档.md`（热点榜接口形态参考）

---

## 1. 背景与目标

Video 服务当前只有列表/分页/名称模糊搜索（[VideoEndpoints.cs](file:///f:/NotBlog/Video.Web.API/Apis/VideoEndpoints.cs#L15-L29)），**无任何热点/热度能力**——既无热度分字段，也无排序接口。相比之下 Markdown 服务已具备完整热点闭环（公式 → DB HeatScore 列 → Redis ZSet 榜单投影 → 写侧实时刷新 → 定时重建兜底），架构经验可直接复用。

本次设计目标：

- 基于**现有可计数数据源**（`VideoQuote`：Upvote / Stars / Watch / Ballot / Share）计算视频热度分；
- 提供**热点榜接口**（TopN 排行，Redis ZSet 直读，秒级响应）；
- 提供**写侧实时刷新 + 定时重建兜底**的双通道一致性保障（与 Markdown 一致）；
- Redis 不可用时自动**降级 DB 实时计算**，服务不中断；
- 曝光筛选与现有列表一致：仅 **公开（VideoPublic）+ 未删除 + 已展示（VideoDisplay）** 的视频上榜。

> 前置依赖：收藏计数（Stars）目前无写入口，见 `Video-互动计数与作者通知-实施文档.md` §6.1 —— 热度交互输入含 Stars，需先完成该接入。

## 2. 需求确认与决策记录

| # | 决策点 | 结论 | 说明 |
|---|--------|------|------|
| D-1 | 热度公式权重 | **互动 50% / 浏览 30% / 时间衰减 20%**（与 Markdown 一致） | 交互权重最高，防纯播放刷榜 |
| D-2 | 交互子项权重 | `Upvote×1 + Stars×2 + Share×3 + Ballot×5`（与 Markdown 对齐） | 投币/分享/收藏权重高于点赞 |
| D-3 | 视频半衰期 | **2 天**（Markdown 为 7 天） | 视频消费周期短、时效性强，前 2 天是流量主窗口 |
| D-4 | 评论是否计入热度 | **不计入**（本期） | 交互子项仅点/藏/传/币；评论互动量级大，单独建模留待二期 |
| D-5 | 热点分存储 | `Videos.HeatScore`（double 列，默认 0），公式重算后落 DB | 对齐 Markdown 的 `MarkQuote.HeatScore`，DB 为唯一事实源 |
| D-6 | 榜单存储 | **Redis ZSet 投影**（key=`video:hot:all`，member=VideoGuid，score=HeatScore） | PG 是事实源，ZSet 是**可降级投影**，Redis 故障回 DB 实时计算 |
| D-7 | 写侧刷新时机 | 点赞/投币/收藏（增删）/分享/观看完成后同步调用 `UpdateScoreAsync` | 与 Markdown 一致；观看已被 5 分钟窗口去重，频率可控 |
| D-8 | 榜单接口 | `GET /api/video/hot?take=10`，返回 TopN（按 HeatScore 降序） | 复用 `VideoResult<T>` 包装与既有鉴权/脱敏管道 |
| D-9 | 实现位置 | 榜单一键快照模式（HotBoardService），不改造 `PageByVideoAsync` 默认排序 | 现有分页保持原状，热点能力独立成服务 |
| D-10 | 定时重建 | 每 10 分钟全量重算（首次启动延迟 1 分钟）；`Interlocked` 防重入 + Redis SetNX 锁单飞 | 与 `MarkdownHeatRebuildBackgroundService` 完全同构 |

## 3. 与 Markdown 热点实现的异同

| 维度 | Markdown（现有） | Video（本方案） |
|------|------------------|-----------------|
| 公式 | `MarkdownHeatFormula` | `VideoHeatFormula`（对齐命名与结构） |
| 半衰期 | 7 天 | **2 天**（视频时效性强） |
| 计数输入 | `MarkQuote`（Love/Favorite/Share/Coin/View） | `VideoQuote`（Upvote/Stars/Share/Ballot/Watch） |
| 实体热度分 | `MarkQuote.HeatScore` | `Videos.HeatScore`（新增列） |
| 榜单 ZSet key | `markdown:hot:all` | `video:hot:all` |
| 重建锁 key | `markdown:hot:rebuild:lock` | `video:hot:rebuild:lock` |
| 曝光筛选 | `IsDelete + MarkApproved` | `!VideoDelete + VideoPublic + VideoDisplay` |
| 服务/任务 | `MarkdownHotBoardService` + Background | `VideoHotBoardService` + `VideoHeatRebuildBackgroundService`（同构） |
| 写侧钩子 | 文档交互端点 | 点赞/收藏/投币/分享/观看命令端点 |

架构骨架完全复用 Markdown 方案，**不发明新模式**。

## 4. 总体设计

### 4.1 数据流

```
        写侧命令(点赞/投币/收藏/分享/观看)
                 │ UpdateScoreAsync（实时单条）
                 ▼
   ┌─────────────────────────────┐    ┌──────────────────────────────┐
   │ PostgreSQL（唯一事实源）       │───►│ Redis ZSet 投影 video:hot:all │
   │ Videos.HeatScore + VideoQuote│    │ (member=VideoGuid, score)    │
   │                             │◄───│                              │
   │ 定时重建(10min)兜底收敛       │    │ 读取：ZRevRange 直读（秒级）    │
   └─────────────────────────────┘    └──────────────────────────────┘
        ▲                                        │ Redis 不可用
        │ DB 降级：实时全量重算                    ▼
        └───────────────────── GetHotBoardAsync: DB 实时计算 TopN ──┘
```

- **写路径**：互动命令成功 → `UpdateScoreAsync(videoGuid)`：加载实体 → `RecalculateHotScore(now)` → 落 HeatScore → 写 ZSet 单条；
- **读路径**：`GetHotBoardAsync(take)`：ZRevRange 直读（空榜→单飞重建后重读）→ 按 Guid 批量回查元数据保序返回；Redis 异常 → DB 实时计算降级；
- **收敛路径**：后台服务每 10 分钟 `RebuildAsync` 全量重算（DB 为真实评分 + ZSet 重建），修正写侧钩子遗漏/失败的偏差。

### 4.2 热度公式（VideoHeatFormula）

```csharp
// Video.Domain/Heat/VideoHeatFormula.cs
/// <summary>
/// Video 热度分计算公式（互动 50% / 浏览 30% / 时效 20%，视频半衰期 2 天）。
///     HeatScore   = 0.5 × Interaction + 0.3 × View + 0.2 × Freshness
///     Interaction = log10(1 + Upvote×1 + Stars×2 + Share×3 + Ballot×5)
///     View        = log10(1 + Watch)
///     Freshness   = exp(-ageDays / 2)
/// </summary>
public static class VideoHeatFormula
{
    public const double InteractionWeight = 0.5;   // 互动权重（50%）
    public const double ViewWeight        = 0.3;   // 浏览权重（30%）
    public const double FreshnessWeight   = 0.2;   // 时效权重（20%）
    public const double HalfLifeDays      = 2;     // 视频半衰期 2 天（D-3）

    public static double Calculate(VideoQuote quote, DateTimeOffset createdAt, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(quote);

        var interaction = Math.Log10(1 + quote.Upvote * 1
                                       + quote.Stars * 2
                                       + quote.Share * 3
                                       + quote.Ballot * 5);

        var view = Math.Log10(1 + quote.Watch);

        var ageDays = Math.Max(0, (now - createdAt).TotalDays);
        var freshness = Math.Exp(-ageDays / HalfLifeDays);

        return InteractionWeight * interaction
               + ViewWeight * view
               + FreshnessWeight * freshness;
    }
}
```

特性说明：
- **对数压缩**：防刷——播放量从十万到百万只带来稳定小幅上升；
- **指数衰减**：第 1 天新鲜度 ≈ 0.61，第 2 天 ≈ 0.37，第 7 天 ≈ 0.03（新视频窗口期占优，老视频靠互动/播放维持排名）；
- **纯函数、无副作用**，便于单测与降级路径复用。

### 4.3 实体扩展

```csharp
// Video.Domain/Entities/Video.cs（增量）
/// <summary>最近一次热度分（DB 持久化；ZSet 投影的数据源）</summary>
public double HeatScore { get; private set; }

/// <summary>重算并持久化热度分；返回最新值（写侧钩子与定时重建共用）</summary>
public double RecalculateHotScore(DateTimeOffset now)
{
    HeatScore = VideoHeatFormula.Calculate(VideoQuote, TimeSpace.CreateAt, now);
    return HeatScore;
}
```

### 4.4 榜单服务契约

```csharp
// Video.Domain/Server/IVideoHotBoardService.cs
/// <summary>视频热点榜服务（Redis ZSet 直读 + 单飞重建 + DB 降级）</summary>
public interface IVideoHotBoardService
{
    /// <summary>获取热点榜 TopN（HeatScore 降序；Redis 不可用自动降级 DB 实时计算）</summary>
    Task<List<VideoHotItem>> GetHotBoardAsync(int take, CancellationToken ct = default);

    /// <summary>写侧钩子：互动后实时刷新单视频热度分（失败仅记日志，定时重建兜底）</summary>
    Task UpdateScoreAsync(Guid videoGuid, CancellationToken ct = default);

    /// <summary>全量重建：DB 重算全部热度分 + 重建 Redis ZSet（防重入）</summary>
    Task RebuildAsync(CancellationToken ct = default);
}

/// <summary>热点榜条目（响应模型）</summary>
public record VideoHotItem(
    Guid VideoGuid, string VideoName, Uri VideoCover,
    double HeatScore, long WatchCount, DateTimeOffset CreateAt);
```

实现 `VideoHotBoardService` 时逐条对齐 `MarkdownHotBoardService`：

- ZSet key：`video:hot:all`；重建锁：`video:hot:rebuild:lock`（30s TTL，`StringSetIfNotExistsAsync` 单飞）；
- `GetHotBoardAsync`：`SortedSetRangeByRank(0, take-1, Descending)` → 空榜单飞重建后重读 → `LoadItemsByGuidsAsync` 批量回查（筛选公开/未删/展示，按榜单序返回）；异常走 `ComputeFromDbAsync` 降级；
- `UpdateScoreAsync`：加载实体 → `RecalculateHotScore` → SaveChanges → `SortedSetAddAsync`；失败仅 LogWarning；
- `RebuildAsync`：`Interlocked.Exchange` 防重入 → 全量 `RecalculateHotScore` → SaveChanges → 删 ZSet → `SortedSetAddManyAsync` 批量回填。

### 4.5 定时收敛（BackgroundService）

`VideoHeatRebuildBackgroundService` 与 Markdown 同构：首次延迟 1 分钟，之后每 10 分钟 `RebuildAsync`；异常记录日志下周期重试。

## 5. 写侧钩子接入点

| 命令/端点 | 钩子调用时机 |
|-----------|--------------|
| [LikeVideoCommandHandler](file:///f:/NotBlog/Video.Web.API/Application/Commands/LikeVideoCommandHandler.cs) | 计数落库后（upvote/ballot/share 增减均触发；与通知逻辑不耦合） |
| [AddVideoToCollectionCommandHandler](file:///f:/NotBlog/Video.Web.API/Application/Commands/AddVideoToCollectionCommandHandler.cs) / Remove | Stars 增减落库后（**前置：实施文档 §6.1 Stars 接入**） |
| [RecordVideoWatchCommandHandler](file:///f:/NotBlog/Video.Web.API/Application/Commands/RecordVideoWatchCommandHandler.cs) / [VideoStreamEndpoints](file:///f:/NotBlog/Video.Web.API/Apis/VideoStreamEndpoints.cs#L86) | `UpWatch()` 后（已受 5 分钟去重窗口约束，频率可控，D-7） |

实现约定：

```csharp
// 各命令处理器主流程末尾（计数提交、缓存失效之后）
await videoHotBoard.UpdateScoreAsync(request.VideoGuid, cancellationToken);
```

- `UpdateScoreAsync` 内部异常自吞（仅日志），**不改变互动请求的成功/失败语义**；
- 不重复加载实体：`UpdateScoreAsync` 内部自包含仓储/上下文加载，命令处理器无需透传实体引用；
- 事件链路（互动通知、热度刷新）彼此独立，互不阻塞。

## 6. 热点榜 API

```http
GET /api/video/hot?take=10
Authorization: Bearer <token>        # 可选鉴权；榜单本身公开
```

响应（沿用 `VideoResult<T>` 包装，[VideoResultType](file:///f:/NotBlog/Video.Domain/Entities/VideoResultType.cs) 语义保持一致）：

```json
{
  "resultType": "VideoResultOk",
  "statusCode": 200,
  "message": "OK",
  "data": [
    { "videoGuid": "…", "videoName": "…", "videoCover": "https://…",
      "heatScore": 4.82, "watchCount": 15230, "createAt": "2026-08-28T10:00:00Z" }
  ]
}
```

实现位置：新建 `VideoHotEndpoints.cs`（`/api/video/hot` 独立端点组），复用 `VideoServiceDI` 注入 `IVideoHotBoardService`；`take` 缺省 10、上限 50（校验并 Clamp）。默认走缓存 ZSet，Redis 故障自动降级，接口保持可用。

## 7. 注册与依赖

```csharp
// Program.cs（增量）
builder.Services.AddSingleton<IVideoHotBoardService, VideoHotBoardService>();
builder.Services.AddHostedService<VideoHeatRebuildBackgroundService>();
```

- `VideoHotBoardService` 通过 `IServiceProvider.GetService<IRedisCacheService>()` 按需取 Redis（Local/单服务模式未注册时返回 null，天然降级 DB），与 `MarkdownHotBoardService` 完全一致；
- DbContext 由服务内部以 `IServiceScopeFactory`/直接注入的 `VideoDbContext`（Singleton 持有 Scoped 有风险）——**注意**：榜单服务为 Singleton，需模仿 Markdown 用 `IServiceProvider` + 手动 scope 或 `VideoDbContext` 工厂获取连接，具体实现时参照 `MarkdownHotBoardService` 的注入方式逐条对齐（其直接注入 `MarkDownDbContext` 亦可），并验证 DbContext 生命周期。

## 8. 数据库与迁移

1. **新增列**：`Videos.HeatScore`（`double precision` not null default 0）；
2. **存量回填**：`Add-Migration VideoHotBoard` 后执行 `dotnet ef database update`；存量视频热度由首次定时重建（10 分钟内）全量算出，无需手工回填脚本；
3. **无新索引**：榜单以 Redis ZSet 承担；DB 降级路径为全量读 + 内存排序（对齐 Markdown，视频体量下可接受；后续量大再做 DB 侧排序优化）。

## 9. 测试计划

**单测**
- `VideoHeatFormulaTests`（新建）：权重组合正确性；对数压缩特性（播放 1e5 与 1e6 差异 < 播放 10 与 100 差异）；`ageDays < 0` 钳位为 0；`freshness` 单调递减（t0=1、t2=0.37、t7≈0.03）；互动为 0 时分数主要由浏览+时效支撑；全 0 输入返回正小数（log10(1)=0 + 0.2×1）。

**集成**
- `VideoHotBoardService`：写侧钩子后 ZSet 分数更新；空榜单飞重建只执行一次（并发两请求）；顶掉未公开/已删/未展示视频；`LoadItemsByGuids` 维持榜单顺序；Redis 断连时降级 DB 返回 TopN 不抛错。

**API**
- `GET /api/video/hot`：默认 10 条、Take 上限 50 钳制；响应字段完整；与列表接口共用鉴权/脱敏管道。

## 10. 风险与后续

| 事项 | 说明 | 处置 |
|------|------|------|
| 公式参数为经验值 | 权重/半衰期待线上数据调优 | 常量集中可配，后续按 `VideoQuote` 分布调整 |
| ZSet 投影与 DB 短暂不一致 | 写侧失败/遗漏由定时重建收敛 | 与 Markdown 相同的最终一致模型，10 分钟窗口可接受 |
| 评论互动未计入热度 | 评论量大且语义不同 | 二期对 `ReviewQuote` Like/Dislike 单独建模型或并入综合热度 |
| 榜单冷启动为空 | 首次启动 1 分钟后重建 | 空榜时读侧单飞重建兜底，服务不阻塞 |
| 定时重建全量扫描 | 视频量大时 10 分钟全量开销上升 | 后续可引入增量重算（仅更新有互动的视频） |
| Singleton 内 DbContext 生命周期 | 榜单服务为 Singleton | 实现时严格对齐 `MarkdownHotBoardService` 的注入/取用方式并做冒烟验证 |

## 11. 文件清单（Video 侧）

| 文件 | 动作 |
|------|------|
| `Video.Domain/Heat/VideoHeatFormula.cs` | 新增（公式） |
| `Video.Domain/Server/IVideoHotBoardService.cs` | 新增（契约 + VideoHotItem） |
| `Video.Domain/Entities/Video.cs` | 修改（HeatScore + RecalculateHotScore） |
| `Video.Web.API/Services/VideoHotBoardService.cs` | 新增（ZSet 读写/单飞重建/DB 降级） |
| `Video.Web.API/Background/VideoHeatRebuildBackgroundService.cs` | 新增（10 分钟定时收敛） |
| `Video.Web.API/Apis/VideoHotEndpoints.cs` | 新增（GET /api/video/hot） |
| `Video.Web.API/Program.cs` | 注册 Singleton 服务 + HostedService |
| `LikeVideoCommandHandler` / Collection 命令 ×2 / `RecordVideoWatchCommandHandler` / `VideoStreamEndpoints` | 修改（写入后调用 UpdateScoreAsync） |
| `Video.Web.API/Dto/Response/` | 如需热点条目专用 DTO 则新增（或直接复用 VideoHotItem） |
| Migration：`Videos.HeatScore` 列 | 新增 |

## 12. 验收清单

- [ ] 公式纯函数单测覆盖权重组合、对数压缩、半衰期衰减、负龄钳位；
- [ ] 互动（点赞/投币/收藏/分享/观看）后 `video:hot:all` 中该视频分数即时更新；
- [ ] `GET /api/video/hot?take=N` 返回 TopN 且仅含公开/未删/已展示视频；
- [ ] Redis 停用后接口自动降级 DB 计算，不抛错、不 502；
- [ ] 定时任务每 10 分钟重建一次，多实例并发只执行一次（锁生效）；
- [ ] `Add-Migration` 成功、`HeatScore` 列存在、存量视频在首次重建后完成回填；
- [ ] Video 服务构建、单测/集成测试通过。