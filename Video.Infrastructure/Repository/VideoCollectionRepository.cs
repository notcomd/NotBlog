using Commons.SeedWork;

namespace Video.Infrastructure.Repository;

/// <summary>
/// 收藏夹仓储实现 — 基于 VideoDbContext，提供收藏夹查询、分页、互动计数更新与软/硬删除。
/// </summary>
public class VideoCollectionRepository(VideoDbContext videoDbContext, ILogger<IVideoCollectionRepository> logger)
    : IVideoCollectionRepository
{
    
    /// <summary>工作单元（VideoDbContext）。</summary>
    public IUnitOfWork UnitOfWork => videoDbContext;
    
    
    
    public async Task<VideoCollection> FindByVideoCollectionAsync(Guid findVideoCollectionGuid)
    {
        var videoCollection = await videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionGuid == findVideoCollectionGuid)
            .Include(en => en.VideoQuote)
            .Include(v => v.VideoControl)
            .FirstOrDefaultAsync();
        if (videoCollection is null) throw new AggregateException($"[{DateTimeOffset.UtcNow}]无法查询到相关信息");
        logger.LogWarning($"[{DateTimeOffset.UtcNow}]查询数据{findVideoCollectionGuid}完成");
        return videoCollection;
    }

    public async Task<VideoCollection> FindByVideoCollectionAsync(string findVideoCollectionName)
    {
        var videoCollection = await videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionName == findVideoCollectionName)
            .Include(v => v.VideoControl)
            .Include(v => v.VideoQuote)
            .FirstOrDefaultAsync();
        if (videoCollection is null) throw new ArgumentNullException($"[{DateTimeOffset.UtcNow}]无法查询到数据");
        return videoCollection;
    }

    public async Task<List<VideoCollection>> BlurredByVideoCollectionAsync(string blurredVideoCollection)
    {
        var videocollection = await videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionName.Contains(blurredVideoCollection))
            .ToListAsync();
        return videocollection;
    }

    public async Task<List<VideoCollection>> PageByVideoCollectionAsync(int page, int pageSize)
    {
        var videoCollection = await videoDbContext.VideoCollections
            .Skip(page).Take(pageSize).ToListAsync();
        if (videoCollection is null) throw new ArgumentNullException($"[{DateTimeOffset.UtcNow}没有数据]");
        return videoCollection;
    }

    public async Task<List<VideoCollection>> FindByVideoCollectionListAsync()
    {
        var videoCollection = await videoDbContext.VideoCollections
            .ToListAsync();
        if (videoCollection is null) throw new ArgumentNullException($"[{DateTimeOffset.UtcNow}没有数据]");
        return videoCollection;
    }

    public async Task AddByVideoCollectionAsync(VideoCollection addVideoCollection)
    {
        await videoDbContext.VideoCollections.AddAsync(addVideoCollection);
    }


    public Task UpdateByVideoCollectionAsync(VideoCollection updateVideoCollection)
    {
        videoDbContext.VideoCollections.Update(updateVideoCollection);
        return Task.CompletedTask;
    }

    public async Task UpdateRangeByVideoCollectionAsync(List<VideoCollection> updateVideoCollections)
    {
        foreach (var item in updateVideoCollections)
        {
            var videoCollection = await FindByVideoCollectionAsync(item.VideoCollectionGuid);
            if (videoCollection != item)
                await videoDbContext.VideoCollections
                    .Where(en => en.VideoCollectionGuid == item.VideoCollectionGuid)
                    .ExecuteUpdateAsync(en1 =>
                        en1.SetProperty(en => en.VideoCollectionName, item.VideoCollectionName)
                            .SetProperty(en => en.VideoCollectionBriefIntroduction,
                                item.VideoCollectionBriefIntroduction)
                            .SetProperty(en => en.VideoControl, item.VideoControl)
                            .SetProperty(en => en.AffiliatedUser, item.AffiliatedUser)
                    );
        }
    }


    public async Task UpdateByQuoteAsync(Guid videoCollectionGuid, VideoQuote videoQuote)
    {
        await videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionGuid == videoCollectionGuid)
            .ExecuteUpdateAsync(en1 =>
                en1.SetProperty(en => en.VideoQuote, videoQuote)
            );
    }

    // ── Standard Delete Operations ──

    public async Task DeleteByIdAsync(Guid id)
    {
        await videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionGuid == id)
            .ExecuteUpdateAsync(up => up.SetProperty(en => en.VideoControl.VideoDelete, true));
    }

    public async Task InDeleteByIdAsync(Guid id)
    {
        await videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionGuid == id)
            .ExecuteDeleteAsync();
    }
}