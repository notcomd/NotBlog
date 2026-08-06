namespace Markdown.Web.API.Dto.Request;

/// <summary>
///     添加收藏请求
/// </summary>
public class AddFavoriteRequest
{
    /// <summary>
    ///     被收藏文章 GUID
    /// </summary>
    [Required(ErrorMessage = "文章标识不能为空")]
    public Guid MarkDownGuid { get; set; }

    /// <summary>
    ///     收藏分类标签（可选，用于 tag 分类管理，最多 20 个）
    /// </summary>
    [MaxLength(20, ErrorMessage = "收藏标签最多 20 个")]
    public List<string>? Tags { get; set; }
}
