using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 评论点赞/点踩命令处理器 — 支持评论 upvote(点赞)/down(点踩)。
/// 计数写入 ReviewQuote（独立于视频 VideoQuote），并同步刷新 Redis ReviewQuote 哈希。
/// </summary>
public class QuoteVideoReviewCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<QuoteVideoReviewCommandHandler> logger)
    : IRequestHandler<QuoteVideoReviewCommand, QuoteVideoReviewResult>
{
    public async Task<QuoteVideoReviewResult> Handler(QuoteVideoReviewCommand request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Field.ToLowerInvariant();
        var validFields = new HashSet<string> { "upvote", "down" };

        if (!validFields.Contains(normalized))
            return new QuoteVideoReviewResult(false, 0,
                $"Invalid field '{request.Field}'. Valid: upvote, down.");

        // 写路径必须从仓储加载实体，确保被 DbContext 跟踪后修改可落库
        var video = await videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        var review = video.VideoReviews?.FirstOrDefault(r => r.VideoReviewGuid == request.ReviewGuid);
        if (review is null)
            return new QuoteVideoReviewResult(false, 0, "Review not found.");

        // ReviewQuote 增量：upvote → Like；down → Dislike（delta = ±1，下限 0 由值对象原子减保护）
        var quote = review.Quote;
        switch (normalized)
        {
            case "upvote":
                if (request.IsLike) quote.UpLike();
                else quote.DownLike();
                break;
            case "down":
                if (request.IsLike) quote.UpDislike();
                else quote.DownDislike();
                break;
        }

        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        // Redis ReviewQuote 哈希增量（尽力而为：失败仅日志，读写侧各自兜底）
        try
        {
            var field = normalized == "upvote"
                ? VideoCacheKeys.ReviewQuoteFields.Like
                : VideoCacheKeys.ReviewQuoteFields.Dislike;
            var delta = request.IsLike ? 1L : -1L;
            await cacheService.IncrementReviewQuoteFieldAsync(request.ReviewGuid, field, delta, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "评论计数缓存增量失败：Review={ReviewGuid} Field={Field}", request.ReviewGuid, normalized);
        }

        await cacheService.InvalidateVideoReviewCachesAsync(request.VideoGuid, ct: cancellationToken);
        await cacheService.RemoveVideoReviewRepliesAsync(request.ReviewGuid, cancellationToken);
        if (review.RootReview is { } rootReviewId && rootReviewId != Guid.Empty)
            await cacheService.RemoveVideoReviewRepliesAsync(rootReviewId, cancellationToken);

        var newCount = normalized == "upvote" ? quote.Like : quote.Dislike;

        logger.LogInformation("Review like: {ReviewGuid} {Field} IsLike={IsLike} NewCount={Count}",
            request.ReviewGuid, request.Field, request.IsLike, newCount);

        return new QuoteVideoReviewResult(true, newCount, null);
    }
}