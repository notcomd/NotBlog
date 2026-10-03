namespace Message.Web.API.Application.Queries.Reports;

/// <summary>
/// 获取当前用户举报列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetMyReportsQueryHandler(
    ITweetReportRepository reportRepository) : IRequestHandler<GetMyReportsQuery, PagedResult<TweetReport>>
{
    public async Task<PagedResult<TweetReport>> Handler(GetMyReportsQuery query, CancellationToken cancellationToken)
    {
        var items = await reportRepository.GetByReporterAsync(query.UserId, query.Page, query.PageSize);
        var totalCount = await reportRepository.CountByReporterAsync(query.UserId);

        return new PagedResult<TweetReport>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}