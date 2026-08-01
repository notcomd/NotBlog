using NotMediator;
using Video.Domain.Entities;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 记录视频观看命令处理器 — 查找或创建观看历史，更新进度。
/// </summary>
public class RecordVideoWatchCommandHandler(
    IVideoHistoryRepository videoHistoryRepository,
    ILogger<RecordVideoWatchCommandHandler> logger)
    : IRequestHandler<RecordVideoWatchCommand, WatchRecordResult>
{
    public async Task<WatchRecordResult> Handler(RecordVideoWatchCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await videoHistoryRepository.FindLastByUserAndVideoAsync(
            request.UserGuid, request.VideoGuid);

        VideoHistory history;
        if (existing is not null && !existing.IsCompleted)
        {
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

        await videoHistoryRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);

        logger.LogInformation("Video watch recorded: {VideoGuid} User={UserGuid} Progress={Progress}",
            request.VideoGuid, request.UserGuid, request.Progress);

        return new WatchRecordResult(true, history.VideoHistoryGuid, null);
    }
}

/// <summary>
/// 结束视频观看命令处理器 — 标记观看结束，更新播放计数。
/// </summary>
public class EndVideoWatchCommandHandler(
    IVideoHistoryRepository videoHistoryRepository,
    IVideoRepository videoRepository,
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
        await videoHistoryRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);

        // 更新视频播放计数
        var video = await videoRepository.FindByVideoAsync(request.VideoGuid);
        if (video is not null)
        {
            video.VideoQuote.UpWatch();
            await videoRepository.UpdateByQuoteAsync(video.VideoQuote);
        }

        logger.LogInformation("Video watch ended: {VideoGuid} User={UserGuid} Duration={Duration}",
            request.VideoGuid, request.UserGuid, history.Duration);

        return new WatchRecordResult(true, history.VideoHistoryGuid, null);
    }
}
