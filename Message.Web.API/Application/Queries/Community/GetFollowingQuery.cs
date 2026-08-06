namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 关注列表查询（分页）。
/// </summary>
public record GetFollowingQuery(Guid UserGuid, int Page, int PageSize) : IRequest<PagedResult<UserFollow>>;

