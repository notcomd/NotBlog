using Message.Domain.Entities.Tweet;

namespace Message.Domain.IRepository;
/// <summary>
/// 微博通知仓储接口
/// </summary>
public interface ITweetNotificationRepository
{
    Task<TweetNotification?> GetByIdAsync(Guid notifyGuid);
    Task<IEnumerable<TweetNotification>> GetByUserAsync(Guid userGuid, bool unreadOnly = false, int page = 1, int pageSize = 20);
    Task<TweetNotification> AddAsync(TweetNotification notification);
    Task UpdateAsync(TweetNotification notification);
    Task<int> GetUnreadCountAsync(Guid userGuid);
    Task MarkAllAsReadAsync(Guid userGuid);
}
