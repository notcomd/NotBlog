namespace Markdown.Web.API.Application.Queries;

/// <summary>
///     我的收藏列表查询（分页 + tag 分类过滤，联表返回文章名称）
/// </summary>
public record MarkFavoriteListQuery(
    Guid UserId,
    string? Tag = null,
    int Skip = 0,
    int Take = 20
) : IRequest<List<MarkFavoriteResponse>>;
