namespace Message.Web.API.Application.Queries.Audit;

/// <summary>
/// 获取待处理举报列表查询。
/// </summary>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetPendingReportsQuery(int Page, int PageSize) : IRequest<PagedResult<TweetReport>>;

/// <summary>
/// 获取待处理举报列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetPendingReportsQueryHandler(
    ITweetReportRepository reportRepository) : IRequestHandler<GetPendingReportsQuery, PagedResult<TweetReport>>
{
    public async Task<PagedResult<TweetReport>> Handler(GetPendingReportsQuery query, CancellationToken cancellationToken)
    {
        var items = await reportRepository.GetByStatusAsync(ReportStatus.Pending, query.Page, query.PageSize);
        var totalCount = await reportRepository.GetPendingCountAsync();

        return new PagedResult<TweetReport>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}