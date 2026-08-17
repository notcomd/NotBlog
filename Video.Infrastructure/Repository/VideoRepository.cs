using Commons.SeedWork;

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
            .Include(en => en.VideoBarrageList!.Where(en => !en.IsDelete))
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

    public async Task<Videos> FindByVideoWithDetailsAsync(Guid findVideoGuid)
    {
        var videoModel = await videoDbContext.Videos
            .Include(en => en.VideoQuote)
            .Include(en => en.VideoControl)
            .Include(en => en.VideoReviews)
            .Include(en => en.VideoBarrageList!.Where(en => !en.IsDelete))
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

    public async Task UpdateByQuoteAsync(Guid videoGuid, VideoQuote videoQuote)
    {
        await videoDbContext.Videos
            .Where(en => en.VideoGuid == videoGuid)
            .ExecuteUpdateAsync(en => en.SetProperty(ens => ens.VideoQuote, videoQuote)
            );
    }

    public async Task UpdateByControlAsync(Guid videoGuid, VideoControl videoControl)
    {
        await videoDbContext.Videos
            .Where(en => en.VideoGuid == videoGuid)
            .ExecuteUpdateAsync(en => en.SetProperty(ens => ens.VideoControl, videoControl)
            );
    }


    public async Task UpdateByTimeSpaceAsync(Guid videoGuid, TimeSpace timeSpace)
    {
        await videoDbContext.Videos
            .Where(en => en.VideoGuid == videoGuid)
            .ExecuteUpdateAsync(en => en.SetProperty(ens => ens.TimeSpace, timeSpace)
            );
    }


    public async Task<List<Videos>> PageByVideoAsync(int page, int pageSize)
    {
        var videoModel = await videoDbContext.Videos
            .Include(en => en.VideoQuote)
            .Include(en=>en.VideoBarrageList!.Where(en => !en.IsDelete))
            .Include(en => en.VideoControl)
            .Where(en => !en.VideoControl.VideoDelete
                && en.VideoControl.AuthorVideo == AuthorVideo.VideoPublic
                && en.VideoControl.VideoDisplay)
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
            .Where(en => en.VideoName == name
                && !en.VideoControl.VideoDelete
                && en.VideoControl.AuthorVideo == AuthorVideo.VideoPublic
                && en.VideoControl.VideoDisplay)
            .SingleOrDefaultAsync();
        if (videoModel is null) throw new AggregateException($"[{DateTimeOffset.UtcNow}]无法查询到相关信息");
        videoLogger.LogWarning($"[{DateTimeOffset.UtcNow}]查询数据{name}完成");
        return videoModel;
    }


    /// <summary>
    /// 根据视频名称模糊查询视频列表
    /// </summary>
    /// <param name="videoName"></param>
    /// <returns></returns>
    public async Task<List<Videos>> BlurredByVideoName(string videoName)
    {
        var videoModel = await videoDbContext.Videos
            .Where(en => en.VideoName.Contains(videoName)
                && !en.VideoControl.VideoDelete
                && en.VideoControl.AuthorVideo == AuthorVideo.VideoPublic
                && en.VideoControl.VideoDisplay)
            .ToListAsync();
        return videoModel;
    }


    public async Task DeleteByVideoControlAsync(Guid videoGuid, VideoControl videoControl)
    {
        await videoDbContext.Videos
            .Where(en => en.VideoGuid == videoGuid)
            .ExecuteUpdateAsync(up => up.SetProperty(en => en.VideoControl.VideoDelete, videoControl.VideoDelete)
            );
    }


    public async Task DeleteByVideoControlRangeAsync(List<Videos> videosList)
    {
        foreach (var item in videosList)
            await videoDbContext.Videos
                .Where(en => en.VideoGuid == item.VideoGuid)
                .ExecuteUpdateAsync(up => up.SetProperty(en => en.VideoControl.VideoDelete, item.VideoControl.VideoDelete)
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

    // ── Standard Delete Operations ──

    public async Task InDeleteByIdAsync(Guid videoGuid)
    {
        await videoDbContext.Videos
            .Where(en => en.VideoGuid == videoGuid)
            .ExecuteDeleteAsync();
    }
}