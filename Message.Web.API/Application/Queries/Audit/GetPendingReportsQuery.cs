namespace Message.Web.API.Application.Queries.Audit;

/// <summary>
/// 获取待处理举报列表查询。
/// </summary>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetPendingReportsQuery(int Page, int PageSize) : IRequest<PagedResult<TweetReport>>;