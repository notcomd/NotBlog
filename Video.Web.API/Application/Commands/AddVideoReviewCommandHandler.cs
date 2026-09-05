using Notcomd.EventBus.Core;
using NotMediator;
using Video.Web.API.Application.IntegrationEvents;
using Video.Web.API.Application.IntegrationEvents.Events;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 添加视频评论命令处理器：评论落库后发布 VideoCommentPublished 集成事件。
/// 顶级评论 → 通知视频作者；回复评论 → 通知被回复的评论作者（自互动由消费端过滤）。
/// </summary>
public class AddVideoReviewCommandHandler(
    IVideoCacheService cacheService,
    IVideoRepository videoRepository,
    IEventBus eventBus,
    ILogger<AddVideoReviewCommandHandler> logger)
    : IRequestHandler<AddVideoReviewCommand, bool>
{
    /// <summary>评论正文预览截断长度（通知文案使用）</summary>
    private const int PreviewMaxLength = 50;

    public async Task<bool> Handler(AddVideoReviewCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding review to video {VideoGuid} by user {UserGuid}",
            request.VideoGuid, request.UserGuid);

        // 写路径必须从仓储加载实体，确保被 DbContext 跟踪后评论可落库
        // （缓存反序列化出的实体未被跟踪，直接 SaveChanges 会静默丢失）
        var video = await videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        if (request.RootReview is { } rootId && rootId != Guid.Empty)
        {
            var parent = video.VideoReviews?
                .FirstOrDefault(r => r.VideoReviewGuid == rootId);
            if (parent is null)
            {
                logger.LogError("Parent review not found: {RootReview}", rootId);
                return false;
            }
        }

        // 生成评论 Id：幂等请求重放时复用 RequestId，保证同一命令的重试不产生多条评论（且通知引用同一 Guid）
        var reviewGuid = request.RequestId;
        video.AddByVideoReview(reviewGuid, request.UserGuid, request.RootReview,
            request.Body, request.VideoImages);
        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);
        await cacheService.InvalidateVideoReviewCachesAsync(request.VideoGuid, ct: cancellationToken);
        if (request.RootReview is { } rootReviewId && rootReviewId != Guid.Empty)
            await cacheService.RemoveVideoReviewRepliesAsync(rootReviewId, cancellationToken);

        // 评论发布通知：顶级→视频作者；回复→被回复的评论作者（TargetUserId 为空(无作者)时跳过）
        await PublishCommentNotificationAsync(video, request, reviewGuid, cancellationToken);

        logger.LogInformation("Review added successfully to video {VideoGuid}", request.VideoGuid);
        return true;
    }

    /// <summary>
    /// 组装并安全发布评论发布事件（总线故障仅记日志，不动摇评论主流程）。
    /// </summary>
    private async Task PublishCommentNotificationAsync(
        Videos video, AddVideoReviewCommand request, Guid reviewGuid, CancellationToken cancellationToken)
    {
        Guid targetUserId;
        try
        {
            if (request.RootReview is { } rootId && rootId != Guid.Empty)
            {
                targetUserId = video.VideoReviews?
                    .FirstOrDefault(r => r.VideoReviewGuid == rootId)?.UserGuid ?? Guid.Empty;
            }
            else
            {
                targetUserId = video.Affiliated?.FirstOrDefault() ?? Guid.Empty;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "评论通知目标用户解析失败，跳过发布：Video={VideoGuid}", request.VideoGuid);
            return;
        }

        if (targetUserId == Guid.Empty)
            return;

        await EventPublishing.PublishSafelyAsync(eventBus, new VideoCommentPublishedIntegrationEvent(
            VideoGuid: video.VideoGuid,
            VideoName: video.VideoName,
            ReviewGuid: reviewGuid,
            RootReviewGuid: request.RootReview is { } r && r != Guid.Empty ? r : null,
            ActorUserId: request.UserGuid,
            TargetUserId: targetUserId,
            CommentPreview: TruncatePreview(request.Body),
            OccurredAt: DateTimeOffset.UtcNow), logger);
    }

    /// <summary>截断评论正文为通知预览（超过上限截断，空/空白返回空串）</summary>
    private static string TruncatePreview(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";
        return body.Length <= PreviewMaxLength ? body : body[..PreviewMaxLength];
    }
}