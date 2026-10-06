using Commons.SeedWork;

namespace Video.Infrastructure.Repository;

/// <summary>
/// 视频仓储实现 — 基于 VideoDbContext，提供视频查询、分页、互动计数/控制更新与软/硬删除。
/// </summary>
public class VideoRepository(ILogger<IVideoRepository> videoLogger, VideoDbContext videoDbContext)
    : IVideoRepository
{
    
    /// <summary>工作单元（VideoDbContext）。</summary>
    public IUnitOfWork UnitOfWork=> videoDbContext;
    
    public async Task<List<Videos>> FindByVideoListAsync(Guid? viewerGuid, bool isAdmin)
    {
        var query = videoDbContext.Videos
            .Include(en => en.VideoQuote)
            .Include(en => en.VideoControl)
            .Include(en => en.VideoBarrageList!.Where(en => !en.IsDelete))
            .Where(en => !en.VideoControl.VideoDelete);

        if (!isAdmin)
        {
            if (viewerGuid is { } uid && uid != Guid.Empty)
            {
                // 作者可见自己的全部状态；其他人仅可见「已审核通过 + 公开」的视频
                query = query.Where(en => en.Affiliated.Contains(uid)
                    || (en.Status == VideoStatus.Approved
                        && en.VideoControl.AuthorVideo == AuthorVideo.VideoPublic));
            }
            else
            {
                // 匿名/未登录：仅公开且已审核通过
                query = query.Where(en => en.Status == VideoStatus.Approved
                    && en.VideoControl.AuthorVideo == AuthorVideo.VideoPublic);
            }
        }

        var videoModel = await query.ToListAsync();
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

    /// <inheritdoc />
    public async Task<List<Videos>> PageByAuthorAsync(Guid authorGuid, VideoStatus? status, int page, int pageSize)
    {
        var query = videoDbContext.Videos
            .Include(en => en.VideoControl)
            .Where(en => !en.VideoControl.VideoDelete && en.Affiliated.Contains(authorGuid));

        if (status is { } value)
            query = query.Where(en => en.Status == value);

        return await query
            .OrderByDescending(en => en.TimeSpace.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> CountByAuthorAsync(Guid authorGuid, VideoStatus? status)
    {
        var query = videoDbContext.Videos
            .Where(en => !en.VideoControl.VideoDelete && en.Affiliated.Contains(authorGuid));

        if (status is { } value)
            query = query.Where(en => en.Status == value);

        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task<List<Videos>> PageByStatusAsync(VideoStatus? status, int page, int pageSize)
    {
        var query = videoDbContext.Videos
            .Include(en => en.VideoControl)
            .Where(en => !en.VideoControl.VideoDelete);

        if (status is { } value)
            query = query.Where(en => en.Status == value);

        return await query
            .OrderByDescending(en => en.TimeSpace.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> CountByStatusAsync(VideoStatus? status)
    {
        var query = videoDbContext.Videos
            .Where(en => !en.VideoControl.VideoDelete);

        if (status is { } value)
            query = query.Where(en => en.Status == value);

        return await query.CountAsync();
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