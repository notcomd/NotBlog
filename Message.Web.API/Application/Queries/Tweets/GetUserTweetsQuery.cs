namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取用户推文列表查询。
/// </summary>
/// <param name="UserGuid">用户 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetUserTweetsQuery(Guid UserGuid, int Page, int PageSize) : IRequest<PagedResult<Tweet>>;

