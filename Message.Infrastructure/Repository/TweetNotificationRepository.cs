
namespace Message.Infrastructure.Repository;

public class TweetNotificationRepository : ITweetNotificationRepository
{
    private readonly MessageDbContext _context;
    private readonly DbSet<TweetNotification> _dbSet;

    public TweetNotificationRepository(MessageDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TweetNotification>();
    }

    public async Task<TweetNotification?> GetByIdAsync(Guid notifyGuid)
    {
        return await _dbSet.FirstOrDefaultAsync(n => n.Id == notifyGuid);
    }

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

    public async Task<TweetNotification> AddAsync(TweetNotification notification)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        var entry = await _dbSet.AddAsync(notification);
        return entry.Entity;
    }

    public async Task UpdateAsync(TweetNotification notification)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        _dbSet.Update(notification);
    }

    public async Task<int> GetUnreadCountAsync(Guid userGuid)
    {
        return await _dbSet.CountAsync(n => n.UserGuid == userGuid && !n.IsRead);
    }

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
