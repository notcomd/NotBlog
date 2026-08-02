using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.IRepository;

public interface ITweetRepository : IRepository<Tweet>
{
    /// <summary>
    /// 获取推文详情
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <returns>推文详情</returns>
    Task<Tweet?> GetByIdAsync(Guid tweetGuid);
    
    /// <summary>
    /// 获取推文列表
    /// </summary>
    /// <param name="authorGuid">作者ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>推文列表</returns>
    Task<IEnumerable<Tweet>> GetByAuthorAsync(Guid authorGuid, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取多个作者的时间线推文列表
    /// </summary>
    /// <param name="authorGuids">作者ID列表</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>指定作者的时间线推文列表</returns>
    Task<IEnumerable<Tweet>> GetTimelineAsync(IEnumerable<Guid> authorGuids, int page = 1, int pageSize = 20);
    
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
    /// 获取指定作者的推文数量
    /// </summary>
    /// <param name="authorGuid">作者ID</param>
    /// <returns>指定作者的推文数量</returns>
    Task<int> GetCountByAuthorAsync(Guid authorGuid);

    /// <summary>
    /// 获取待审核推文数量
    /// </summary>
    /// <returns>待审核推文数量</returns>
    Task<int> GetPendingAuditCountAsync();
    /// <summary>
    /// 获取时间线推文数量（仅统计已审核通过的推文）
    /// </summary>
    /// <param name="authorGuids">作者ID列表</param>
    /// <returns>时间线推文数量</returns>
    Task<int> GetTimelineCountAsync(IEnumerable<Guid> authorGuids);

    /// <summary>
    /// 获取热门推文数量（仅统计已审核通过的推文）
    /// </summary>
    /// <returns>热门推文数量</returns>
    Task<int> GetTrendingCountAsync();
}
