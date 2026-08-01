namespace Message.Web.API.Application.Queries.Audit;

/// <summary>
/// 获取待处理举报列表查询。
/// </summary>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetPendingReportsQuery(int Page, int PageSize) : IRequest<IEnumerable<TweetReport>>;

/// <summary>
/// 获取待处理举报列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetPendingReportsQueryHandler(
    ITweetReportRepository reportRepository) : IRequestHandler<GetPendingReportsQuery, IEnumerable<TweetReport>>
{
    public async Task<IEnumerable<TweetReport>> Handler(GetPendingReportsQuery query, CancellationToken cancellationToken)
        => await reportRepository.GetByStatusAsync(ReportStatus.Pending, query.Page, query.PageSize);
}
