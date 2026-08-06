namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 圈子帖子流查询（仅成员可见，分页）。
/// </summary>
public record GetCirclePostsQuery(Guid CircleGuid, Guid CurrentUserId, int Page, int PageSize)
    : IRequest<PagedResult<Tweet>>;

