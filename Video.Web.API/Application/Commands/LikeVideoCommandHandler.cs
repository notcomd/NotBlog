using CacheMemory.Core;
using Notcomd.EventBus.Core;
using NotMediator;
using Video.Web.API.Application.IntegrationEvents;
using Video.Web.API.Application.IntegrationEvents.Events;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 视频点赞命令处理器 — 支持视频 upvote/down/ballot/share 操作。
/// 点赞/投币为「首次」时发布 VideoInteraction 集成事件通知视频作者（Redis 去重键判定，取消操作不发）。
/// </summary>
public class LikeVideoCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    IEventBus eventBus,
    IRedisCacheService redis,
    ILogger<LikeVideoCommandHandler> logger)
    : IRequestHandler<LikeVideoCommand, LikeVideoResult>
{
    public async Task<LikeVideoResult> Handler(LikeVideoCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Field.ToLowerInvariant();
        var validFields = new HashSet<string> { "upvote", "down", "ballot", "share" };

        if (!validFields.Contains(normalized))
            return new LikeVideoResult(false, 0, $"Invalid field '{request.Field}'. Valid: upvote, down, ballot, share.");

        // 写路径必须从仓储加载实体，确保被 DbContext 跟踪后修改可落库
        // （缓存反序列化出的实体未被跟踪，直接 SaveChanges 会静默丢失）
        var video = await videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        // 取消操作（unlike）的「首次」去重键不删除：已通知过即不再通知（互动实施文档 D-5）
        var quote = video.VideoQuote;
        var isFirstInteraction = false;
        if (request.IsLike)
        {
            switch (normalized)
            {
                case "upvote": quote.UpUpvote(); break;
                case "down": quote.UpDown(); break;
                case "ballot": quote.UpBallot(); break;
                case "share": quote.UpShare(); break;
            }

            if (normalized is "upvote" or "ballot")
                isFirstInteraction = await TryMarkFirstInteractionAsync(request.VideoGuid, request.UserGuid, normalized, cancellationToken);
        }
        else
        {
            switch (normalized)
            {
                case "upvote": quote.DownUpvote(); break;
                case "down": quote.DownDown(); break;
                case "ballot": quote.DownBallot(); break;
                case "share": quote.DownShare(); break;
            }
        }

        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);
        await cacheService.InvalidateVideoListsAsync(cancellationToken);

        // 仅首次发生的点赞/投币发布作者通知（自互动由消费端过滤；失败不阻断主流程）
        if (isFirstInteraction)
        {
            var target = video.Affiliated?.FirstOrDefault(id => id != request.UserGuid) ?? Guid.Empty;
            if (target != Guid.Empty)
            {
                await EventPublishing.PublishSafelyAsync(eventBus, new VideoInteractionIntegrationEvent(
                    InteractionType: normalized == "upvote" ? VideoInteractionType.VideoLiked : VideoInteractionType.VideoCoined,
                    VideoGuid: video.VideoGuid,
                    VideoName: video.VideoName,
                    ActorUserId: request.UserGuid,
                    TargetUserId: target,
                    OccurredAt: DateTimeOffset.UtcNow), logger);
            }
        }

        var newCount = normalized switch
        {
            "upvote" => quote.Upvote,
            "down" => quote.Down,
            "ballot" => quote.Ballot,
            "share" => quote.Share,
            _ => 0L
        };

        logger.LogInformation("Video like: {VideoGuid} {Field} IsLike={IsLike} NewCount={Count}",
            request.VideoGuid, request.Field, request.IsLike, newCount);

        return new LikeVideoResult(true, newCount, null);
    }

    /// <summary>
    /// 尝试将本次互动标记为「首次」（SETNX 语义）：成功返回 true 表示需发布通知。
    /// Redis 不可用时降级为「非首次」，避免重复通知轰炸。
    /// </summary>
    private async Task<bool> TryMarkFirstInteractionAsync(Guid videoGuid, Guid userGuid, string field, CancellationToken ct)
    {
        try
        {
            var key = VideoCacheKeys.VideoInteractionOnce(videoGuid, userGuid, field);
            return await redis.StringSetIfNotExistsAsync(key, "1",
                expiry: VideoCacheKeys.VideoInteractionOnceTtl, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "互动去重键写入失败，跳过本次互动通知：Video={VideoGuid} Field={Field}", videoGuid, field);
            return false;
        }
    }
}
