namespace Markdown.Web.API.Application.Dto;

/// <summary>
/// 创建 Markdown 文章请求
/// </summary>
public class CreateMarkdownRequest
{
    /// <summary>
    /// 文章名称
    /// </summary>
    [Required(ErrorMessage = "文章名称不能为空")]
    [StringLength(200, ErrorMessage = "文章名称长度不能超过 200 个字符")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// 文章内容（Markdown 格式）
    /// </summary>
    [Required(ErrorMessage = "文章内容不能为空")]
    public string Content { get; set; } = null!;

    /// <summary>
    /// 标签列表（可选）
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 文章权限类型（可选，默认为 PublicMark）
    /// </summary>
    public string? Auth { get; set; }
}
