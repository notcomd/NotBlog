using Message.Domain.Entities.Announcement;

namespace Message.Infrastructure.Repository;

/// <summary>
/// 公报仓储实现（EF）
/// </summary>
public class AnnouncementRepository(MessageDbContext context) : IAnnouncementRepository
{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Announcement> _dbSet = context.Set<Announcement>();

    /// <summary>按公告标识获取公告，不存在时返回 null。</summary>
    public async Task<Announcement?> GetByIdAsync(Guid announcementGuid)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.AnnouncementGuid == announcementGuid);
    }

    /// <summary>分页获取未撤回的公告，按创建时间倒序。</summary>
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

    /// <summary>统计未撤回的公告数量。</summary>
    public async Task<int> GetActiveCountAsync()
    {
        return await _dbSet.CountAsync(a => !a.IsRecalled);
    }

    /// <summary>分页获取全部公告（含已撤回），按创建时间倒序。</summary>
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

    /// <summary>统计全部公告数量（含已撤回）。</summary>
    public async Task<int> GetCountAsync()
    {
        return await _dbSet.CountAsync();
    }

    /// <summary>新增公告并返回已跟踪的实体。</summary>
    public async Task<Announcement> AddAsync(Announcement announcement)
    {
        var entry = await _dbSet.AddAsync(announcement);
        return entry.Entity;
    }

    /// <summary>更新公告；仅对未跟踪实体执行更新。</summary>
    public async Task<Announcement> UpdateAsync(Announcement announcement)
    {
        if (context.Entry(announcement).State == EntityState.Detached)
            _dbSet.Update(announcement);
        return announcement;
    }
}