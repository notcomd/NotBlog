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

        video.AddByVideoReview(request.UserGuid, request.RootReview,
            request.Body, request.VideoImages);
        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);
        await cacheService.InvalidateVideoReviewCachesAsync(request.VideoGuid, ct: cancellationToken);
        if (request.RootReview is { } rootReviewId && rootReviewId != Guid.Empty)
            await cacheService.RemoveVideoReviewRepliesAsync(rootReviewId, cancellationToken);

        logger.LogInformation("Review added successfully to video {VideoGuid}", request.VideoGuid);
        return true;
    }
}
