using Message.Infrastructure.EntityFramework;

namespace Message.Infrastructure.Repository;

/// <summary>动态举报仓储实现，负责 TweetReports 的查询与持久化。</summary>
public class TweetReportRepository : ITweetReportRepository
{
    private readonly MessageDbContext _context;
    private readonly DbSet<TweetReport> _dbSet;

    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => _context;

    /// <summary>初始化 <see cref="TweetReportRepository"/> 实例。</summary>
    /// <param name="context">数据库上下文。</param>
    public TweetReportRepository(MessageDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TweetReport>();
    }

    /// <summary>按举报标识获取举报记录，不存在时返回 null。</summary>
    public async Task<TweetReport?> GetByIdAsync(Guid reportGuid)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.ReportGuid == reportGuid);
    }

    /// <summary>分页获取指定举报人提交的举报，按创建时间倒序。</summary>
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

    /// <summary>按举报状态分页获取举报，按创建时间倒序。</summary>
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

    /// <summary>获取针对指定目标（类型 + 标识）的举报，按创建时间倒序。</summary>
    public async Task<IEnumerable<TweetReport>> GetByTargetAsync(ReportTargetType targetType, Guid targetGuid)
    {
        return await _dbSet
            .Where(r => r.TargetType == targetType && r.TargetGuid == targetGuid)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    /// <summary>新增举报记录并返回已跟踪的实体。</summary>
    public async Task<TweetReport> AddAsync(TweetReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        var entry = await _dbSet.AddAsync(report);
        return entry.Entity;
    }

    /// <summary>更新举报记录并返回已跟踪的实体。</summary>
    public async Task<TweetReport> UpdateAsync(TweetReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        var entry = _dbSet.Update(report);
        return entry.Entity;
    }

    /// <summary>判断指定举报记录是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid reportGuid)
    {
        return await _dbSet.AnyAsync(r => r.ReportGuid == reportGuid);
    }

    /// <summary>统计待处理的举报数量。</summary>
    public async Task<int> GetPendingCountAsync()
    {
        return await _dbSet.CountAsync(r => r.Status == ReportStatus.Pending);
    }

    /// <summary>统计指定举报人提交的举报数量。</summary>
    public async Task<int> CountByReporterAsync(Guid reporterGuid)
    {
        return await _dbSet.CountAsync(r => r.ReporterGuid == reporterGuid);
    }
}
