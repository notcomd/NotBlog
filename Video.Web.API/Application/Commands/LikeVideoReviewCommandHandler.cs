using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 评论点赞命令处理器 — 支持评论 upvote/down/ballot/share 操作。
/// </summary>
public class LikeVideoReviewCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<LikeVideoReviewCommandHandler> logger)
    : IRequestHandler<LikeVideoReviewCommand, LikeVideoReviewResult>
{
    public async Task<LikeVideoReviewResult> Handler(LikeVideoReviewCommand request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Field.ToLowerInvariant();
        var validFields = new HashSet<string> { "upvote", "down", "ballot", "share" };

        if (!validFields.Contains(normalized))
            return new LikeVideoReviewResult(false, 0,
                $"Invalid field '{request.Field}'. Valid: upvote, down, ballot, share.");

        var video = await cacheService.GetVideoMetaAsync(request.VideoGuid, cancellationToken);
        if (video is null)
        {
            video = await videoRepository.FindByVideoAsync(request.VideoGuid);
            if (video is null)
                return new LikeVideoReviewResult(false, 0, "Video not found.");
        }

        var review = video.VideoReviews?.FirstOrDefault(r => r.VideoReviewGuid == request.ReviewGuid);
        if (review is null)
            return new LikeVideoReviewResult(false, 0, "Review not found.");

        var quote = review.VideoQuote;
        if (request.IsLike)
        {
            switch (normalized)
            {
                case "upvote": quote.UpUpvote(); break;
                case "down": quote.UpDown(); break;
                case "ballot": quote.UpBallot(); break;
                case "share": quote.UpShare(); break;
            }
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

        await videoRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        var newCount = normalized switch
        {
            "upvote" => quote.Upvote,
            "down" => quote.Down,
            "ballot" => quote.Ballot,
            "share" => quote.Share,
            _ => 0L
        };

        logger.LogInformation("Review like: {ReviewGuid} {Field} IsLike={IsLike} NewCount={Count}",
            request.ReviewGuid, request.Field, request.IsLike, newCount);

        return new LikeVideoReviewResult(true, newCount, null);
    }
}
