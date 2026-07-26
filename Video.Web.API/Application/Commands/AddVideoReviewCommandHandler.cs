using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 添加视频评论命令处理程序
/// </summary>

public class AddVideoReviewCommandHandler : IRequestHandler<AddVideoReviewCommand, bool>
{
    private readonly IVideoCacheService _cacheService;
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<AddVideoReviewCommandHandler> _logger;

    public AddVideoReviewCommandHandler(IVideoCacheService cacheService,
                                        IVideoRepository videoRepository,
                                        ILogger<AddVideoReviewCommandHandler> logger)
    {
        _cacheService = cacheService;
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public async Task<bool> Handler(AddVideoReviewCommand request, CancellationToken cancellationToken)
    {
        
        _logger.LogInformation("Adding review to video {VideoGuid} by user {UserGuid}",
            request.VideoGuid, request.UserGuid);

        var video = await _cacheService.GetVideoMetaAsync(request.VideoGuid, cancellationToken);
        if (video is null)
        {
            video = await _videoRepository.FindByVideoAsync(request.VideoGuid);
            if (video is null)
            {
                _logger.LogError("Video not found: {VideoGuid}", request.VideoGuid);
                return false;
            }
        }

        if (request.RootReview is { } rootId && rootId != Guid.Empty)
        {
            var parent = video.VideoReviews?
                .FirstOrDefault(r => r.VideoReviewGuid == rootId);
            if (parent is null)
            {
                _logger.LogError("Parent review not found: {RootReview}", rootId);
                return false;
            }
        }
        video.AddByVideoReview(request.UserGuid, request.RootReview,
            request.Body, request.VideoImages);
        await _videoRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        // 4. 使缓存失效
        await _cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);
        _logger.LogInformation("Review added successfully to video {VideoGuid}", request.VideoGuid);
        return true;
    }
}
