
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

    /// <summary>通知总数（R-04：分页 TotalCount，可按未读过滤）</summary>
    Task<int> CountByUserAsync(Guid userGuid, bool unreadOnly = false);

    Task MarkAllAsReadAsync(Guid userGuid);
}
