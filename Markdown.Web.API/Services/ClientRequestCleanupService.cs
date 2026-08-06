using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Services;

/// <summary>
///     ClientRequest 幂等记录清理服务：定期删除超过保留期的记录，防止幂等表无限膨胀
///     （幂等记录仅在命令重试窗口内有意义，保留 7 天足够覆盖绝大多数重试场景）
/// </summary>
public class ClientRequestCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ClientRequestCleanupService> logger) : BackgroundService
{
    /// <summary>清理周期：每 24 小时执行一次</summary>
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);

    /// <summary>幂等记录保留期：7 天</summary>
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);
        do
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // 清理失败不影响主流程，下个周期重试
                logger.LogError(ex, "清理幂等记录失败");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarkDownDbContext>();
        var cutoff = DateTimeOffset.UtcNow - Retention;

        // ExecuteDelete 直接生成 DELETE SQL，不加载实体到内存
        var deleted = await context.ClientRequests
            .Where(c => c.Created < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            logger.LogInformation("已清理 {Count} 条过期幂等记录（保留 {Days} 天）", deleted, Retention.Days);
    }
}
