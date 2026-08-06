namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取用户时间线推文列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetTimelineQuery(Guid UserId, int Page, int PageSize) : IRequest<PagedResult<Tweet>>;

