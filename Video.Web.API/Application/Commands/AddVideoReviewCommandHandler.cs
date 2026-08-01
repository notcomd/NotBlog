using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 添加视频评论命令处理器。
/// </summary>
public class AddVideoReviewCommandHandler(
    IVideoCacheService cacheService,
    IVideoRepository videoRepository,
    ILogger<AddVideoReviewCommandHandler> logger)
    : IRequestHandler<AddVideoReviewCommand, bool>
{
    public async Task<bool> Handler(AddVideoReviewCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding review to video {VideoGuid} by user {UserGuid}",
            request.VideoGuid, request.UserGuid);

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

        video.AddByVideoReview(request.UserGuid, request.RootReview,
            request.Body, request.VideoImages);
        await videoRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        logger.LogInformation("Review added successfully to video {VideoGuid}", request.VideoGuid);
        return true;
    }
}
