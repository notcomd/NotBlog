
using NotMediator;

namespace Video.Web.API.Apis;

/// <summary>视频服务依赖注入</summary>
public record VideoServiceDI(
    IVideoService VideoService,
    IVideoRepository VideoRepository,
    ILoggerFactory LoggerFactory,
    IVideoCacheService VideoCacheService,
    INotMediator NotMediator,
    IVideoCollectionRepository VideoCollectionRepository,
    IVideoHistoryRepository? VideoHistoryRepository,
    ILogger<VideoServiceDI> Logger
);
