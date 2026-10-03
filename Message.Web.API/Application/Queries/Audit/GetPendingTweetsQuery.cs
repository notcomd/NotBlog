namespace Message.Web.API.Application.Queries.Audit;

/// <summary>
/// 获取待审核推文列表查询。
/// </summary>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetPendingTweetsQuery(int Page, int PageSize) : IRequest<PagedResult<Tweet>>;