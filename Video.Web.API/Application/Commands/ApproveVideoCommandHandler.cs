using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 审核通过视频命令处理器：仅待审核视频可通过，通过后同步置为公开展示。
/// </summary>
public class ApproveVideoCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<ApproveVideoCommandHandler> logger)
    : IRequestHandler<ApproveVideoCommand, bool>
{
    public async Task<bool> Handler(ApproveVideoCommand request, CancellationToken cancellationToken)
    {
        var video = await videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        video.Approve();
        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.InvalidateVideoAsync(request.VideoGuid, ct: cancellationToken);

        logger.LogInformation("Video {VideoGuid} approved", request.VideoGuid);
        return true;
    }
}
