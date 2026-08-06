

namespace Message.Domain.IRepository;
/// <summary>
/// 微博互动仓储接口
/// </summary>
public interface ITweetInteractionRepository 
{
    Task<TweetInteraction?> GetAsync(Guid tweetGuid, Guid userGuid, InteractionType type);
    Task<IEnumerable<TweetInteraction>> GetByTweetAsync(Guid tweetGuid, InteractionType? type = null, int page = 1, int pageSize = 50);
    Task<IEnumerable<TweetInteraction>> GetByUserAsync(Guid userGuid, InteractionType? type = null, int page = 1, int pageSize = 20);
    Task<bool> ExistsAsync(Guid tweetGuid, Guid userGuid, InteractionType type);
    Task<TweetInteraction> AddAsync(TweetInteraction interaction);
    Task DeleteAsync(Guid tweetGuid, Guid userGuid, InteractionType type);
    Task<int> GetCountByTweetAsync(Guid tweetGuid, InteractionType type);
}
