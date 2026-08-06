namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 圈子的邀请列表查询（圈主/管理员）。
/// </summary>
public record GetCircleInvitationsQuery(Guid CircleGuid, Guid OperatorGuid, int Page, int PageSize)
    : IRequest<PagedResult<CircleInvitation>>;

