
namespace Message.Domain.IRepository;
/// <summary>
/// 微博审核仓储接口
/// </summary>
public interface ITweetAuditRepository 
{
    Task<TweetAuditLog?> GetByIdAsync(Guid auditGuid);
    Task<IEnumerable<TweetAuditLog>> GetByTweetAsync(Guid tweetGuid);
    Task<IEnumerable<TweetAuditLog>> GetByAuditorAsync(Guid auditorGuid, int page = 1, int pageSize = 20);
    Task<TweetAuditLog> AddAsync(TweetAuditLog auditLog);
    Task<int> GetCountByTweetAsync(Guid tweetGuid);

    /// <summary>管理端操作日志分页（全部操作者，按时间倒序）</summary>
    Task<IEnumerable<TweetAuditLog>> GetPagedAsync(int page = 1, int pageSize = 20);

    /// <summary>管理端操作日志总数</summary>
    Task<int> GetCountAsync();
}
