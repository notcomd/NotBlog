namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 我收到的直邀列表查询。
/// </summary>
public record GetMyInvitationsQuery(Guid UserId, int Page, int PageSize)
    : IRequest<PagedResult<CircleInvitation>>;

