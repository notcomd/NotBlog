
using NotMediator;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.Cache;


namespace Video.Web.API.Application.Commands;

/// <summary>
/// 上传视频命令处理程序
/// </summary>
/// <param name="videoRepository">视频仓储</param>
/// <param name="logger">日志记录器</param>
/// <returns>是否成功上传视频</returns>
public class UploadVideoCommandHandler(IVideoRepository videoRepository, IVideoCacheService cacheService
,ILogger<UploadVideoCommandHandler> logger):IRequestHandler<UploadVideoCommand,bool>
{

    private readonly ILogger<UploadVideoCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IVideoRepository _videoRepository = videoRepository ?? throw new ArgumentNullException(nameof(videoRepository));
    private readonly IVideoCacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    
    public async Task<bool> Handler(UploadVideoCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[UploadVideoCommandHandler] 上传视频成功: VideoName={VideoName}", command.VideoName);

        var video = new Videos(command.AffectedUserGuid, command.VideoName, command.VideoCover,
         command.VideoFileUri, command.BriefIntroduction, command.Tags);
        video.VideoControlChangeByVideoController(command.VideoControl);

        await _videoRepository.AddByVideoAsync(video);
        await _videoRepository.UnitOfWork.SaveEntitiesAsync();

        // P-04：新增视频后失效视频列表缓存，确保新视频立即可见
        await _cacheService.InvalidateVideoListsAsync(cancellationToken);

        return true;
    }
}
