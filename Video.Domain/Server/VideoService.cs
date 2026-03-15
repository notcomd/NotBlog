using Microsoft.Extensions.Logging;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.ValueObjects;

namespace Video.Domain.Server;

public class VideoService(IVideoRepository videoRepository, ILogger<IVideoRepository> logger)
{
    public async Task<List<Videos>> GetByVideosAllAsync()
    {
        return await videoRepository.FindByVideoListAsync();
    }

    public async Task<Videos> GetByVideoAsync(Guid videoGUid)
    {
        return await videoRepository.FindByVideoAsync(videoGUid);
    }

    public async Task<Videos> GetByVideoAsync(string videoName)
    {
        return await videoRepository.FindByVideoName(videoName);
    }

    public async Task<List<Videos>> PagesByVideosAsync(int index, int size)
    {
        return await videoRepository.PageByVideoAsync(index, size);
    }

    public async Task<List<Videos>> BlurredByVideoAsync(string videoName)
    {
        return await videoRepository.BlurredByVideoName(videoName);
    }

    public async Task AddByVideoAsync(Videos addVideo)
    {
        await videoRepository.AddByVideoAsync(addVideo);
    }

    public async Task AddByVideoRangeAsync(List<Videos> addVideos)
    {
        await videoRepository.AddByVideoRangeAsync(addVideos);
    }

    public async Task UpdateByControlAsync(Guid videoGuid, VideoControl videoControl)
    {
        var videoModel = await GetByVideoAsync(videoGuid);
        if (videoModel.VideoControl != videoControl)
            await videoRepository.UpdateByControlAsync(videoControl);
    }

    public async Task UpdateByVideoAsync(Videos videos)
    {
        var videoModel = await videoRepository.FindByVideoAsync(videos.VideoGuid);
        if (videoModel is null)
            throw new ArgumentNullException("没有数据！");
        await videoRepository.UpdateByVideoAsync(videos);
        logger.LogInformation($"{videoModel.VideoName}更新了视频信息");
    }

    public async Task UpdateByAffiliatedUserAsync(Guid videoGuid, HashSet<Guid> userGuid, VideoControl videoControl)
    {
        var data=await videoRepository.FindByVideoAsync(videoGuid);
        if(data.Affiliated.Overlaps(userGuid))
        {
            if(data.VideoControl.Equals(videoControl)) return;
            data.
        }
    }

    public async Task UpdateByQuoteAsync(Guid videoGUid, VideoQuote videoQuote)
    {
        var videoModel = await GetByVideoAsync(videoGUid);
        if (videoModel.VideoQuote != videoQuote)
            await videoRepository.UpdateByQuoteAsync(videoQuote);
        logger.LogInformation($"{videoModel.VideoName}更新了点赞数");
    }
}