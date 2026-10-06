using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 提交视频审核命令处理器：仅作者本人可提交（草稿/被驳回 → 待审核）。
/// </summary>
public class SubmitVideoForReviewCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<SubmitVideoForReviewCommandHandler> logger)
    : IRequestHandler<SubmitVideoForReviewCommand, bool>
{
    public async Task<bool> Handler(SubmitVideoForReviewCommand request, CancellationToken cancellationToken)
    {
        // 写路径必须从仓储加载实体，确保被 DbContext 跟踪后修改可落库
        var video = await videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        if (video.Affiliated is null || !video.Affiliated.Contains(request.UserGuid))
        {
            logger.LogWarning("User {UserGuid} is not the author of video {VideoGuid}", request.UserGuid, request.VideoGuid);
            throw new UnauthorizedAccessException("仅视频作者本人可提交审核");
        }

        video.SubmitForReview();
        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.InvalidateVideoAsync(request.VideoGuid, ct: cancellationToken);

        logger.LogInformation("Video {VideoGuid} submitted for review by {UserGuid}", request.VideoGuid, request.UserGuid);
        return true;
    }
}
