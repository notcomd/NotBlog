using Message.Domain.Entities.Tweet;
using Message.Domain.SeedWork;

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
}
