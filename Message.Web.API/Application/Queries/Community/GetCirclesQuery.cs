namespace Message.Web.API.Application.Queries.Community;

/// <summary>圈子发现列表查询（R-12：活跃圈子 + 名称模糊搜索，分页）。</summary>
public record GetCirclesQuery(string? Keyword, int Page, int PageSize) : IRequest<PagedResult<Circle>>;
