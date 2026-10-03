namespace Message.Web.API.Application.Queries.Reports;

/// <summary>
/// 获取当前用户举报列表查询（分页）。
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetMyReportsQuery(Guid UserId, int Page, int PageSize) : IRequest<PagedResult<TweetReport>>;