namespace Markdown.Web.API.Dto.Request;

/// <summary>
/// 更新 MarkReview 评论请求
/// </summary>
public class UpdateMarkReviewRequest
{
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required(ErrorMessage = "评论内容不能为空")]
    [StringLength(2000, ErrorMessage = "评论内容长度不能超过 2000 个字符")]
    public string Content { get; set; } = null!;
}
