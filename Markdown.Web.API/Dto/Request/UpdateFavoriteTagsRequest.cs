namespace Markdown.Web.API.Dto.Request;

/// <summary>
///     覆盖式更新收藏标签请求（空列表/ null 表示清空标签）
/// </summary>
public class UpdateFavoriteTagsRequest
{
    /// <summary>
    ///     新的收藏标签列表（最多 20 个，单标签不超过 50 字符）
    /// </summary>
    [MaxLength(20, ErrorMessage = "收藏标签最多 20 个")]
    public List<string>? Tags { get; set; }
}
