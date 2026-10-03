
namespace Message.Domain.IRepository;

/// <summary>
/// 推文仓储接口（Tweet 聚合根）。
/// </summary>
public interface ITweetRepository : IRepository<Tweet, IUnitOfWork>
{
    /// <summary>
    /// 获取推文详情
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <returns>推文详情</returns>
    Task<Tweet?> GetByIdAsync(Guid tweetGuid);
    
    /// <summary>
    /// 获取用户推文列表（R-02/R-03/R-07：SQL 层完成可见性/状态/圈子过滤，分页与计数同条件）
    /// </summary>
    /// <param name="authorGuid">作者ID</param>
    /// <param name="viewerId">查看者ID（未认证为 Guid.Empty）</param>
    /// <param name="followingIds">查看者的关注集合（Followers 可见性判定）</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>推文列表</returns>
    Task<IEnumerable<Tweet>> GetByAuthorAsync(Guid authorGuid, Guid viewerId, IEnumerable<Guid> followingIds, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取指定作者指定状态的推文列表（R-08：草稿箱，按最近编辑倒序）
    /// </summary>
    Task<IEnumerable<Tweet>> GetByAuthorAndStatusAsync(Guid authorGuid, TweetStatus status, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取指定作者指定状态的推文数量（R-08：草稿箱 TotalCount）
    /// </summary>
    Task<int> CountByAuthorAndStatusAsync(Guid authorGuid, TweetStatus status);

    /// <summary>
    /// 获取多个作者的时间线推文列表（R-02/R-03/R-07：SQL 层完成可见性过滤）
    /// </summary>
    /// <param name="authorGuids">作者ID列表</param>
    /// <param name="viewerId">查看者ID（未认证为 Guid.Empty）</param>
    /// <param name="followingIds">查看者的关注集合（Followers 可见性判定）</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>指定作者的时间线推文列表</returns>
    Task<IEnumerable<Tweet>> GetTimelineAsync(IEnumerable<Guid> authorGuids, Guid viewerId, IEnumerable<Guid> followingIds, int page = 1, int pageSize = 20);
    
    /// <summary>
    /// 获取热门推文列表（按热度排序）
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>热门推文列表</returns>
    Task<IEnumerable<Tweet>> GetTrendingAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取待审核推文列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>趋势待审核推文列表</returns>
    Task<IEnumerable<Tweet>> GetPendingAuditAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取指定状态的推文列表
    /// </summary>
    /// <param name="status">推文状态</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>指定状态的推文列表</returns>
    Task<IEnumerable<Tweet>> GetByStatusAsync(TweetStatus status, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取指定作者的置顶推文列表
    /// </summary>
    /// <param name="authorGuid">作者ID</param>
    /// <returns>指定作者的置顶推文列表</returns>
    Task<IEnumerable<Tweet>> GetPinnedByAuthorAsync(Guid authorGuid);

    /// <summary>
    /// 添加推文
    /// </summary>
    /// <param name="tweet">推文</param>
    /// <returns>添加的推文</returns>
    Task<Tweet> AddAsync(Tweet tweet);

    /// <summary>
    /// 更新推文
    /// </summary>
    /// <param name="tweet">推文</param>
    /// <returns>更新后的推文</returns>
    Task<Tweet> UpdateAsync(Tweet tweet);

    /// <summary>
    /// 删除推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    Task DeleteAsync(Guid tweetGuid);

    /// <summary>
    /// 检查推文是否存在
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Guid tweetGuid);

    /// <summary>
    /// 获取用户推文数量（与 GetByAuthorAsync 同条件，R-07 分页计数对齐）
    /// </summary>
    /// <param name="authorGuid">作者ID</param>
    /// <param name="viewerId">查看者ID（未认证为 Guid.Empty）</param>
    /// <param name="followingIds">查看者的关注集合（Followers 可见性判定）</param>
    /// <returns>指定作者对查看者可见的推文数量</returns>
    Task<int> GetCountByAuthorAsync(Guid authorGuid, Guid viewerId, IEnumerable<Guid> followingIds);

    /// <summary>
    /// 获取作者已发布作品（Approved 状态，公开口径）的获赞总数（个人主页统计用，忽略可见性以稳定对外口径）
    /// </summary>
    /// <param name="authorGuid">作者ID</param>
    /// <returns>获赞总数</returns>
    Task<long> GetLikeTotalByAuthorAsync(Guid authorGuid);

    /// <summary>
    /// 按 ID 批量获取推文（可见性过滤；保持传入顺序 — 收藏/点赞列表等按互动时间倒序场景使用）
    /// </summary>
    /// <param name="tweetGuids">推文 ID 列表</param>
    /// <param name="viewerId">查看者ID（未认证为 Guid.Empty）</param>
    /// <param name="followingIds">查看者的关注集合（Followers 可见性判定）</param>
    /// <returns>可见推文列表（按 <paramref name="tweetGuids"/> 顺序）</returns>
    Task<IEnumerable<Tweet>> GetVisibleByIdsAsync(IEnumerable<Guid> tweetGuids, Guid viewerId, IEnumerable<Guid> followingIds);

    /// <summary>
    /// 获取待审核推文数量
    /// </summary>
    /// <returns>待审核推文数量</returns>
    Task<int> GetPendingAuditCountAsync();
    /// <summary>
    /// 获取时间线推文数量（与 GetTimelineAsync 同条件，R-07 分页计数对齐）
    /// </summary>
    /// <param name="authorGuids">作者ID列表</param>
    /// <param name="viewerId">查看者ID（未认证为 Guid.Empty）</param>
    /// <param name="followingIds">查看者的关注集合（Followers 可见性判定）</param>
    /// <returns>时间线推文数量</returns>
    Task<int> GetTimelineCountAsync(IEnumerable<Guid> authorGuids, Guid viewerId, IEnumerable<Guid> followingIds);

    /// <summary>
    /// 获取热门推文数量（仅统计已审核通过的推文）
    /// </summary>
    /// <returns>热门推文数量</returns>
    Task<int> GetTrendingCountAsync();

    /// <summary>
    /// 获取圈子帖子列表（仅 Approved，按时间倒序）
    /// </summary>
    Task<IEnumerable<Tweet>> GetByCircleAsync(Guid circleGuid, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取圈子帖子数量
    /// </summary>
    Task<int> GetCirclePostCountAsync(Guid circleGuid);

    /// <summary>
    /// 获取话题帖子列表（全局帖 + 圈子帖，仅 Approved，按时间倒序）
    /// </summary>
    Task<IEnumerable<Tweet>> GetByTopicAsync(Guid topicGuid, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取话题帖子数量
    /// </summary>
    Task<int> GetTopicPostCountAsync(Guid topicGuid);

    /// <summary>
    /// 获取关注 Feed（我 + 关注者发布的全局帖，仅 Approved，按时间倒序；R-02：SQL 层排除 Private）
    /// </summary>
    Task<IEnumerable<Tweet>> GetCommunityFeedAsync(IEnumerable<Guid> authorGuids, Guid viewerId, IEnumerable<Guid> followingIds, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取关注 Feed 数量（与 GetCommunityFeedAsync 同条件，R-07 分页计数对齐）
    /// </summary>
    Task<int> GetCommunityFeedCountAsync(IEnumerable<Guid> authorGuids, Guid viewerId, IEnumerable<Guid> followingIds);

    /// <summary>全量推文总数（运营统计用，不分状态）</summary>
    Task<int> GetCountAllAsync();
}
