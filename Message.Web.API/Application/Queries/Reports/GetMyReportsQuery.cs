namespace Message.Web.API.Application.Queries.Reports;

/// <summary>
/// 获取当前用户举报列表查询（分页）。
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetMyReportsQuery(Guid UserId, int Page, int PageSize) : IRequest<IEnumerable<TweetReport>>;

/// <summary>
/// 获取当前用户举报列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetMyReportsQueryHandler(
    ITweetReportRepository reportRepository) : IRequestHandler<GetMyReportsQuery, IEnumerable<TweetReport>>
{
    public async Task<IEnumerable<TweetReport>> Handler(GetMyReportsQuery query, CancellationToken cancellationToken)
        => await reportRepository.GetByReporterAsync(query.UserId, query.Page, query.PageSize);
}
