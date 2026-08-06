namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 粉丝列表查询（分页）。
/// </summary>
public record GetFollowersQuery(Guid UserGuid, int Page, int PageSize) : IRequest<PagedResult<UserFollow>>;

