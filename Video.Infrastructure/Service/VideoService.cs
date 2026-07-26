using Microsoft.Extensions.Logging;
using Video.Domain.Cache;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.Server;

namespace Video.Infrastructure.Service;

/// <summary>
/// Video service implementation — cache-aside queries only.
/// All write operations are handled by CQRS commands.
/// </summary>
public class VideoService : IVideoService
{
    private readonly IVideoRepository _videoRepository;
    private readonly IVideoCacheService? _cacheService;
    private readonly ILogger<IVideoRepository> _logger;

    public VideoService(IVideoRepository videoRepository, ILogger<IVideoRepository> logger)
    {
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public VideoService(IVideoRepository videoRepository, IVideoCacheService cacheService,
        ILogger<IVideoRepository> logger) : this(videoRepository, logger)
    {
        _cacheService = cacheService;
    }

    public async Task<List<Videos>> GetByVideosAllAsync()
    {
        if (_cacheService is not null)
        {
            var cached = await _cacheService.GetVideoListAsync(VideoCacheKeys.VideoListAll);
            if (cached is { Count: > 0 }) return cached;
        }

        var result = await _videoRepository.FindByVideoListAsync();

        if (_cacheService is not null)
            _ = _cacheService.SetVideoListAsync(VideoCacheKeys.VideoListAll, result);

        return result;
    }

    public async Task<Videos> GetByVideoAsync(Guid videoGuid)
    {
        if (_cacheService is not null)
        {
            var cached = await _cacheService.GetVideoMetaAsync(videoGuid);
            if (cached is not null) return cached;
        }

        var video = await _videoRepository.FindByVideoAsync(videoGuid);

        if (_cacheService is not null && video is not null)
            _ = _cacheService.SetVideoMetaAsync(video);

        return video;
    }

    public async Task<Videos> GetByVideoAsync(string videoName)
    {
        var key = VideoCacheKeys.VideoListByName(videoName);

        if (_cacheService is not null)
        {
            var cached = await _cacheService.GetVideoListAsync(key);
            if (cached is { Count: 1 }) return cached[0];
        }

        var video = await _videoRepository.FindByVideoName(videoName);

        if (_cacheService is not null && video is not null)
            _ = _cacheService.SetVideoMetaAsync(video);

        return video;
    }

    public async Task<List<Videos>> PagesByVideosAsync(int index, int size)
    {
        var key = VideoCacheKeys.VideoListPage(index, size);

        if (_cacheService is not null)
        {
            var cached = await _cacheService.GetVideoListAsync(key);
            if (cached is { Count: > 0 }) return cached;
        }

        var result = await _videoRepository.PageByVideoAsync(index, size);

        if (_cacheService is not null)
            _ = _cacheService.SetVideoListAsync(key, result);

        return result;
    }

    public async Task<List<Videos>> BlurredByVideoAsync(string videoName)
    {
        var key = VideoCacheKeys.VideoListBlurred(videoName);

        if (_cacheService is not null)
        {
            var cached = await _cacheService.GetVideoListAsync(key);
            if (cached is { Count: > 0 }) return cached;
        }

        var result = await _videoRepository.BlurredByVideoName(videoName);

        if (_cacheService is not null)
            _ = _cacheService.SetVideoListAsync(key, result);

        return result;
    }
}
