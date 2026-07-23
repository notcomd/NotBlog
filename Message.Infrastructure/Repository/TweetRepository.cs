using Message.Infrastructure.EntityFramework;

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
        var query = DbSet
            .Where(t => t.AuthorGuid == authorGuid)
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetTimelineAsync(IEnumerable<Guid> authorGuids, int page = 1, int pageSize = 20)
    {
        var query = DbSet
            .Where(t => t.TweetStatus == TweetStatus.Approved && authorGuids.Contains(t.AuthorGuid))
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetTrendingAsync(int page = 1, int pageSize = 20)
    {
        var query = DbSet
            .Where(t => t.TweetStatus == TweetStatus.Approved)
            .OrderByDescending(t => t.HotScore)
            .ThenByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetPendingAuditAsync(int page = 1, int pageSize = 20)
    {
        var query = DbSet
            .Where(t => t.TweetStatus == TweetStatus.Pending)
            .OrderBy(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetByStatusAsync(TweetStatus status, int page = 1, int pageSize = 20)
    {
        var query = DbSet
            .Where(t => t.TweetStatus == status)
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
}
