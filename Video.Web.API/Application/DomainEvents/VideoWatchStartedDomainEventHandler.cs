using NotMediator;
using Video.Domain.Events;

namespace Video.Web.API.Application.DomainEvents;

/// <summary>
/// 处理用户开始观看视频的领域事件。
/// - 记录观看日志
/// - 通过 Redis 原子递增视频播放次数
/// </summary>
public class VideoWatchStartedDomainEventHandler(
    ILogger<VideoWatchStartedDomainEventHandler> logger)
    : INotificationHandler<VideoWatchStartedDomainEvent>
{
    public async Task Handler(VideoWatchStartedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[Domain] Watch started: History={HistoryGuid}, User={UserGuid}, Video={VideoGuid}, Start={StartTime:O}",
            notification.VideoHistoryGuid, notification.UserGuid,
            notification.VideoGuid, notification.StartTime);

        // 播放计数可在此接入 Redis 原子递增或消息队列异步写入
        // 示例：await _cacheService.IncrementQuoteFieldAsync(
        //     notification.VideoGuid, VideoCacheKeys.QuoteFields.Watch, 1, cancellationToken);

        await Task.CompletedTask;
    }
}
