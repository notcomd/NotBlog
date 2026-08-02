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

        // 写路径必须从仓储加载实体，确保被 DbContext 跟踪后删除可落库
        var video = await videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        if (video.Affiliated is null || !video.Affiliated.Contains(request.UserGuid))
        {
            logger.LogWarning("User {UserGuid} not affiliated with video {VideoGuid}", request.UserGuid, request.VideoGuid);
            return false;
        }

        
        video.DeleteVideo();

   
        // P-04：删除视频时按视频维度批量失效所有相关缓存
        // （meta/quote/列表缓存，以及该视频所有评论的回复与计数缓存）
        var reviewGuids = video.VideoReviews?.Select(r => r.VideoReviewGuid).ToArray() ?? [];
        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.InvalidateVideoAsync(request.VideoGuid, reviewGuids, cancellationToken);

        logger.LogInformation("Video {VideoGuid} deleted", request.VideoGuid);
        return true;
    }
}
