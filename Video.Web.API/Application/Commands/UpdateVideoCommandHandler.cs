
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
        var video = await _videoCacheService.GetVideoMetaAsync(request.VideoGuid, cancellationToken);

        if (video is null)
        {
            video = await _videoRepository.FindByVideoAsync(request.VideoGuid);
            if (video is null)
            {
                throw new KeyNotFoundException($"Video with Guid {request.VideoGuid} not found.");
            }
        }

        video.UpDataVideo(request.VideoName, request.BriefIntroduction,
        request.VideoCover, request.VideoFileUri,
        request.Tags, request.VideoControl);

        await _videoRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        await _videoCacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        _logger.LogInformation("[UpdateVideoCommandHandler] 更新视频元数据成功: VideoGuid={VideoGuid}",
            request.VideoGuid);

        return true;
    }
}