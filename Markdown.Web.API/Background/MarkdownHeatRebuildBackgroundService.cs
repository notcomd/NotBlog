namespace Markdown.Web.API.Background;

/// <summary>
///     热点榜定时重建任务：每 10 分钟全量重算所有文档热度分
///     （DB HeatScore 列 + Redis ZSet 兜底收敛，保证榜单最终一致）。
///     首次启动延迟 1 分钟执行（等待服务与 Redis 就绪）。
/// </summary>
public class MarkdownHeatRebuildBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<MarkdownHeatRebuildBackgroundService> logger) : BackgroundService
{
    /// <summary>重建间隔</summary>
    private static readonly TimeSpan RebuildInterval = TimeSpan.FromMinutes(10);

    /// <summary>启动后首次执行延迟</summary>
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(RebuildInterval);
        try
        {
            do
            {
                await RebuildOnceAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("热点榜定时重建任务已停止");
        }
    }

    private async Task RebuildOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var hotBoard = scope.ServiceProvider.GetRequiredService<IMarkdownHotBoardService>();
            await hotBoard.RebuildAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "热点榜定时重建执行失败（下个周期重试）");
        }
    }
}
