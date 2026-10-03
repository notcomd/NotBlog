
namespace Message.Infrastructure.Repository;

/// <summary>动态通知仓储实现，负责 TweetNotifications 的查询与持久化。</summary>
public class TweetNotificationRepository : ITweetNotificationRepository
{
    private readonly MessageDbContext _context;
    private readonly DbSet<TweetNotification> _dbSet;

    /// <summary>初始化 <see cref="TweetNotificationRepository"/> 实例。</summary>
    /// <param name="context">数据库上下文。</param>
    public TweetNotificationRepository(MessageDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TweetNotification>();
    }

    /// <summary>按通知标识获取通知，不存在时返回 null。</summary>
    public async Task<TweetNotification?> GetByIdAsync(Guid notifyGuid)
    {
        return await _dbSet.FirstOrDefaultAsync(n => n.Id == notifyGuid);
    }

    /// <summary>分页获取指定用户的通知（可选择仅未读），按创建时间倒序。</summary>
    public async Task<IEnumerable<TweetNotification>> GetByUserAsync(Guid userGuid, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        var query = _dbSet.Where(n => n.UserGuid == userGuid);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        query = query.OrderByDescending(n => n.CreateTime);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>新增通知并返回已跟踪的实体。</summary>
    public async Task<TweetNotification> AddAsync(TweetNotification notification)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        var entry = await _dbSet.AddAsync(notification);
        return entry.Entity;
    }

    /// <summary>更新通知。</summary>
    public async Task UpdateAsync(TweetNotification notification)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        _dbSet.Update(notification);
    }

    /// <summary>统计指定用户的未读通知数量。</summary>
    public async Task<int> GetUnreadCountAsync(Guid userGuid)
    {
        return await _dbSet.CountAsync(n => n.UserGuid == userGuid && !n.IsRead);
    }

    /// <summary>统计指定用户的通知数量（可选择仅统计未读）。</summary>
    public async Task<int> CountByUserAsync(Guid userGuid, bool unreadOnly = false)
    {
        var query = _dbSet.Where(n => n.UserGuid == userGuid);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);
        return await query.CountAsync();
    }

    /// <summary>将指定用户的全部未读通知标记为已读。</summary>
    public async Task MarkAllAsReadAsync(Guid userGuid)
    {
        var notifications = await _dbSet
            .Where(n => n.UserGuid == userGuid && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        _dbSet.UpdateRange(notifications);
    }
}
