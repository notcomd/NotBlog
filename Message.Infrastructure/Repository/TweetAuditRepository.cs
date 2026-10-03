
namespace Message.Infrastructure.Repository;

/// <summary>动态审核日志仓储实现，负责 TweetAuditLogs 的查询与持久化。</summary>
public class TweetAuditRepository : ITweetAuditRepository
{
    private readonly MessageDbContext _context;
    private readonly DbSet<TweetAuditLog> _dbSet;

    /// <summary>初始化 <see cref="TweetAuditRepository"/> 实例。</summary>
    /// <param name="context">数据库上下文。</param>
    public TweetAuditRepository(MessageDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TweetAuditLog>();
    }

    /// <summary>按审核日志标识获取日志，不存在时返回 null。</summary>
    public async Task<TweetAuditLog?> GetByIdAsync(Guid auditGuid)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.AuditGuid == auditGuid);
    }

    /// <summary>获取指定动态的全部审核日志，按审核时间倒序。</summary>
    public async Task<IEnumerable<TweetAuditLog>> GetByTweetAsync(Guid tweetGuid)
    {
        return await _dbSet
            .Where(a => a.TweetGuid == tweetGuid)
            .OrderByDescending(a => a.AuditTime)
            .ToListAsync();
    }

    /// <summary>分页获取指定审核人的审核日志，按审核时间倒序。</summary>
    public async Task<IEnumerable<TweetAuditLog>> GetByAuditorAsync(Guid auditorGuid, int page = 1, int pageSize = 20)
    {
        var query = _dbSet
            .Where(a => a.AuditorGuid == auditorGuid)
            .OrderByDescending(a => a.AuditTime);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>新增审核日志并返回已跟踪的实体。</summary>
    public async Task<TweetAuditLog> AddAsync(TweetAuditLog auditLog)
    {
        if (auditLog == null) throw new ArgumentNullException(nameof(auditLog));
        var entry = await _dbSet.AddAsync(auditLog);
        return entry.Entity;
    }

    /// <summary>统计指定动态的审核日志数量。</summary>
    public async Task<int> GetCountByTweetAsync(Guid tweetGuid)
    {
        return await _dbSet.CountAsync(a => a.TweetGuid == tweetGuid);
    }

    /// <summary>管理端操作日志分页（全部操作者，按时间倒序）</summary>
    public async Task<IEnumerable<TweetAuditLog>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(a => a.AuditTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>管理端操作日志总数</summary>
    public async Task<int> GetCountAsync()
    {
        return await _dbSet.CountAsync();
    }
}
