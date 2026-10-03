namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>我的草稿列表查询（R-08，仅作者本人可见，按最近编辑倒序）。</summary>
public record GetMyDraftsQuery(Guid UserId, int Page, int PageSize) : IRequest<PagedResult<Tweet>>;
