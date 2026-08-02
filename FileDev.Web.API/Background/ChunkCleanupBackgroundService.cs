using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileDev.Web.API.Background;

/// <summary>
/// 过期分片清理后台任务（S-09）。
/// <para>
/// 定期扫描超过 <see cref="NotFileStorageOptions.ChunkExpirationHours"/> 仍未完成（未合并/未取消）的
/// 分片上传任务，删除其临时分片文件并移除数据库记录，防止磁盘无限增长。
/// </para>
/// </summary>
public class ChunkCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptionsSnapshot<NotFileStorageOptions> options,
    ILogger<ChunkCleanupBackgroundService> logger) : BackgroundService
{
    /// <summary>清理周期：1 小时</summary>
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 启动后先执行一次，之后周期性清理
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredChunksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ChunkCleanup] 过期分片清理失败");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredChunksAsync(CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow.AddHours(-options.Value.ChunkExpirationHours);

        // BackgroundService 为单例，仓储为 Scoped，需通过作用域工厂解析
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFileChunkRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<INotFileStorageService>();

        var expired = await repository.GetExpiredRecordsAsync(threshold, ct);
        foreach (var record in expired)
        {
            await storageService.CleanupChunksAsync(record.FileKey);
            await repository.DeleteAsync(record.FileKey, ct);
            logger.LogInformation("[ChunkCleanup] 清理过期分片: FileKey={FileKey}, CreatedAt={CreatedAt}",
                record.FileKey, record.CreatedAt);
        }

        if (expired.Any())
            await repository.UnitOfWork.SaveChangesAsync(ct);
    }
}
