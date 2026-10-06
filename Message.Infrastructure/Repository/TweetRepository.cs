namespace Message.Infrastructure.Repository;

/// <summary>动态（推文）仓储实现，负责 Tweets 表的查询与持久化，查询统一应用可见性过滤。</summary>
public class TweetRepository(MessageDbContext context) : ITweetRepository

{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
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

        // 状态：匿名/他人仅见 Approved；作者本人可见全部状态（Draft / Pending / Approved / Rejected）。
        // 取舍：作者查询自己的内容（含用户主页作品列表 GetByAuthorAsync）将同时展示草稿、待审核与驳回稿，
        //       便于作者看到审核进度与驳回原因；代价是主页不再只呈现"已发布"内容。
        //       他人/匿名不受影响（仍仅 Approved），时间线/社区流等已在调用侧预过滤 Approved，无泄漏。
        query = query.Where(t => t.TweetStatus == TweetStatus.Approved
                                 || (isAuth && t.AuthorGuid == viewerId));

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

    /// <summary>按动态标识获取动态，不存在时返回 null。</summary>
    public async Task<Tweet?> GetByIdAsync(Guid tweetGuid)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.TweetGuid == tweetGuid);
    }

    /// <summary>分页获取指定作者的动态（经可见性过滤），按创建时间倒序。</summary>
    public async Task<IEnumerable<Tweet>> GetByAuthorAsync(Guid authorGuid, Guid viewerId, IEnumerable<Guid> followingIds, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = ApplyVisibleTo(DbSet.Where(t => t.AuthorGuid == authorGuid), viewerId, followingIds)
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>分页获取指定作者指定状态的动态，按更新时间倒序。</summary>
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

    /// <summary>统计指定作者指定状态的动态数量。</summary>
    public async Task<int> CountByAuthorAndStatusAsync(Guid authorGuid, TweetStatus status)
    {
        return await DbSet.CountAsync(t => t.AuthorGuid == authorGuid && t.TweetStatus == status);
    }

    /// <summary>
    /// 「我的内容」：分页获取指定作者的动态，可按状态可选过滤，按创建时间倒序。
    /// <para>仅用于作者查询自己的内容（调用方已限定 UserId 为当前登录用户），故不做可见性过滤。</para>
    /// </summary>
    public async Task<IEnumerable<Tweet>> GetByAuthorWithStatusAsync(Guid authorGuid, TweetStatus? status, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet.Where(t => t.AuthorGuid == authorGuid);
        if (status.HasValue)
            query = query.Where(t => t.TweetStatus == status.Value);

        return await query
            .OrderByDescending(t => t.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>「我的内容」数量统计（与 GetByAuthorWithStatusAsync 同条件）。</summary>
    public async Task<int> CountByAuthorWithStatusAsync(Guid authorGuid, TweetStatus? status)
    {
        var query = DbSet.Where(t => t.AuthorGuid == authorGuid);
        if (status.HasValue)
            query = query.Where(t => t.TweetStatus == status.Value);

        return await query.CountAsync();
    }

    /// <summary>分页获取关注时间线（指定作者集合的非圈子动态，经可见性过滤），按创建时间倒序。</summary>
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

    /// <summary>分页获取热门动态（已通过、非圈子、公开），按热度与创建时间倒序。</summary>
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

    /// <summary>分页获取待审核动态，按创建时间升序。</summary>
    public async Task<IEnumerable<Tweet>> GetPendingAuditAsync(int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(t => t.TweetStatus == TweetStatus.Pending)
            .OrderBy(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>分页获取指定状态的非圈子动态，按创建时间倒序。</summary>
    public async Task<IEnumerable<Tweet>> GetByStatusAsync(TweetStatus status, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(t => t.TweetStatus == status && t.CircleGuid == null)
            .OrderByDescending(t => t.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>获取指定作者置顶的动态，按更新时间倒序。</summary>
    public async Task<IEnumerable<Tweet>> GetPinnedByAuthorAsync(Guid authorGuid)
    {
        return await DbSet
            .Where(t => t.AuthorGuid == authorGuid && t.IsPinned)
            .OrderByDescending(t => t.UpdateTime)
            .ToListAsync();
    }

    /// <summary>新增动态并返回已跟踪的实体。</summary>
    public async Task<Tweet> AddAsync(Tweet tweet)
    {
        var entry = await DbSet.AddAsync(tweet);
        return entry.Entity;
    }

    /// <summary>更新动态并返回已跟踪的实体。</summary>
    public async Task<Tweet> UpdateAsync(Tweet tweet)
    {
        var entry = DbSet.Update(tweet);
        return entry.Entity;
    }

    /// <summary>删除指定动态。</summary>
    public async Task DeleteAsync(Guid tweetGuid)
    {
        var tweet = await GetByIdAsync(tweetGuid);
        if (tweet is not null)
        {
            DbSet.Remove(tweet);
        }
    }

    /// <summary>判断指定动态是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid tweetGuid)
    {
        return await DbSet.AnyAsync(t => t.TweetGuid == tweetGuid);
    }

    /// <summary>统计指定作者对查看者可见的动态数量。</summary>
    public async Task<int> GetCountByAuthorAsync(Guid authorGuid, Guid viewerId, IEnumerable<Guid> followingIds)
    {
        return await ApplyVisibleTo(DbSet.Where(t => t.AuthorGuid == authorGuid), viewerId, followingIds)
            .CountAsync();
    }

    /// <summary>统计指定作者全部已通过动态的点赞总数。</summary>
    public async Task<long> GetLikeTotalByAuthorAsync(Guid authorGuid)
    {
        return await DbSet
            .Where(t => t.AuthorGuid == authorGuid && t.TweetStatus == TweetStatus.Approved)
            .SumAsync(t => (long)t.LikeCount);
    }

    /// <summary>按 ID 集合批量获取对查看者可见的动态，并还原传入顺序。</summary>
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

    /// <summary>统计待审核动态的数量。</summary>
    public async Task<int> GetPendingAuditCountAsync()
    {
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Pending);
    }

    /// <summary>全量推文总数（运营统计用，不分状态）</summary>
    public async Task<int> GetCountAllAsync()
    {
        return await DbSet.CountAsync();
    }

    /// <summary>统计关注时间线中对查看者可见的动态数量。</summary>
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

    /// <summary>统计热门动态的数量。</summary>
    public async Task<int> GetTrendingCountAsync()
    {
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Approved
                                           && t.CircleGuid == null
                                           && t.Visibility == Visibility.Public);
    }

    /// <summary>分页获取指定圈子内已通过的动态，按创建时间倒序。</summary>
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

    /// <summary>统计指定圈子内已通过的动态数量。</summary>
    public async Task<int> GetCirclePostCountAsync(Guid circleGuid)
    {
        return await DbSet.CountAsync(t => t.CircleGuid == circleGuid && t.TweetStatus == TweetStatus.Approved);
    }

    /// <summary>分页获取包含指定话题的已通过动态，按创建时间倒序。</summary>
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

    /// <summary>统计包含指定话题的已通过动态数量。</summary>
    public async Task<int> GetTopicPostCountAsync(Guid topicGuid)
    {
        var pattern = $"%{topicGuid:N}%";
        return await DbSet.CountAsync(t => t.TweetStatus == TweetStatus.Approved
                                           && EF.Functions.Like(t.TopicGuidsJson, pattern));
    }

    /// <summary>分页获取社区动态流（指定作者集合的非圈子动态，经可见性过滤），按创建时间倒序。</summary>
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

    /// <summary>统计社区动态流中可见的动态数量。</summary>
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
