
using NotMediator;
using Video.Domain.IRepository;
using Video.Domain.Cache;
namespace Video.Web.API.Application.Commands;

public class UpdateVideoQuoteCommandHandler(
    IVideoCacheService cacheService,
    IVideoRepository videoRepository,
    ILogger<UpdateVideoQuoteCommandHandler> logger) : IRequestHandler<UpdateVideoQuoteCommand, bool>
{
    public async Task<bool> Handler(UpdateVideoQuoteCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating video quote {Field} for video {VideoGuid}",
            request.Field, request.VideoGuid);

        
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

       
        var quote = video.VideoQuote;
        var normalized = request.Field.ToLowerInvariant();
        if (request.IsAdd)
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
                    logger.LogError("Invalid quote field: {Field}", request.Field);
                    return false;
            }
        }else{
            switch (normalized)
            {
                case "upvote": quote.DownUpvote(); break;
                case "stars": quote.DownStars(); break;
                case "down": quote.DownDown(); break;
                case "ballot": quote.DownBallot(); break;
                case "share": quote.DownShare(); break;
                default:
                    logger.LogError("Invalid quote field: {Field}", request.Field);
                    return false;
            }
        }


        
        await videoRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        logger.LogInformation("Video quote updated: {VideoGuid} {Field} incremented",
            request.VideoGuid, request.Field);

        return true;
    }
}

