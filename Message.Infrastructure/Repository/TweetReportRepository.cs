using Message.Infrastructure.EntityFramework;

namespace Message.Infrastructure.Repository;

public class TweetReportRepository : ITweetReportRepository
{
    private readonly MessageDbContext _context;
    private readonly DbSet<TweetReport> _dbSet;

    public IUnitOfWork UnitOfWork => _context;

    public TweetReportRepository(MessageDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TweetReport>();
    }

    public async Task<TweetReport?> GetByIdAsync(Guid reportGuid)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.ReportGuid == reportGuid);
    }

    public async Task<IEnumerable<TweetReport>> GetByReporterAsync(Guid reporterGuid, int page = 1, int pageSize = 20)
    {
        var query = _dbSet
            .Where(r => r.ReporterGuid == reporterGuid)
            .OrderByDescending(r => r.CreateTime);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<TweetReport>> GetByStatusAsync(ReportStatus status, int page = 1, int pageSize = 20)
    {
        var query = _dbSet
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreateTime);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<TweetReport>> GetByTargetAsync(ReportTargetType targetType, Guid targetGuid)
    {
        return await _dbSet
            .Where(r => r.TargetType == targetType && r.TargetGuid == targetGuid)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public async Task<TweetReport> AddAsync(TweetReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        var entry = await _dbSet.AddAsync(report);
        return entry.Entity;
    }

    public async Task<TweetReport> UpdateAsync(TweetReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        var entry = _dbSet.Update(report);
        return entry.Entity;
    }

    public async Task<bool> ExistsAsync(Guid reportGuid)
    {
        return await _dbSet.AnyAsync(r => r.ReportGuid == reportGuid);
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _dbSet.CountAsync(r => r.Status == ReportStatus.Pending);
    }

    public async Task<int> CountByReporterAsync(Guid reporterGuid)
    {
        return await _dbSet.CountAsync(r => r.ReporterGuid == reporterGuid);
    }
}
