using NotMediator;
using Video.Domain.IRepository;
using Video.Domain.Cache;

namespace Video.Web.API.Application.Commands;

public class DeleteVideoCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<DeleteVideoCommandHandler> logger) : IRequestHandler<DeleteVideoCommand, bool>
{
    public async Task<bool> Handler(DeleteVideoCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting video {VideoGuid}", request.VideoGuid);

        
        var video = await cacheService.GetVideoMetaAsync(request.VideoGuid, cancellationToken);
        if (video is null)
        {
            video = await videoRepository.FindByVideoAsync(request.VideoGuid);
            if (video is null)
            {
                logger.LogWarning("Video {VideoGuid} not found", request.VideoGuid);
                return false;
            }
        }
       
        if (video.Affiliated is null || !video.Affiliated.Contains(request.UserGuid))
        {
            logger.LogWarning("User {UserGuid} not affiliated with video {VideoGuid}", request.UserGuid, request.VideoGuid);
            return false;
        }

        
        video.DeleteVideo();

   
        await videoRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        logger.LogInformation("Video {VideoGuid} deleted", request.VideoGuid);
        return true;
    }
}
