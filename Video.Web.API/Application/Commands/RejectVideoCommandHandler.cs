using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 驳回视频命令处理器：仅待审核视频可驳回，驳回后记录原因并取消公开展示。
/// </summary>
public class RejectVideoCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<RejectVideoCommandHandler> logger)
    : IRequestHandler<RejectVideoCommand, bool>
{
    public async Task<bool> Handler(RejectVideoCommand request, CancellationToken cancellationToken)
    {
        var video = await videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        video.Reject(request.Reason);
        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.InvalidateVideoAsync(request.VideoGuid, ct: cancellationToken);

        logger.LogInformation("Video {VideoGuid} rejected: {Reason}", request.VideoGuid, request.Reason);
        return true;
    }
}
