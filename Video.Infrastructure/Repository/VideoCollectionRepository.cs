using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Infrastructure.EntityFramework;

namespace Video.Infrastructure.Repository;

public class VideoCollectionRepository : IVideoCollectionRepository
{
    private readonly ILogger<IVideoCollectionRepository> _logger;

    private readonly VideoDbContext _videoDbContext;

    public VideoCollectionRepository(VideoDbContext videoDbContext, ILogger<IVideoCollectionRepository> logger)
    {
        _videoDbContext = videoDbContext;
        _logger = logger;
    }


    public async Task<VideoCollection> FindByVideoCollectionAsync(Guid findVideoCollectionGuid)
    {
        var videoCollection = await _videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionGuid == findVideoCollectionGuid)
            .Include(en => en.VideoQuote)
            .Include(v => v.VideoControl)
            .FirstOrDefaultAsync();
        if (videoCollection is null) throw new AggregateException($"[{DateTimeOffset.UtcNow}]无法查询到相关信息");
        _logger.LogWarning($"[{DateTimeOffset.UtcNow}]查询数据{findVideoCollectionGuid}完成");
        return videoCollection;
    }

    public async Task<VideoCollection> FindByVideoCollectionAsync(string findVideoCollectionName)
    {
        var videoCollection = await _videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionName == findVideoCollectionName)
            .Include(v => v.VideoControl)
            .Include(v => v.VideoQuote)
            .FirstOrDefaultAsync();
        if (videoCollection is null) throw new ArgumentNullException($"[{DateTimeOffset.UtcNow}]无法查询到数据");
        return videoCollection;
    }

    public async Task<List<VideoCollection>> BlurredByVideoCollectionAsync(string blurredVideoCollection)
    {
        var videocollection = await _videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionName.Contains(blurredVideoCollection))
            .ToListAsync();
        return videocollection;
    }

    public async Task<List<VideoCollection>> PageByVideoCollectionAsync(int page, int pageSize)
    {
        var videoCollection = await _videoDbContext.VideoCollections
            .Skip(page).Take(pageSize).ToListAsync();
        if (videoCollection is null) throw new ArgumentNullException($"[{DateTimeOffset.UtcNow}没有数据]");
        return videoCollection;
    }

    public async Task<List<VideoCollection>> FindByVideoCollectionListAsync()
    {
        var videoCollection = await _videoDbContext.VideoCollections
            .ToListAsync();
        if (videoCollection is null) throw new ArgumentNullException($"[{DateTimeOffset.UtcNow}没有数据]");
        return videoCollection;
    }

    public async Task AddByVideoCollectionAsync(VideoCollection addVideoCollection)
    {
        await _videoDbContext.VideoCollections.AddAsync(addVideoCollection);
    }


    public Task UpdateByVideoCollectionAsync(VideoCollection updataVideoCollection)
    {
        _videoDbContext.VideoCollections.Update(updataVideoCollection);
        return Task.CompletedTask;
    }

    public async Task UpdateRangeByVideoCollectionAsync(List<VideoCollection> updateVideoCollections)
    {
        foreach (var item in updateVideoCollections)
        {
            var videoCollection = await FindByVideoCollectionAsync(item.VideoCollectionGuid);
            if (videoCollection != item)
                await _videoDbContext.VideoCollections
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


    public async Task UpdateByQuoteAsync(VideoQuote videoQuote)
    {
        await _videoDbContext.VideoCollections
            .ExecuteUpdateAsync(en1 =>
                en1.SetProperty(en => en.VideoQuote, videoQuote)
            );
    }

    public async Task<List<VideoCollection>> ColmonByVideoCollectionAsync(string missing_name)
    {
        var videoCollection = await _videoDbContext.VideoCollections
            .Where(en => en.VideoCollectionName.Contains(missing_name))
            .ToListAsync();
        if (videoCollection is null) throw new ArgumentNullException($"[{DateTimeOffset.UtcNow}]数据为空");
        _logger.LogInformation($"[{DateTimeOffset.UtcNow}]查询完成{missing_name}");
        return videoCollection;
    }
}