
namespace Video.Web.API.Application.Commands;

/// <summary>
/// 记录视频观看命令处理器 — 查找或创建观看历史，更新进度。
/// </summary>
public class RecordVideoWatchCommandHandler(
    IVideoHistoryRepository videoHistoryRepository,
    IVideoRepository videoRepository,
    ILogger<RecordVideoWatchCommandHandler> logger)
    : IRequestHandler<RecordVideoWatchCommand, WatchRecordResult>
{
    public async Task<WatchRecordResult> Handler(RecordVideoWatchCommand request,
        CancellationToken cancellationToken)
    {
        // S-18.3：观看历史落库前校验视频存在，防止对不存在/已删除的视频无限写入历史。
        try
        {
            _ = await videoRepository.FindByVideoAsync(request.VideoGuid);
        }
        catch (AggregateException)
        {
            logger.LogWarning("Video {VideoGuid} not found while recording watch for user {UserGuid}",
                request.VideoGuid, request.UserGuid);
            return new WatchRecordResult(false, null, "Video not found.");
        }

        var existing = await videoHistoryRepository.FindLastByUserAndVideoAsync(
            request.UserGuid, request.VideoGuid);

        VideoHistory history;
        if (existing is not null && !existing.IsCompleted)
        {
            // 进行中的记录（IsCompleted=false）不重复创建，直接复用并更新进度
            history = existing;
            history.UpdateProgress(request.Progress, request.LastPositionSeconds);
            await videoHistoryRepository.UpdateAsync(history);
        }
        else
        {
            history = new VideoHistory(request.UserGuid, request.VideoGuid);
            history.UpdateProgress(request.Progress, request.LastPositionSeconds);
            await videoHistoryRepository.AddAsync(history);
        }

        await videoHistoryRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("Video watch recorded: {VideoGuid} User={UserGuid} Progress={Progress}",
            request.VideoGuid, request.UserGuid, request.Progress);

        return new WatchRecordResult(true, history.VideoHistoryGuid, null);
    }
}

/// <summary>
/// 结束视频观看命令处理器 — 标记观看结束，更新播放计数（Redis 5 分钟去重）。
/// </summary>
public class EndVideoWatchCommandHandler(
    IVideoHistoryRepository videoHistoryRepository,
    IVideoRepository videoRepository,
    IRedisCacheService redis,
    IVideoCacheService cacheService,
    ILogger<EndVideoWatchCommandHandler> logger)
    : IRequestHandler<EndVideoWatchCommand, WatchRecordResult>
{
    public async Task<WatchRecordResult> Handler(EndVideoWatchCommand request,
        CancellationToken cancellationToken)
    {
        var history = await videoHistoryRepository.FindLastByUserAndVideoAsync(
            request.UserGuid, request.VideoGuid);

        if (history is null)
            return new WatchRecordResult(false, null, "Watch history not found.");

        history.EndWatching(DateTimeOffset.UtcNow);
        await videoHistoryRepository.UpdateAsync(history);
        await videoHistoryRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // S-18.2：更新播放计数前做 Redis 去重（与流接口共用「视频+用户+5 分钟窗口」key），
        // 避免「Range 请求计数 + 结束观看」双重计数；异常不阻断主流程。
        try
        {
            var video = await videoRepository.FindByVideoAsync(request.VideoGuid);
            var dedupKey = VideoCacheKeys.VideoWatchWindow(request.VideoGuid, request.UserGuid);
            var shouldCount = await redis.StringSetIfNotExistsAsync(
                dedupKey, "1", VideoCacheKeys.VideoWatchWindowTtl, cancellationToken);
            if (shouldCount)
            {
                video.VideoQuote.UpWatch();
                await videoRepository.UpdateByQuoteAsync(video.VideoGuid, video.VideoQuote);
                // P-04：播放计数变化后失效视频元数据与列表缓存（列表 JSON 含 quote）
                await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);
                await cacheService.InvalidateVideoListsAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update watch count for video {VideoGuid} on end-watch",
                request.VideoGuid);
        }

        logger.LogInformation("Video watch ended: {VideoGuid} User={UserGuid} Duration={Duration}",
            request.VideoGuid, request.UserGuid, history.Duration);

        return new WatchRecordResult(true, history.VideoHistoryGuid, null);
    }
}
