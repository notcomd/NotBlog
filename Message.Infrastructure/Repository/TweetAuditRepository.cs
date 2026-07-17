using Message.Domain.Entities.Tweet;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Message.Infrastructure.Repository;

public class TweetAuditRepository : ITweetAuditRepository
{
    private readonly MessageDbContext _context;
    private readonly DbSet<TweetAuditLog> _dbSet;

    public TweetAuditRepository(MessageDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TweetAuditLog>();
    }

    public async Task<TweetAuditLog?> GetByIdAsync(Guid auditGuid)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.AuditGuid == auditGuid);
    }

    public async Task<IEnumerable<TweetAuditLog>> GetByTweetAsync(Guid tweetGuid)
    {
        return await _dbSet
            .Where(a => a.TweetGuid == tweetGuid)
            .OrderByDescending(a => a.AuditTime)
            .ToListAsync();
    }

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

    public async Task<TweetAuditLog> AddAsync(TweetAuditLog auditLog)
    {
        if (auditLog == null) throw new ArgumentNullException(nameof(auditLog));
        var entry = await _dbSet.AddAsync(auditLog);
        return entry.Entity;
    }

    public async Task<int> GetCountByTweetAsync(Guid tweetGuid)
    {
        return await _dbSet.CountAsync(a => a.TweetGuid == tweetGuid);
    }
}
