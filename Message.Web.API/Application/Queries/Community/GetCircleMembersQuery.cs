namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 圈子成员列表查询（仅成员可见）。
/// </summary>
public record GetCircleMembersQuery(Guid CircleGuid, Guid CurrentUserId, int Page, int PageSize)
    : IRequest<PagedResult<CircleMember>>;

