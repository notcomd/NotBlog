using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Commons.SeedWork;
using NotMediator;

namespace Message.Domain.IRepository;
/// <summary>
/// 微博举报仓储接口
/// </summary>
public interface ITweetReportRepository : IRepository<TweetReport, IUnitOfWork>
{
    Task<TweetReport?> GetByIdAsync(Guid reportGuid);
    Task<IEnumerable<TweetReport>> GetByReporterAsync(Guid reporterGuid, int page = 1, int pageSize = 20);
    Task<IEnumerable<TweetReport>> GetByStatusAsync(ReportStatus status, int page = 1, int pageSize = 20);
    Task<IEnumerable<TweetReport>> GetByTargetAsync(ReportTargetType targetType, Guid targetGuid);
    Task<TweetReport> AddAsync(TweetReport report);
    Task<TweetReport> UpdateAsync(TweetReport report);
    Task<bool> ExistsAsync(Guid reportGuid);
    Task<int> GetPendingCountAsync();
    /// <summary>
    /// 获取指定举报人提交的举报数量
    /// </summary>
    /// <param name="reporterGuid">举报人ID</param>
    /// <returns>举报数量</returns>
    Task<int> CountByReporterAsync(Guid reporterGuid);
}
