
namespace Message.Domain.IRepository;
/// <summary>
/// 微博通知仓储接口
/// </summary>
public interface ITweetNotificationRepository
{
    /// <summary>按通知 ID 查询（不存在返回 null）</summary>
    Task<TweetNotification?> GetByIdAsync(Guid notifyGuid);
    /// <summary>分页获取用户通知（可按未读过滤）</summary>
    Task<IEnumerable<TweetNotification>> GetByUserAsync(Guid userGuid, bool unreadOnly = false, int page = 1, int pageSize = 20);
    /// <summary>新增通知</summary>
    Task<TweetNotification> AddAsync(TweetNotification notification);
    /// <summary>更新通知</summary>
    Task UpdateAsync(TweetNotification notification);
    /// <summary>获取用户未读通知数量</summary>
    Task<int> GetUnreadCountAsync(Guid userGuid);

    /// <summary>通知总数（R-04：分页 TotalCount，可按未读过滤）</summary>
    Task<int> CountByUserAsync(Guid userGuid, bool unreadOnly = false);

    /// <summary>将用户全部通知标记为已读</summary>
    Task MarkAllAsReadAsync(Guid userGuid);
}
