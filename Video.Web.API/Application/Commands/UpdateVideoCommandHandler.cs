
using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;
namespace Video.Web.API.Application.Commands;


public class UpdateVideoCommandHandler : IRequestHandler<UpdateVideoCommand, bool>
{
    private readonly IVideoRepository _videoRepository;

    private readonly IVideoCacheService _videoCacheService;

    private readonly ILogger<UpdateVideoCommandHandler> _logger;


    public UpdateVideoCommandHandler(IVideoRepository videoRepository, IVideoCacheService videoCacheService, ILogger<UpdateVideoCommandHandler> logger)
    {
        _videoRepository = videoRepository ?? throw new ArgumentNullException(nameof(videoRepository));
        _videoCacheService = videoCacheService ?? throw new ArgumentNullException(nameof(videoCacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handler(UpdateVideoCommand request, CancellationToken cancellationToken)
    {

        _logger.LogInformation("[UpdateVideoCommandHandler] 更新视频元数据: VideoGuid={VideoGuid}",
            request.VideoGuid);
        // 写路径必须从仓储加载实体，确保被 DbContext 跟踪后更新可落库
        // （缓存反序列化出的实体未被跟踪，直接 SaveChanges 会静默丢失）
        var video = await _videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        video.UpDataVideo(request.VideoName, request.BriefIntroduction,
        request.VideoCover, request.VideoFileUri,
        request.Tags, request.VideoControl);

        await _videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await _videoCacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);
        await _videoCacheService.InvalidateVideoListsAsync(cancellationToken);

        _logger.LogInformation("[UpdateVideoCommandHandler] 更新视频元数据成功: VideoGuid={VideoGuid}",
            request.VideoGuid);

        return true;
    }
}