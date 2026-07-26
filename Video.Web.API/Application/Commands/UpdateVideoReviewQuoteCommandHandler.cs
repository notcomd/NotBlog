using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;
namespace Video.Web.API.Application.Commands;



public class UpdateVideoReviewQuoteCommandHandler(
    IVideoCacheService cacheService,
    IVideoRepository videoRepository,
    ILogger<UpdateVideoReviewQuoteCommandHandler> logger) : IRequestHandler<UpdateVideoReviewQuoteCommand, bool>
{
    public async Task<bool> Handler(UpdateVideoReviewQuoteCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating review quote {Field} ({Dir}) for review {ReviewGuid}",
            request.Field, request.IsIncrement ? "+" : "-", request.ReviewGuid);

        // 1. 获取视频聚合（优先缓存，写操作无需回填 —— 后续会清缓存）
        var video = await cacheService.GetVideoMetaAsync(request.VideoGuid, cancellationToken);
        if (video is null)
        {
            video = await videoRepository.FindByVideoAsync(request.VideoGuid);
            if (video is null)
            {
                logger.LogError("Video not found: {VideoGuid}", request.VideoGuid);
                return false;
            }
        }

        
        var review = video.VideoReviews?.FirstOrDefault(r => r.VideoReviewGuid == request.ReviewGuid);
        if (review is null)
        {
            logger.LogError("Review not found: {ReviewGuid}", request.ReviewGuid);
            return false;
        }

       
        var quote = review.VideoQuote;
        ApplyQuoteChange(quote, request.Field, request.IsIncrement);

        
        await videoRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);

        
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        logger.LogInformation("Review quote updated: {ReviewGuid} {Field} {Dir}",
            request.ReviewGuid, request.Field, request.IsIncrement ? "incremented" : "decremented");

        return true;
    }

    private static void ApplyQuoteChange(Video.Domain.ValueObjects.VideoQuote quote, string field, bool isIncrement)
    {
        var normalized = field.ToLowerInvariant();

        if (isIncrement)
        {
            switch (normalized)
            {
                case "upvote": quote.UpUpvote(); break;
                case "stars": quote.UpStars(); break;
                case "watch": quote.UpWatch(); break;
                case "down": quote.UpDown(); break;
                case "ballot": quote.UpBallot(); break;
                case "share": quote.UpShare(); break;
                default:
                    throw new ArgumentException($"Invalid quote field: {field}");
            }
        }
        else
        {
            switch (normalized)
            {
                case "upvote": quote.DownUpvote(); break;
                case "stars": quote.DownStars(); break;
                case "down": quote.DownDown(); break;
                case "ballot": quote.DownBallot(); break;
                case "share": quote.DownShare(); break;
                default:
                    throw new ArgumentException($"Invalid quote field: {field}");
            }
        }
    }
}