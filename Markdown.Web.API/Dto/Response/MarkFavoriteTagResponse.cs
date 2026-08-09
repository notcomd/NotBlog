namespace Markdown.Web.API.Dto.Response;

/// <summary>
///     收藏标签库响应（标签复用建议项）
/// </summary>
public class MarkFavoriteTagResponse
{
    public string Tag { get; set; } = null!;

    /// <summary>
    ///     使用次数（打标签命中次数，用于常用标签排序）
    /// </summary>
    public int UseCount { get; set; }

    public DateTimeOffset LastUsedAt { get; set; }
}
