
namespace Message.Infrastructure.Repository;

public class TweetRepository(MessageDbContext context) : ITweetRepository

{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Tweet> DbSet = context.Tweets;

    public async Task<Tweet?> GetByIdAsync(Guid tweetGuid)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.TweetGuid == tweetGuid);
    }

    public async Task<IEnumerable<Tweet>> GetByAuthorAsync(Guid authorGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(t => t.AuthorGuid == authorGuid)
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetTimelineAsync(IEnumerable<Guid> authorGuids, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(t => t.TweetStatus == TweetStatus.Approved
                        && t.CircleGuid == null
                        && authorGuids.Contains(t.AuthorGuid))
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetTrendingAsync(int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(t => t.TweetStatus == TweetStatus.Approved && t.CircleGuid == null)
            .OrderByDescending(t => t.HotScore)
            .ThenByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetPendingAuditAsync(int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(t => t.TweetStatus == TweetStatus.Pending)
            .OrderBy(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetByStatusAsync(TweetStatus status, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(t => t.TweetStatus == status && t.CircleGuid == null)
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetPinnedByAuthorAsync(Guid authorGuid)
    {
        return await DbSet
            .Where(t => t.AuthorGuid == authorGuid && t.IsPinned)
            .OrderByDescending(t => t.UpdateTime)
            .ToListAsync();
    }

    public async Task<Tweet> AddAsync(Tweet tweet)
    {
        var entry = await DbSet.AddAsync(tweet);
        return entry.Entity;
    }

    public async Task<Tweet> UpdateAsync(Tweet tweet)
    {
        var entry = DbSet.Update(tweet);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid tweetGuid)
    {
        var tweet = await GetByIdAsync(tweetGuid);
        if (tweet is not null)
        {
            DbSet.Remove(tweet);
        }
    }

    public async Task<bool> ExistsAsync(Guid tweetGuid)
    {
        return await DbSet.AnyAsync(t => t.TweetGuid == tweetGuid);
    }

    public async Task<int> GetCountByAuthorAsync(Guid authorGuid)
    {
        return await DbSet.CountAsync(t => t.AuthorGuid == authorGuid);
    }

    public async Task<int> GetPendingAuditCountAsync()
    {
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Pending);
    }

    public async Task<int> GetTimelineCountAsync(IEnumerable<Guid> authorGuids)
    {
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Approved
            && t.CircleGuid == null
            && authorGuids.Contains(t.AuthorGuid));
    }

    public async Task<int> GetTrendingCountAsync()
    {
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Approved && t.CircleGuid == null);
    }

    public async Task<IEnumerable<Tweet>> GetByCircleAsync(Guid circleGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        return await DbSet
            .Where(t => t.CircleGuid == circleGuid && t.TweetStatus == TweetStatus.Approved)
            .OrderByDescending(t => t.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCirclePostCountAsync(Guid circleGuid)
    {
        return await DbSet.CountAsync(t => t.CircleGuid == circleGuid && t.TweetStatus == TweetStatus.Approved);
    }

    public async Task<IEnumerable<Tweet>> GetByTopicAsync(Guid topicGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var pattern = $"%{topicGuid:N}%";
        return await DbSet
            .Where(t => t.TweetStatus == TweetStatus.Approved
                        && EF.Functions.Like(t.TopicGuidsJson, pattern))
            .OrderByDescending(t => t.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTopicPostCountAsync(Guid topicGuid)
    {
        var pattern = $"%{topicGuid:N}%";
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Approved
                                           && EF.Functions.Like(t.TopicGuidsJson, pattern));
    }

    public async Task<IEnumerable<Tweet>> GetCommunityFeedAsync(IEnumerable<Guid> authorGuids, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var ids = authorGuids.Distinct().ToArray();
        return await DbSet
            .Where(t => t.TweetStatus == TweetStatus.Approved
                        && t.CircleGuid == null
                        && ids.Contains(t.AuthorGuid))
            .OrderByDescending(t => t.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCommunityFeedCountAsync(IEnumerable<Guid> authorGuids)
    {
        var ids = authorGuids.Distinct().ToArray();
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Approved
                                           && t.CircleGuid == null
                                           && ids.Contains(t.AuthorGuid));
    }
}
