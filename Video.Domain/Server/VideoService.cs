using Microsoft.Extensions.Logging;
using Video.Domain.Cache;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.ValueObjects;

namespace Video.Domain.Server;

public class VideoService
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

        if (_cacheService is not null)
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

        if (_cacheService is not null)
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

    public async Task AddByVideoAsync(Videos addVideo)
    {
        await _videoRepository.AddByVideoAsync(addVideo);

        if (_cacheService is not null)
            _ = _cacheService.InvalidateVideoListsAsync();
    }

    public async Task AddByVideoRangeAsync(List<Videos> addVideos)
    {
        await _videoRepository.AddByVideoRangeAsync(addVideos);

        if (_cacheService is not null)
            _ = _cacheService.InvalidateVideoListsAsync();
    }

    public async Task UpdateByControlAsync(Guid videoGuid, VideoControl videoControl)
    {
        var videoModel = await GetByVideoAsync(videoGuid);
        if (videoModel.VideoControl != videoControl)
        {
            await _videoRepository.UpdateByControlAsync(videoControl);
            if (_cacheService is not null)
                _ = _cacheService.RemoveVideoMetaAsync(videoGuid);
        }
    }

    public async Task UpdateByVideoAsync(Videos videos)
    {
        var videoModel = await _videoRepository.FindByVideoAsync(videos.VideoGuid);
        if (videoModel is null)
            throw new ArgumentNullException("No data found!");
        await _videoRepository.UpdateByVideoAsync(videos);
        _logger.LogInformation($"{videoModel.VideoName} updated video info");

        if (_cacheService is not null)
            _ = _cacheService.InvalidateVideoAsync(videos.VideoGuid);
    }

    public async Task UpdateByAffiliatedUserAsync(Guid videoGuid, HashSet<Guid> userGuid, VideoControl videoControl)
    {
        var data = await _videoRepository.FindByVideoAsync(videoGuid);
        if (data.Affiliated.Overlaps(userGuid))
        {
            if (data.VideoControl.Equals(videoControl)) return;
            data.VideoControl.ChangeByVideoController(videoControl);
            await _videoRepository.UpdateByControlAsync(videoControl);

            if (_cacheService is not null)
                _ = _cacheService.RemoveVideoMetaAsync(videoGuid);
        }
    }

    public async Task AddByVideoBarrageAsync(Guid videoGuid, VideoBarrage videoBarrage)
    {
        var video = await _videoRepository.FindByVideoAsync(videoGuid);
        video.AddByVideoBarrage(videoBarrage);
        await _videoRepository.UpdateByVideoAsync(video);

        if (_cacheService is not null)
            _ = _cacheService.RemoveVideoMetaAsync(videoGuid);
    }

    public async Task AddByVideoReviewAsync(Guid videoGuid, Guid userGuid, Guid? rootReview,
        string? videoReviewBody, List<VideoImage>? videoImages)
    {
        var video = await _videoRepository.FindByVideoAsync(videoGuid);
        video.AddByVideoReview(userGuid, rootReview, videoReviewBody, videoImages);
        await _videoRepository.UpdateByVideoAsync(video);

        if (_cacheService is not null)
        {
            _ = _cacheService.RemoveVideoMetaAsync(videoGuid);
            _ = _cacheService.InvalidateVideoReviewCachesAsync(videoGuid);
        }
    }

    /// <summary>
    /// Update video quote (interaction counters).
    /// Uses Redis Hash for atomic increment and persists to DB.
    /// Falls back to Redis atomic increment; DB persistence can be async.
    /// </summary>
    public async Task UpdateByQuoteAsync(Guid videoGuid, VideoQuote videoQuote)
    {
        var videoModel = await _videoRepository.FindByVideoAsync(videoGuid);
        if (videoModel.VideoQuote != videoQuote)
        {
            await _videoRepository.UpdateByQuoteAsync(videoQuote);

            // Sync to Redis cache
            if (_cacheService is not null)
                _ = _cacheService.SetVideoQuoteAsync(videoGuid, videoQuote);
        }

        _logger.LogInformation($"{videoModel.VideoName} updated interaction counts");
    }

    /// <summary>
    /// Update interaction count for a specific comment/review.
    /// Supports: upvote, stars, watch (increment only), down, ballot, share.
    /// Uses Redis Hash for atomic increment (near-real-time) and persists to DB.
    /// </summary>
    public async Task UpdateVideoReviewQuoteAsync(Guid videoGuid, Guid reviewGuid,
        string field, bool isIncrement)
    {
        var video = await _videoRepository.FindByVideoAsync(videoGuid);
        var review = video.VideoReviews?.FirstOrDefault(r => r.VideoReviewGuid == reviewGuid)
                     ?? throw new InvalidOperationException("Review not found.");

        var quote = review.VideoQuote;
        var normalized = field.ToLowerInvariant();
        var delta = isIncrement ? 1L : -1L;

        // Apply to domain object
        switch (normalized)
        {
            case "upvote":
                if (isIncrement) quote.UpUpvote(); else quote.DownUpvote();
                break;
            case "stars":
                if (isIncrement) quote.UpStars(); else quote.DownStars();
                break;
            case "watch":
                if (isIncrement) quote.UpWatch();
                break;
            case "down":
                if (isIncrement) quote.UpDown(); else quote.DownDown();
                break;
            case "ballot":
                if (isIncrement) quote.UpBallot(); else quote.DownBallot();
                break;
            case "share":
                if (isIncrement) quote.UpShare(); else quote.DownShare();
                break;
            default:
                throw new ArgumentException($"Invalid interaction field: {field}");
        }

        await _videoRepository.UpdateByVideoAsync(video);

        // Redis: atomic increment for near-real-time counters + invalidate review list
        if (_cacheService is not null)
        {
            _ = _cacheService.IncrementReviewQuoteFieldAsync(reviewGuid, normalized, delta);
            _ = _cacheService.RemoveVideoMetaAsync(videoGuid);
            _ = _cacheService.InvalidateVideoReviewCachesAsync(videoGuid);
        }

        _logger.LogInformation("Review {ReviewGuid} on video {VideoGuid}: {Field} {Direction}",
            reviewGuid, videoGuid, normalized, isIncrement ? "incremented" : "decremented");
    }
}