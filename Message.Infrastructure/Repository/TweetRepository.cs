namespace Message.Infrastructure.Repository;

public class TweetRepository(MessageDbContext context) : ITweetRepository

{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Tweet> DbSet = context.Tweets;

    /// <summary>
    /// R-02/R-03/R-07：对推文查询应用"对查看者可见"过滤（状态/可见性/圈子），全部在 SQL 层完成，
    /// 保证分页列表与 TotalCount 使用同一条件，杜绝内存过滤导致的分页不一致与 Private 泄露。
    /// </summary>
    /// <param name="query">已按业务范围过滤的推文查询</param>
    /// <param name="viewerId">查看者 ID（未认证为 Guid.Empty）</param>
    /// <param name="followingIds">查看者的关注集合（Followers 可见性判定）</param>
    private IQueryable<Tweet> ApplyVisibleTo(IQueryable<Tweet> query, Guid viewerId, IEnumerable<Guid> followingIds)
    {
        var following = followingIds.Distinct().ToArray();
        var isAuth = viewerId != Guid.Empty;

        // 状态：Approved 全员可见；草稿仅作者本人（用户主页场景保留草稿展示）
        query = query.Where(t => t.TweetStatus == TweetStatus.Approved
                                 || (isAuth && t.AuthorGuid == viewerId && t.TweetStatus == TweetStatus.Draft));

        // 可见性：Public 全员可见；Private 仅作者；Followers 仅查看者关注列表内的作者（R-03 接入关注关系）
        query = query.Where(t => t.Visibility == Visibility.Public
                                 || t.AuthorGuid == viewerId
                                 || (t.Visibility == Visibility.Followers && following.Contains(t.AuthorGuid)));

        // 圈子帖：仅作者本人或圈子成员（Active）可见，非成员视为不存在
        query = query.Where(t => t.CircleGuid == null
                                 || t.AuthorGuid == viewerId
                                 || (isAuth && context.CircleMembers.Any(cm =>
                                     cm.CircleGuid == t.CircleGuid
                                     && cm.UserGuid == viewerId
                                     && cm.Status == CircleMemberStatus.Active)));

        return query;
    }

    public async Task<Tweet?> GetByIdAsync(Guid tweetGuid)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.TweetGuid == tweetGuid);
    }

    public async Task<IEnumerable<Tweet>> GetByAuthorAsync(Guid authorGuid, Guid viewerId, IEnumerable<Guid> followingIds, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = ApplyVisibleTo(DbSet.Where(t => t.AuthorGuid == authorGuid), viewerId, followingIds)
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetByAuthorAndStatusAsync(Guid authorGuid, TweetStatus status, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        return await DbSet
            .Where(t => t.AuthorGuid == authorGuid && t.TweetStatus == status)
            .OrderByDescending(t => t.UpdateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByAuthorAndStatusAsync(Guid authorGuid, TweetStatus status)
    {
        return await DbSet.CountAsync(t => t.AuthorGuid == authorGuid && t.TweetStatus == status);
    }

    public async Task<IEnumerable<Tweet>> GetTimelineAsync(IEnumerable<Guid> authorGuids, Guid viewerId, IEnumerable<Guid> followingIds, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var ids = authorGuids.Distinct().ToArray();
        var query = ApplyVisibleTo(
                DbSet.Where(t => t.TweetStatus == TweetStatus.Approved
                                 && t.CircleGuid == null
                                 && ids.Contains(t.AuthorGuid)),
                viewerId, followingIds)
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Tweet>> GetTrendingAsync(int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(t => t.TweetStatus == TweetStatus.Approved
                        && t.CircleGuid == null
                        && t.Visibility == Visibility.Public)
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

    public async Task<int> GetCountByAuthorAsync(Guid authorGuid, Guid viewerId, IEnumerable<Guid> followingIds)
    {
        return await ApplyVisibleTo(DbSet.Where(t => t.AuthorGuid == authorGuid), viewerId, followingIds)
            .CountAsync();
    }

    public async Task<long> GetLikeTotalByAuthorAsync(Guid authorGuid)
    {
        return await DbSet
            .Where(t => t.AuthorGuid == authorGuid && t.TweetStatus == TweetStatus.Approved)
            .SumAsync(t => (long)t.LikeCount);
    }

    public async Task<IEnumerable<Tweet>> GetVisibleByIdsAsync(IEnumerable<Guid> tweetGuids, Guid viewerId, IEnumerable<Guid> followingIds)
    {
        var ids = tweetGuids.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        var visible = await ApplyVisibleTo(
                DbSet.Where(t => ids.Contains(t.TweetGuid)),
                viewerId, followingIds)
            .ToListAsync();

        // 还原传入顺序（互动列表按收藏时间倒序）
        var byId = visible.ToDictionary(t => t.TweetGuid);
        return ids.Select(id => byId.GetValueOrDefault(id)).Where(t => t is not null).Cast<Tweet>();
    }

    public async Task<int> GetPendingAuditCountAsync()
    {
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Pending);
    }

    /// <summary>全量推文总数（运营统计用，不分状态）</summary>
    public async Task<int> GetCountAllAsync()
    {
        return await DbSet.CountAsync();
    }

    public async Task<int> GetTimelineCountAsync(IEnumerable<Guid> authorGuids, Guid viewerId, IEnumerable<Guid> followingIds)
    {
        var ids = authorGuids.Distinct().ToArray();
        return await ApplyVisibleTo(
                DbSet.Where(t => t.TweetStatus == TweetStatus.Approved
                                 && t.CircleGuid == null
                                 && ids.Contains(t.AuthorGuid)),
                viewerId, followingIds)
            .CountAsync();
    }

    public async Task<int> GetTrendingCountAsync()
    {
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Approved
                                           && t.CircleGuid == null
                                           && t.Visibility == Visibility.Public);
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

    public async Task<IEnumerable<Tweet>> GetCommunityFeedAsync(IEnumerable<Guid> authorGuids, Guid viewerId, IEnumerable<Guid> followingIds, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var ids = authorGuids.Distinct().ToArray();
        var query = ApplyVisibleTo(
                DbSet.Where(t => t.TweetStatus == TweetStatus.Approved
                                 && t.CircleGuid == null
                                 && ids.Contains(t.AuthorGuid)),
                viewerId, followingIds)
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetCommunityFeedCountAsync(IEnumerable<Guid> authorGuids, Guid viewerId, IEnumerable<Guid> followingIds)
    {
        var ids = authorGuids.Distinct().ToArray();
        return await ApplyVisibleTo(
                DbSet.Where(t => t.TweetStatus == TweetStatus.Approved
                                 && t.CircleGuid == null
                                 && ids.Contains(t.AuthorGuid)),
                viewerId, followingIds)
            .CountAsync();
    }
}
