namespace Markdown.Web.API.Dto.Request;

/// <summary>
/// 审核驳回 Markdown 文章请求（body 可空，空 body 视为无驳回原因）
/// </summary>
public class RejectMarkdownRequest
{
    /// <summary>
    /// 驳回原因（可选，最长 500 个字符；留空表示不填写原因）
    /// </summary>
    [StringLength(500, ErrorMessage = "驳回原因长度不能超过 500 个字符")]
    public string? Reason { get; set; }
}
