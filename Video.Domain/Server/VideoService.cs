using Microsoft.Extensions.Logging;
using Video.Domain.Entities;
using Video.Domain.IRepository;

namespace Video.Domain.Server;

public class VideoService
{
    private readonly ILogger<IVideoRepository> _logger;
    private readonly IVideoRepository _videoRepository;

    public VideoService(IVideoRepository videoRepository, ILogger<IVideoRepository> logger)
    {
        _logger = logger;
        _videoRepository = videoRepository;
    }

    public async Task<List<Videos>> GetByVideosAllAsync()
    {
        return await _videoRepository.FindByVideoListAsync();
    }

    public async Task<Videos> GetByVideoAsync(Guid videoGUid)
    {
        return await _videoRepository.FindByVideoAsync(videoGUid);
    }

    public async Task<Videos> GetByVideoAsync(string videoName)
    {
        return await _videoRepository.FindByVideoName(videoName);
    }

    public async Task<List<Videos>> PagesByVideosAsync(int index, int size)
    {
        return await _videoRepository.PageByVideoAsync(index, size);
    }

    public async Task<List<Videos>> BlurredByVideoAsync(string videoName)
    {
        return await _videoRepository.BlurredByVideoName(videoName);
    }

    public async Task AddByVideoAsync(Videos addVideo)
    {
        await _videoRepository.AddByVideoAsync(addVideo);
    }

    public async Task AddByVideoRangeAsync(List<Videos> addVideos)
    {
        await _videoRepository.AddByVideoRangeAsync(addVideos);
    }

    public async Task UpdateByControlAsync(Guid videoGuid, VideoControl videoControl)
    {
        var videoModel = await GetByVideoAsync(videoGuid);
        if (videoModel.VideoControl != videoControl)
            await _videoRepository.UpdateByControlAsync(videoControl);
    }

    public async Task UpdateByVideoAsync(Videos videos)
    {
        var videoModel = await _videoRepository.FindByVideoAsync(videos.VideoGuid);
        if (videoModel is null)
            throw new ArgumentNullException($"没有数据！");
        await _videoRepository.UpdateByVideoAsync(videos);
        _logger.LogInformation($"{videoModel.VideoName}更新了视频信息");
    }

    public async Task UpdateByAffiliatedUserAsync(Guid videoGuid, Guid userGuid, VideoControl videoControl)
    {
        var videoModel = await GetByVideoAsync(videoGuid);
        if (videoModel.Affiliated.Select(sdf => sdf.AffiliatedUserUuid == userGuid).Any() && videoModel.Affiliated.Select(en => en.AffiliatedAuthorize == AffiliatedAuthorize.AffiliatedAuthorizeAdmin).Any())
        {
            if (videoModel.VideoControl.Equals(videoControl)) return;
            await _videoRepository.UpdateByControlAsync(videoModel.VideoControl);
        }
    }

    public async Task UpdateByQuoteAsync(Guid videoGUid, VideoQuote videoQuote)
    {
        var videoModel = await GetByVideoAsync(videoGUid);
        if (videoModel.VideoQuote != videoQuote)
            await _videoRepository.UpdateByQuoteAsync(videoQuote);
        _logger.LogInformation($"{videoModel.VideoName}更新了点赞数");
    }
}