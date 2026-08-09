namespace Markdown.Web.API.Application.Queries;

/// <summary>
///     我的收藏标签库查询（标签复用建议：按使用次数/最近使用排序，支持关键字过滤）
/// </summary>
public record MarkFavoriteTagsQuery(
    Guid UserId,
    string? Keyword = null,
    int Limit = 50
) : IRequest<List<MarkFavoriteTagResponse>>;
