using CacheMemory.Core;
using Markdown.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Markdown.Web.API.Services;

/// <summary>
///     Markdown 热点榜服务（Redis ZSet 直读 + 单飞重建 + DB 降级）。
///     <para>
///     数据流：PG 是唯一事实源（MarkQuote 计数 + HeatScore 列），Redis ZSet 是可降级投影。
///     写侧钩子（交互端点）调用 <see cref="UpdateScoreAsync"/> 实时刷新单文档热度；
///     定时任务调用 <see cref="RebuildAsync"/> 全量重算兜底收敛（10 分钟）。
///     Redis 不可用时（Local 模式/故障）自动降级为 DB 实时计算，服务不中断。
///     </para>
/// </summary>
public class MarkdownHotBoardService(
    IServiceProvider serviceProvider,
    MarkDownDbContext dbContext,
    ILogger<MarkdownHotBoardService> logger) : IMarkdownHotBoardService
{
    /// <summary>热点榜 ZSet key（member=MarkDownGuid:N，score=热度分）</summary>
    public const string HotBoardKey = "markdown:hot:all";

    /// <summary>重建互斥锁 key（单飞防击穿）</summary>
    private const string RebuildLockKey = "markdown:hot:rebuild:lock";

    /// <summary>重建互斥锁 TTL（30s，防止任务崩溃后锁残留）</summary>
    private static readonly TimeSpan RebuildLockTtl = TimeSpan.FromSeconds(30);

    /// <summary>Redis 服务（Local 模式未注册时为 null，自动降级 DB）</summary>
    private IRedisCacheService? Redis => serviceProvider.GetService<IRedisCacheService>();

    /// <summary>防重入（定时任务与请求触发的重建互斥）</summary>
    private int _rebuilding;

    /// <inheritdoc />
    public async Task<List<MarkdownHotItem>> GetHotBoardAsync(int take, CancellationToken ct = default)
    {
        // 1. 优先 Redis ZSet 直读（榜单命中即返回，秒级）
        try
        {
            var redis = Redis;
            if (redis is not null)
            {
                var members = (await redis.SortedSetRangeByRankAsync(
                    HotBoardKey, 0, take - 1, Order.Descending, ct)).ToList();
                if (members.Count == 0)
                {
                    // 空榜：单飞重建（防击穿：并发请求只有一个执行重建）
                    await RebuildWithSingleFlightAsync(ct);
                    members = (await redis.SortedSetRangeByRankAsync(
                        HotBoardKey, 0, take - 1, Order.Descending, ct)).ToList();
                }

                if (members.Count > 0)
                {
                    return await LoadItemsByGuidsAsync(members, ct);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis 热点榜读取失败，降级 DB 实时计算");
        }

        // 2. 降级：DB 实时计算（Redis 缺失/故障时保可用）
        return await ComputeFromDbAsync(take, ct);
    }

    /// <inheritdoc />
    public async Task UpdateScoreAsync(Guid markDownGuid, CancellationToken ct = default)
    {
        try
        {
            var markdown = await dbContext.Markdowns
                .FirstOrDefaultAsync(m => m.MarkDownGuid == markDownGuid, ct);
            if (markdown is null)
                return;

            var score = markdown.RecalculateHotScore(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(ct);

            var redis = Redis;
            if (redis is not null)
            {
                await redis.SortedSetAddAsync(HotBoardKey, markDownGuid.ToString("N"), score, ct);
            }

            logger.LogDebug("文档 {Guid} 热度分已更新：{Score}", markDownGuid, score);
        }
        catch (Exception ex)
        {
            // 热点更新失败不影响业务主流程（计数已提交）；定时重建会兜底收敛
            logger.LogWarning(ex, "文档 {Guid} 热度分更新失败", markDownGuid);
        }
    }

    /// <inheritdoc />
    public async Task RebuildAsync(CancellationToken ct = default)
    {
        // 防重入：定时任务与请求触发的重建并发时只允许一个执行
        if (Interlocked.Exchange(ref _rebuilding, 1) == 1)
            return;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var markdowns = await dbContext.Markdowns.AsNoTracking().ToListAsync(ct);

            foreach (var markdown in markdowns)
            {
                markdown.RecalculateHotScore(now);
            }

            await dbContext.SaveChangesAsync(ct);

            // 重建 Redis ZSet（Redis 可用时）
            var redis = Redis;
            if (redis is not null)
            {
                await redis.KeyDeleteAsync(HotBoardKey, ct);
                if (markdowns.Count > 0)
                {
                    await redis.SortedSetAddManyAsync(HotBoardKey,
                        markdowns.Select(m => (m.MarkDownGuid.ToString("N"), m.MarkQuote.HeatScore)), ct);
                }
            }

            logger.LogInformation("热点榜全量重建完成，共 {Count} 篇文档", markdowns.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "热点榜全量重建失败");
        }
        finally
        {
            Interlocked.Exchange(ref _rebuilding, 0);
        }
    }

    /// <summary>单飞重建：抢到锁才执行，其余调用方短等后重读榜单</summary>
    private async Task RebuildWithSingleFlightAsync(CancellationToken ct)
    {
        var redis = Redis;
        if (redis is null)
        {
            await RebuildAsync(ct);
            return;
        }

        var lockAcquired = await redis.StringSetIfNotExistsAsync(
            RebuildLockKey, "1", RebuildLockTtl, ct);
        if (!lockAcquired)
        {
            // 其他实例正在重建：短暂等待后由调用方重读榜单
            await Task.Delay(300, ct);
            return;
        }

        try
        {
            await RebuildAsync(ct);
        }
        finally
        {
            await redis.KeyDeleteAsync(RebuildLockKey, ct);
        }
    }

    /// <summary>按 guid 批量回查文档元数据（保持 ZSet 排名顺序）</summary>
    private async Task<List<MarkdownHotItem>> LoadItemsByGuidsAsync(IEnumerable<string> memberGuids, CancellationToken ct)
    {
        var guids = memberGuids
            .Select(g => Guid.TryParse(g, out var guid) ? guid : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
        if (guids.Count == 0)
            return [];

        var docs = await dbContext.Markdowns.AsNoTracking()
            .Where(m => guids.Contains(m.MarkDownGuid) && !m.IsDelete)
            .ToListAsync(ct);

        var orderIndex = guids.Select((g, i) => (g, i)).ToDictionary(x => x.g, x => x.i);
        return docs
            .OrderBy(m => orderIndex[m.MarkDownGuid])
            .Select(m => new MarkdownHotItem(m.MarkDownGuid, m.MarkDownName, m.MarkQuote.HeatScore, m.CreateAt))
            .ToList();
    }

    /// <summary>DB 实时计算（降级路径）：全量重算后按热度排序取 TopN</summary>
    private async Task<List<MarkdownHotItem>> ComputeFromDbAsync(int take, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var docs = await dbContext.Markdowns.AsNoTracking()
            .Where(m => !m.IsDelete && m.Status == MarkStatus.MarkApproved)
            .ToListAsync(ct);

        return docs
            .Select(m => new MarkdownHotItem(m.MarkDownGuid, m.MarkDownName,
                MarkdownHeatFormula.Calculate(m.MarkQuote, m.CreateAt, now), m.CreateAt))
            .OrderByDescending(i => i.HeatScore)
            .Take(take)
            .ToList();
    }
}

/// <summary>
///     热点榜条目（响应模型）
/// </summary>
public record MarkdownHotItem(Guid MarkDownGuid, string Name, double HeatScore, DateTimeOffset CreateAt);
