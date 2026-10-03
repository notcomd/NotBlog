

namespace Message.Domain.IRepository;
/// <summary>
/// 微博互动仓储接口
/// </summary>
public interface ITweetInteractionRepository 
{
    /// <summary>查询指定用户对指定推文的某类互动（不存在返回 null）</summary>
    Task<TweetInteraction?> GetAsync(Guid tweetGuid, Guid userGuid, InteractionType type);
    /// <summary>分页获取某推文的互动列表（可按类型过滤）</summary>
    Task<IEnumerable<TweetInteraction>> GetByTweetAsync(Guid tweetGuid, InteractionType? type = null, int page = 1, int pageSize = 50);
    /// <summary>分页获取某用户的互动列表（可按类型过滤）</summary>
    Task<IEnumerable<TweetInteraction>> GetByUserAsync(Guid userGuid, InteractionType? type = null, int page = 1, int pageSize = 20);
    /// <summary>判断互动是否存在</summary>
    Task<bool> ExistsAsync(Guid tweetGuid, Guid userGuid, InteractionType type);
    /// <summary>新增互动</summary>
    Task<TweetInteraction> AddAsync(TweetInteraction interaction);
    /// <summary>删除互动</summary>
    Task DeleteAsync(Guid tweetGuid, Guid userGuid, InteractionType type);
    /// <summary>统计某推文某类互动的数量</summary>
    Task<int> GetCountByTweetAsync(Guid tweetGuid, InteractionType type);
    /// <summary>统计某用户某类互动的数量</summary>
    Task<int> GetCountByUserAsync(Guid userGuid, InteractionType type);
}
