namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 话题列表查询（按帖子数降序，分页）。
/// </summary>
public record GetTopicsQuery(int Page, int PageSize) : IRequest<PagedResult<Topic>>;

