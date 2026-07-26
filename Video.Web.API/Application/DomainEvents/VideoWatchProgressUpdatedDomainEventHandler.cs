using NotMediator;
using Video.Domain.Events;

namespace Video.Web.API.Application.DomainEvents;

/// <summary>
/// 处理用户观看进度更新的领域事件（25%/50%/75% 里程碑节流触发）。
/// - 记录进度里程碑日志
/// - 可扩展：接入实时分析、推荐系统兴趣度加权等
/// </summary>
public class VideoWatchProgressUpdatedDomainEventHandler(
    ILogger<VideoWatchProgressUpdatedDomainEventHandler> logger)
    : INotificationHandler<VideoWatchProgressUpdatedDomainEvent>
{
    public async Task Handler(VideoWatchProgressUpdatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[Domain] Watch progress: History={HistoryGuid}, User={UserGuid}, " +
            "Video={VideoGuid}, Progress={Progress:P0}, UpdatedAt={UpdatedAt:O}",
            notification.VideoHistoryGuid, notification.UserGuid,
            notification.VideoGuid, notification.Progress, notification.UpdatedAt);

        await Task.CompletedTask;
    }
}
