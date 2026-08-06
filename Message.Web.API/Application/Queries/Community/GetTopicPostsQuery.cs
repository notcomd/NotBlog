namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 话题帖子流查询（全局帖 + 当前用户可见的圈子帖，分页）。
/// </summary>
public record GetTopicPostsQuery(Guid TopicGuid, Guid CurrentUserId, int Page, int PageSize)
    : IRequest<PagedResult<Tweet>>;

