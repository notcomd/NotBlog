namespace Markdown.Web.API.Application.Dto;

/// <summary>
/// 创建 MarkReview 评论请求
/// </summary>
public class CreateMarkReviewRequest
{
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required(ErrorMessage = "评论内容不能为空")]
    [StringLength(2000, ErrorMessage = "评论内容长度不能超过 2000 个字符")]
    public string Content { get; set; } = null!;

    /// <summary>
    /// 评论配图列表（可选）
    /// </summary>
    public List<ReviewImage>? ReviewImages { get; set; }

    /// <summary>
    /// 评论权限类型（可选，默认为 ReviewAuthPublic）
    /// </summary>
    public string? Auth { get; set; }
}
