
namespace Message.Domain.IRepository;
/// <summary>
/// 微博举报仓储接口
/// </summary>
public interface ITweetReportRepository : IRepository<TweetReport, IUnitOfWork>
{
    /// <summary>按举报 ID 查询（不存在返回 null）</summary>
    Task<TweetReport?> GetByIdAsync(Guid reportGuid);
    /// <summary>分页获取某举报人提交的举报</summary>
    Task<IEnumerable<TweetReport>> GetByReporterAsync(Guid reporterGuid, int page = 1, int pageSize = 20);
    /// <summary>按状态分页获取举报</summary>
    Task<IEnumerable<TweetReport>> GetByStatusAsync(ReportStatus status, int page = 1, int pageSize = 20);
    /// <summary>按举报目标查询相关举报</summary>
    Task<IEnumerable<TweetReport>> GetByTargetAsync(ReportTargetType targetType, Guid targetGuid);
    /// <summary>新增举报</summary>
    Task<TweetReport> AddAsync(TweetReport report);
    /// <summary>更新举报</summary>
    Task<TweetReport> UpdateAsync(TweetReport report);
    /// <summary>判断举报是否存在</summary>
    Task<bool> ExistsAsync(Guid reportGuid);
    /// <summary>获取待处理举报数量</summary>
    Task<int> GetPendingCountAsync();
    /// <summary>
    /// 获取指定举报人提交的举报数量
    /// </summary>
    /// <param name="reporterGuid">举报人ID</param>
    /// <returns>举报数量</returns>
    Task<int> CountByReporterAsync(Guid reporterGuid);
}
