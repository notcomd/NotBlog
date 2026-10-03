
namespace Message.Domain.IRepository;
/// <summary>
/// 微博审核仓储接口
/// </summary>
public interface ITweetAuditRepository 
{
    /// <summary>按审核日志 ID 查询（不存在返回 null）</summary>
    Task<TweetAuditLog?> GetByIdAsync(Guid auditGuid);
    /// <summary>获取某推文的全部审核日志</summary>
    Task<IEnumerable<TweetAuditLog>> GetByTweetAsync(Guid tweetGuid);
    /// <summary>分页获取某审核人的操作日志</summary>
    Task<IEnumerable<TweetAuditLog>> GetByAuditorAsync(Guid auditorGuid, int page = 1, int pageSize = 20);
    /// <summary>新增审核日志</summary>
    Task<TweetAuditLog> AddAsync(TweetAuditLog auditLog);
    /// <summary>统计某推文的审核日志数量</summary>
    Task<int> GetCountByTweetAsync(Guid tweetGuid);

    /// <summary>管理端操作日志分页（全部操作者，按时间倒序）</summary>
    Task<IEnumerable<TweetAuditLog>> GetPagedAsync(int page = 1, int pageSize = 20);

    /// <summary>管理端操作日志总数</summary>
    Task<int> GetCountAsync();
}
