# Markdown 服务文件化存储与热点体系重构设计

> 状态：设计稿 v0.1（待确认）
> 范围：Markdown.Domain / Markdown.Infrastructure / Markdown.Web.API
> 关联：FileDev（文件存储）、CacheMemory（Redis 缓存）、Message（硬币口径）

---

## 一、现状诊断

### 1.1 当前问题

| # | 问题 | 现状 |
|---|------|------|
| P1 | **文档全文存 DB** | `MarkDown.MarkDownContent` text 列（上限 100 万字符），历史快照 `OldMarkDown.OldMarkDownContent` 也是全文 → 大字段膨胀、备份/迁移成本高、全文无法利用文件级能力（版本管理/秒传/引用计数） |
| P2 | **计数语义错位** | `MarkQuote` 挂在 `MarkReview`（评论）上，但名称/字段（Share/Coin 等）更像文档级；文档实体本身**零计数**，无法计算文档热点 |
| P3 | **计数缺口** | 文档级浏览/点赞/收藏/分享/硬币全部不存在；评论级 `CommentSome`/`ShareSome` 无维护入口（死方法）；无"踩"功能 |
| P4 | **历史版本=全文快照** | `CreateHistorySnapshot` 直接从实体 `MarkDownContent` 取全文入库；MarkDown 去内容化后此链路失效，需改为从文件流获取 |
| P5 | **无热点体系** | 无热度算法、无热点榜、无 Redis 缓存（AppHost 已注入 Redis 引用，代码零使用） |

### 1.2 相关现状（不改的部分）

- `MarkFavorite`（收藏）独立聚合根，`(UserGuid, MarkDownGuid)` 唯一约束防重，收藏数可由表 COUNT 或冗余计数得出
- FileDev gRPC 已全链路可用：`UploadFile`（bytes 直传 + expected_md5 秒传）/ `DownloadFile`（流式）/ `GetFileInfo` / `UpdateFileInfo` / `DeleteFile`，`FileType.FILE_DOCUMENT`，JWT 认证（caller_user_id），本地磁盘 `FileStorage/{userId:N}/{guid v7}{ext}` + PG 元数据
- Message 已有硬币口径：`UserInfo.Coins`（账户余额，`AddCoins/ConsumeCoins`），`Tweet.CoinCount` 收款口径；**CoinTweet 扣余额联动未完成**（设计先行，联动后续）
- 评论点赞防重已有范式：`(MarkReviewGuid, UserId)` 唯一约束 + 点赞记录与计数同事务提交
- 浏览量防刷已有范式（Message R-06）：Redis Set `tweet:viewed:{guid:N}` SADD + 24h TTL

---

## 二、设计原则

1. **数据库只存元数据**：文档正文一律文件化（FileDev 默认），DB 保存文件引用与统计；历史版本同理（表结构保留，内容来源改为文件流）
2. **PG 是唯一事实源，Redis 是可降级投影**：热点榜/计数缓存全部可从 DB 重建；Redis 故障只降级不丢数据
3. **计数口径单一**：写侧原子更新（唯一约束 / ExecuteUpdate），读侧缓存 Cache-Aside + TTL 收敛
4. **聚合根自治**：计数变更走聚合根领域方法（`MarkDown.AddLove()` / `MarkReview.AddDislike()` 等），仓储负责持久化
5. **向后兼容优先**：OldMarkDown 表结构不变（内容来源改变）；存量数据一次性迁移为文件

---

## 三、总体架构

```
┌────────────┐   gRPC(FileDocument)   ┌────────────┐
│ Markdown   │ ─────────────────────► │ FileDev    │  文档正文文件 / 历史版本文件
│  Web.API   │ ◄───────────────────── │ (FILE_DOC) │  FileStorage/{userId}/{guid}.md
└─────┬──────┘                        └────────────┘
      │ 元数据 + 计数（PG）
      ▼
┌────────────┐     热度榜/计数缓存      ┌────────────┐
│ MarkDownDb │ ◄─────────────────────► │ Redis      │  ZSet 热点榜 / 计数 / 防刷
└────────────┘     (Cache-Aside+TTL)   └────────────┘
```

- **内容文件**：MarkDown 创建/更新时上传 FileDev（FILE_DOCUMENT），`MarkDown` 表只存 `FileId/FileUri/FileSize/FileMd5/FileName`
- **历史版本**：更新前从 FileDev 下载当前文件流 → 写入 `OldMarkDown`（结构不变）；远期可 Git 化（见 §8）
- **热点**：`MarkQuote` 持原始计数（含持久化 HeatScore 列）→ 定时任务/写侧钩子重算 → 写 Redis ZSet（`markdown:hot:all`）→ `GET /api/markdowns/hot` 直接读榜

---

## 四、数据模型（实体重构）

### 4.1 MarkDown（文档=文件元数据）

| 字段 | 类型 | 变化 | 说明 |
|------|------|------|------|
| MarkDownGuid / MarkUserGuid / MarkDownName | Guid/string | 不变 | |
| MarkDownHash | string | 不变 | 当前文件内容 SHA-256 |
| **FileId** | string | **新增** | FileDev 文件 ID（唯一引用，防重复上传秒传） |
| **FileUri** | string | **新增** | 文件访问 URI（不对外暴露，仅内部定位） |
| **FileSize** | long | **新增** | 文件字节数 |
| **FileExt** | string | **新增** | 扩展名（.md / .markdown） |
| MarkDownContent | text | **删除** | 迁移时存量内容导出为 FileDev 文件后删列 |
| MarkDownTagboard / MarkDownAuth / Status / IsDelete / CreateAt / UpdateAt | — | 不变 | |
| MarkReviews / OldMarkDowns | 导航 | 不变 | |
| **MarkQuote** | Owned | **新增** | 文档交互计数（见 4.2） |

- 创建：`POST /api/markdowns` 收内容 → 上传 FileDev → 得 file_id → 建实体（元数据 + MarkQuote 初始 0）
- 更新：旧文件内容 → 历史快照（§4.4）→ 上传新文件 → 更新元数据 + 新哈希（秒传语义：同哈希直接复用 FileId）
- 读取：`GET /api/markdowns/{guid}/content` → 校验权限 → DownloadFile 流式返回（正文不进 DB）
- 删除：软删文档 + FileDev 引用计数软删（有旧版本引用时物理文件保留）

### 4.2 MarkQuote（文档级交互计数，Owned by MarkDown）

| 字段 | 列名 | 语义 | 维护入口 |
|------|------|------|----------|
| ViewSome | ViewCount | 浏览数 | `POST /{guid}/view`（Redis Set 24h 防刷 + 原子 +1，Message R-06 同模式） |
| LoveSome | LoveCount | 点赞数 | `POST /{guid}/like|unlike`（MarkDocumentLike 表唯一约束防重 + 同事务） |
| FavoriteSome | FavoriteCount | 收藏数 | 收藏/取消收藏命令钩子（冗余计数，与 MarkFavorite 表同事务） |
| ShareSome | ShareCount | 分享数 | `POST /{guid}/share`（防刷可选） |
| CoinSome | CoinCount | 硬币（打赏） | `POST /{guid}/coin`（MarkCoin 记录表 + 扣 UserInfo.Coins，Message 联动后续） |
| HeatScore | HeatScore | 热度分 | 定时任务/写侧重算（§6），默认 0 |

> 字段名沿用 `XxxSome`（与现有 MarkQuote 命名一致），语义全部改为文档级。

### 4.3 ReviewQuote（评论级交互计数，Owned by MarkReview，替代原 MarkQuote）

| 字段 | 列名 | 语义 | 维护入口 |
|------|------|------|----------|
| LoveSome | LoveCount | 点赞 | 现有 `LikeReviewAsync/RemoveLikeReviewAsync`（不变） |
| ViewSome | ViewCount | 查看 | 现有 `IncreaseReviewViewAsync`（不变） |
| ReplySome | ReplyCount | 回复数 | `AddChildReview`（不变） |
| DislikeSome | DislikeCount | 踩 | **新增**：`MarkReviewDislike` 表（(MarkReviewGuid, UserId) 唯一约束防重）+ `POST /reviews/{guid}/dislike|undislike` |

- 原 `CommentSome`（评论数）/`ShareSome`（分享数）**删除**：无维护入口、语义与"评论/子评论实体"重复（评论树本身就是计数）
- 迁移：`MarkReview` 表 5 列 → 4 列（LoveCount/ViewCount/ReplyCount/DislikeCount，DislikeCount 默认 0）

### 4.4 OldMarkDown（表结构不变，思路改变）

```
保存逻辑（变更后）：
  更新文档时：
    1. 从 FileDev 下载当前文档文件流（旧内容来源：文件，不再是实体属性）
    2. 构造 OldMarkDown（OldMarkDownContent = 文件流读出全文，OldMarkDownHash = 现有哈希）
    3. 入库（表结构、字段、软删除语义完全不变）
```

- 历史详情 `GET /history/{oldGuid}`、删除、还原 `POST /history/{oldGuid}/restore` 逻辑**零改动**（仍从 DB 读全文快照）
- 远期（§8）：历史数据 Git 化后，OldMarkDown 表退化为"版本索引"（存 commit/引用），`OldMarkDownContent` 列停用——**表结构仍不变**

---

## 五、新增/变更 API

### 5.1 MarkdownApi 变更

| 端点 | 变化 | 说明 |
|------|------|------|
| `POST /api/markdowns` | 改 | 请求体含 MarkDownContent → 服务端上传 FileDev 存元数据 |
| `PUT /api/markdowns/{guid}` | 改 | 同上 + 更新前文件流快照 |
| `GET /api/markdowns/{guid}` | 改 | 返回元数据 + MarkQuote 计数（不含正文） |
| `GET /api/markdowns/{guid}/content` | **新增** | 正文流式返回（权限校验同详情） |
| `POST /api/markdowns/{guid}/view` | **新增** | 浏览 +1（Redis Set 24h 防刷） |
| `POST /api/markdowns/{guid}/like` / `unlike` | **新增** | 文档点赞/取消（MarkDocumentLike 唯一约束防重） |
| `POST /api/markdowns/{guid}/share` | **新增** | 分享 +1 |
| `POST /api/markdowns/{guid}/coin` | **新增** | 打赏硬币（MarkCoin 记录 + 扣投币者余额） |
| `GET /api/markdowns/hot?scope=all&take=20` | **新增** | 热点榜（Redis ZSet 直读，miss 则重建） |

### 5.2 MarkdownReviewApi 变更

| 端点 | 变化 | 说明 |
|------|------|------|
| `POST /reviews/{guid}/dislike` / `undislike` | **新增** | 踩/取消踩（MarkReviewDislike 唯一约束防重） |
| 评论列表/详情响应 | 改 | `Quote` 字段换 `ReviewQuote`（4 项） |
| 点赞/取消/浏览/回复 | 不变 | 计数目标从 MarkQuote 改为 ReviewQuote |

### 5.3 新增表（EF Migration）

| 表 | 关键约束 | 用途 |
|----|----------|------|
| `MarkDocumentLike` | (MarkDownGuid, UserId) 唯一 | 文档点赞防重 |
| `MarkCoin` | 无唯一（可多次打赏） | 打赏记录（MarkDownGuid, UserId, Amount, CreateAt） |
| `MarkReviewDislike` | (MarkReviewGuid, UserId) 唯一 | 评论踩防重 |

---

## 六、热点算法（用户已定权重）

```
HeatScore = 0.5 × Interaction + 0.3 × View + 0.2 × Freshness
```

### 6.1 三项计算（建议，可调）

| 项 | 公式 | 说明 |
|----|------|------|
| Interaction（互动） | `log10(1 + Love×1 + Favorite×2 + Share×3 + Coin×5)` | 收藏/分享/硬币行为权重高于点赞；log 压缩量级 |
| View（浏览） | `log10(1 + ViewCount)` | 浏览量大，log 压缩 |
| Freshness（时间衰减） | `exp(-ageDays / 7)` | 7 天半衰期：新文档衰减快、权重高；7 天后 ≈ 0.37，14 天 ≈ 0.14 |

> 归一化说明：三项均落在 ≈[0, 1+log 上限] 区间，加权后 HeatScore 可直接作 ZSet score。
> 备选公式：Freshness 也可用 `1 / (1 + ageDays/7)`（衰减更平滑），上线后按榜单分布微调。

### 6.2 计算与缓存策略

| 时机 | 动作 |
|------|------|
| 写侧（任何计数 +1/-1） | 同步更新 `MarkQuote` 对应计数（事务内）+ Redis `ZINCRBY markdown:hot:all <Δscore> {guid}`（失败仅记日志） |
| 定时任务（如每 10 分钟，HostedService） | 全量重算 HeatScore：更新 DB 列 + 重建 Redis ZSet（幂等，榜单兜底收敛） |
| 读热点榜 | `ZREVRANGE markdown:hot:all 0 take-1` → 按 guid 批量回查元数据；ZSet miss/空 → 惰性重建（单飞，`StringSetIfNotExists` 占位锁防击穿） |
| TTL | 热点榜 key 无 TTL（由定时任务维护）；计数缓存 key TTL 5min ± 随机抖动（防雪崩） |

### 6.3 一致性保证（承接上一轮结论）

- **PG 权威**：所有计数以 PG 为准；Redis 任何 key 均可从 DB 重建
- **写侧失败不阻断**：ZINCRBY/删 key 失败只记日志，TTL/定时重建自动收敛
- **防击穿/防雪崩**：单飞重建 + TTL 抖动
- **防刷**：浏览 Redis Set 24h 去重；点赞/收藏/踩 DB 唯一约束（绝对权威）

---

## 七、安全

| 风险 | 对策 |
|------|------|
| 文件 ID 枚举下载 | `GET /content` 走 Markdown 服务权限校验（HasPermission + 审核门控，与详情一致）；FileDev 文件本身无鉴权 URL 不可枚举（沿用现有 /files/{**path} 设计） |
| 私有文档内容外泄 | 文档 MarkDownAuth=Private/Protected 时 FileDev 文件 identity 设 `FILE_PRIVATE`；Markdown 侧读前强校验 |
| 打赏越权/滥用 | `POST /coin` 需认证 + 数量校验（如 1~100 整数）+ 投币者余额校验（ConsumeCoins 联动 Message UserInfo，**Message 侧扣减待联动**） |
| 计数刷量 | 浏览 Set 防刷（24h）；点赞/踩/收藏唯一约束；分享可后续加防刷 |

---

## 八、历史版本 Git 化（远期可选，本轮不实施）

| 方案 | 说明 | 优点 | 风险 |
|------|------|------|------|
| A. 维持 DB 快照（本轮） | OldMarkDown 表结构不变，内容从文件流获取 | 零风险、现有 API 零改动 | DB 仍存历史全文 |
| B. 本地 Git 仓库 | 每文档一个文件，git 天然管理历史（commit hash 存 OldMarkDown 新列或复用 Hash） | 版本差异/回溯能力最强，无外部依赖 | 需引入 git 运行时依赖（LibGit2Sharp），OldMarkDown 表结构需微调 |
| C. GitHub 远程仓库 | 历史推到 GitHub 私有仓 | 异地备份 | **网络依赖**（本机 github.com:443 不通，仅 SSH 22 可达；部署环境未知）；文档隐私（私有文档进第三方仓库）；需 GitHub token 管理 |

**建议**：本轮实施 A（零风险、API 不变）；B/C 作为后续独立需求评估——若走 C，必须解决网络可用性与私有文档不出域的问题（或仅公开文档入 Git）。

---

## 九、迁移与兼容

### 9.1 存量数据处理（一次性）

1. 新增列：MarkDown 加 FileId/FileUri/FileSize/FileExt/MarkQuote 6 列；MarkReview 5 列改 4 列（CommentCount/ShareCount 删除）
2. 数据回填脚本：遍历现有 `MarkDown` 行 → 内容上传 FileDev（FILE_DOCUMENT，文件名 `{guid:N}.md`）→ 回填 FileId/FileSize/FileMd5
3. 存量 OldMarkDownContent 保留（历史可读）；`MarkDownContent` 列删除（回填完成后）
4. 新表创建：MarkDocumentLike / MarkCoin / MarkReviewDislike

### 9.2 兼容性

- 前端影响：详情接口不再返回 Content（新增 `/content` 流式端点）；列表响应增加计数字段；评论响应 Quote → ReviewQuote（字段名变化，前端需同步）
- 旧历史版本仍可读/可还原（DB 快照语义不变）
- 删除文档时 FileDev 引用计数软删（旧版本引用保留物理文件）

---

## 十、分阶段实施 checklist

### 阶段 1：领域模型重构（Domain + Infrastructure）
- [ ] `MarkQuote.cs` 改造为文档级 6 计数（含 HeatScore），从 MarkReview 摘除
- [ ] `ReviewQuote.cs` 新建（4 计数），挂 MarkReview
- [ ] `MarkDown.cs` 去 MarkDownContent，加文件引用字段 + MarkQuote；创建/更新领域方法改签名（内容走文件流参数或 FileId）
- [ ] `OldMarkDown` 快照逻辑改文件流来源（CreateHistorySnapshot 由命令层提供内容流）
- [ ] `MarkReview` 计数操作改 ReviewQuote；MarkQuote 类删除评论级方法
- [ ] 新实体：MarkDocumentLike / MarkCoin / MarkReviewDislike
- [ ] EF Migration（列变更 + 新表）+ 存量回填脚本
- [ ] Markdown.Tests 更新（MarkQuoteTests → MarkQuote/ReviewQuote 双测试）

### 阶段 2：文件化存取 + 端点（Web.API）
- [ ] csproj 引入 FileDev gRPC 客户端（照 Message.FileStorageGrpcClient 模板）+ 认证附加
- [ ] 创建/更新命令：上传 FileDev → 元数据落库；更新前文件流快照
- [ ] `GET /content` 流式端点 + 权限校验
- [ ] 新增端点：view / like / unlike / share / coin / hot
- [ ] 评论 dislike / undislike 端点
- [ ] 响应 DTO 变更（MarkdownResponse 去 Content 加计数；MarkReviewResponse 换 ReviewQuote）

### 阶段 3：热点与 Redis 缓存
- [ ] Markdown.Web.API 接 CacheMemory（csproj + `AddCacheMemory(builder.Configuration, "Redis")`；AppHost 已配 `WithReference(redis)`）
- [ ] 热度计算服务（公式 §6）+ 写侧 ZINCRBY 钩子
- [ ] 定时重算任务（HostedService）
- [ ] 热点榜端点（ZSet 直读 + 单飞重建）
- [ ] 浏览防刷 Set（24h TTL）
- [ ] 冒烟验证：热点榜读写、Redis 停机降级、计数一致性（写 DB vs 榜分数）

### 阶段 4（可选，后续）
- [ ] 历史 Git 化评估（§8 B/C）
- [ ] Message CoinTweet 扣余额联动（文档打赏扣 UserInfo.Coins）

---

## 十一、待确认决策点

| # | 决策 | 推荐 |
|---|------|------|
| D1 | 正文文件存储后端 | **FileDev**（现成 gRPC/秒传/引用计数）；GitHub 仅作远期历史方案 |
| D2 | OldMarkDown 本轮 | **表结构不变 + 快照从文件流**（§4.4）；Git 化远期 |
| D3 | 存量 MarkDownContent | 一次性导出为 FileDev 文件后**删列**（§9.1） |
| D4 | 硬币打赏本轮范围 | 本轮**先落记录 + 计数**（MarkCoin 表 + MarkQuote.CoinCount）；扣 UserInfo.Coins 联动随 Message 阶段 4 |
| D5 | 热度子项权重 | 互动子项：Love×1 / Favorite×2 / Share×3 / Coin×5；半衰期 7 天（§6.1，上线可按榜单微调） |
| D6 | 评论响应字段 | Quote → ReviewQuote（前端需同步改名） |
