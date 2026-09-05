using Message.Domain.Entities.Announcement;

namespace Message.Infrastructure.Repository;

/// <summary>
/// 公报仓储实现（EF）
/// </summary>
public class AnnouncementRepository(MessageDbContext context) : IAnnouncementRepository
{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Announcement> _dbSet = context.Set<Announcement>();

    public async Task<Announcement?> GetByIdAsync(Guid announcementGuid)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.AnnouncementGuid == announcementGuid);
    }

    public async Task<IEnumerable<Announcement>> GetActiveAsync(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await _dbSet
            .AsNoTracking()
            .Where(a => !a.IsRecalled)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _dbSet.CountAsync(a => !a.IsRecalled);
    }

    public async Task<IEnumerable<Announcement>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<Announcement> AddAsync(Announcement announcement)
    {
        var entry = await _dbSet.AddAsync(announcement);
        return entry.Entity;
    }

    public async Task<Announcement> UpdateAsync(Announcement announcement)
    {
        if (context.Entry(announcement).State == EntityState.Detached)
            _dbSet.Update(announcement);
        return announcement;
    }
}