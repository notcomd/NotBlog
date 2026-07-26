using NotMediator;
using Video.Domain.Events;

namespace Video.Web.API.Application.DomainEvents;

/// <summary>
/// 处理用户完成观看视频的领域事件。
/// - 记录观看完成日志（含实际观看时长）
/// - 可扩展：写入用户观看统计、推荐系统消费记录等
/// </summary>
public class VideoWatchCompletedDomainEventHandler(
    ILogger<VideoWatchCompletedDomainEventHandler> logger)
    : INotificationHandler<VideoWatchCompletedDomainEvent>
{
    public async Task Handler(VideoWatchCompletedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[Domain] Watch completed: History={HistoryGuid}, User={UserGuid}, " +
            "Video={VideoGuid}, Duration={Duration}, CompletedAt={CompletedAt:O}",
            notification.VideoHistoryGuid, notification.UserGuid,
            notification.VideoGuid, notification.Duration, notification.CompletedAt);

        await Task.CompletedTask;
    }
}
