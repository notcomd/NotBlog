namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>获取我的收藏列表查询（按收藏时间倒序，含可见性过滤）。</summary>
public record GetMyFavoritesQuery(Guid UserId, int Page = 1, int PageSize = 12)
    : IRequest<PagedResult<CommunityPostDto>>;