namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取趋势推文列表查询。
/// </summary>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetTrendingQuery(int Page, int PageSize) : IRequest<PagedResult<Tweet>>;

