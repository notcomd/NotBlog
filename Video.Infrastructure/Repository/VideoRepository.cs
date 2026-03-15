using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.SeedWork;
using Video.Domain.ValueObjects;
using Video.Infrastructure.EntityFramework;

namespace Video.Infrastructure.Repository;

public class VideoRepository(ILogger<IVideoRepository> videoLogger, VideoDbContext videoDbContext)
    : IVideoRepository
{
    
    public IUnitOfWork UnitOfWork=> videoDbContext;
    
    public async Task<List<Videos>> FindByVideoListAsync()
    {
        var videoModel = await videoDbContext.Videos
            .Include(en => en.VideoQuote)
            .Include(en => en.VideoControl)
            .ToListAsync();
        return videoModel;
    }

    public async Task<Videos> FindByVideoAsync(Guid findVideoGuid)
    {
        var videoModel = await videoDbContext.Videos
            .SingleOrDefaultAsync(en => en.VideoGuid == findVideoGuid);
        if (videoModel is null) throw new AggregateException($"[{DateTimeOffset.UtcNow}]无法查询到相关信息");
        videoLogger.LogWarning($"[{DateTimeOffset.UtcNow}]查询数据{findVideoGuid}完成");
        return videoModel;
    }

    public async Task<Videos> FindByNvidAsync(string nvId)
    {
        var videoModel = await videoDbContext.Videos
            .SingleOrDefaultAsync(en => en.VideoNvid == nvId);
        if (videoModel is null) throw new AggregateException($"[{DateTimeOffset.UtcNow}]无法查询到相关信息");
        videoLogger.LogWarning($"[{DateTimeOffset.UtcNow}]查询数据{nvId}完成");
        return videoModel;
    }

    public async Task<Videos> FindByVideoAsync(string blurredVideoName)
    {
        var videoModel = await videoDbContext.Videos
            .SingleOrDefaultAsync(en => en.VideoName.Contains(blurredVideoName));
        if (videoModel is null) throw new AggregateException($"[{DateTimeOffset.UtcNow}]无法查询到相关信息");
        videoLogger.LogWarning($"[{DateTimeOffset.UtcNow}]查询数据{blurredVideoName}完成");
        return videoModel;
    }

    public async Task AddByVideoAsync(Videos addVideo)
    {
        await videoDbContext.Videos.AddAsync(addVideo);
    }

    public async Task AddByVideoRangeAsync(List<Videos> addVideos)
    {
        await videoDbContext.Videos.AddRangeAsync(addVideos);
    }

    public async Task UpdateByQuoteAsync(VideoQuote videoQuote)
    {
        await videoDbContext.Videos
            .ExecuteUpdateAsync(en => en.SetProperty(ens => ens.VideoQuote, videoQuote)
            );
    }

    public async Task UpdateByControlAsync(VideoControl videoControl)
    {
        await videoDbContext.Videos
            .ExecuteUpdateAsync(en => en.SetProperty(ens => ens.VideoControl, videoControl)
            );
    }


    public async Task UpdateByTimeSpaceAsync(TimeSpace timeSpace)
    {
        await videoDbContext.Videos
            .ExecuteUpdateAsync(en => en.SetProperty(ens => ens.TimeSpace, timeSpace)
            );
    }


    public async Task<List<Videos>> PageByVideoAsync(int page, int pageSize)
    {
        var videoModel = await videoDbContext.Videos
            .Include(en => en.VideoQuote)
            .Include(en => en.VideoControl)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return videoModel;
    }

    public async Task UpdateByVideoAsync(Videos videos)
    {
        await videoDbContext.Videos
            .ExecuteUpdateAsync(en => en.SetProperty(ens => ens.VideoName, videos.VideoName)
                .SetProperty(ens => ens.VideoCover, videos.VideoCover)
                .SetProperty(ens => ens.VideoTags, videos.VideoTags)
                .SetProperty(ens => ens.BriefIntroduction, videos.BriefIntroduction)
                .SetProperty(ens => ens.VideoCover, videos.VideoCover)
            );
    }

    public async Task<Videos> FindByVideoName(string name)
    {
        var videoModel = await videoDbContext.Videos
            .SingleOrDefaultAsync(en => en.VideoName == name);
        if (videoModel is null) throw new AggregateException($"[{DateTimeOffset.UtcNow}]无法查询到相关信息");
        videoLogger.LogWarning($"[{DateTimeOffset.UtcNow}]查询数据{name}完成");
        return videoModel;
    }


    public async Task<List<Videos>> BlurredByVideoName(string videoName)
    {
        var videoModel = await videoDbContext.Videos
            .Where(en => en.VideoName.Contains(videoName))
            .ToListAsync();
        return videoModel;
    }


    public async Task DeleteByVideoControlAsync(VideoControl videoControl)
    {
        await videoDbContext.Videos
            .ExecuteUpdateAsync(up => up.SetProperty(en => en.VideoControl.VideoDelete, videoControl.VideoDelete)
            );
    }


    public async Task DeleteByVideoControlRangeAsync(List<VideoControl> videoControl)
    {
        foreach (var item in videoControl)
            await videoDbContext.Videos
                .ExecuteUpdateAsync(up => up.SetProperty(en => en.VideoControl.VideoDelete, item.VideoDelete)
                );
    }


    public async Task InDeleteByVideoAsync(Videos videoControl)
    {
        await videoDbContext.Videos
            .Where(en => en.VideoGuid == videoControl.VideoGuid)
            .ExecuteDeleteAsync();
    }


    public async Task InDeleteByVideoRangeAsync(List<Videos> videosList)
    {
        foreach (var item in videosList)
            await videoDbContext.Videos
                .Where(en => en.VideoGuid == item.VideoGuid)
                .ExecuteDeleteAsync();
    }
}