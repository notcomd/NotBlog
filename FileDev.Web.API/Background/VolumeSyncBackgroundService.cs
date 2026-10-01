using FileDev.Domain.IServices;
using Microsoft.Extensions.DependencyInjection;

namespace FileDev.Web.API.Background;

/// <summary>
/// 启动时卷注册表同步：应用启动与迁移完成后，把 FileBox 存储的卷/目录统计
/// 全量回填到 DB 卷表（NotFileVolume），供卷管理 API 与租户卷查询使用。
/// 仅执行一次即结束；失败仅记录日志，不影响应用启动。
/// </summary>
public class VolumeSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<VolumeSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 稍微延后执行，确保迁移与后续初始化（如卷注册表装载）已完成
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var volumeService = scope.ServiceProvider.GetRequiredService<INotFileVolumeService>();
        try
        {
            var synced = await volumeService.SyncVolumesAsync(stoppingToken);
            logger.LogInformation("[VolumeSync] 启动时卷注册表同步完成，卷数={Count}", synced);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning("");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[VolumeSync] 启动时卷注册表同步失败（不影响应用启动）");
        }
    }
}