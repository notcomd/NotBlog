namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// MarkReview 评论响应
/// </summary>
public class MarkReviewResponse
{
    public Guid MarkReviewGuid { get; set; }
    public Guid MarkDownGuid { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = null!;
    public string Auth { get; set; } = null!;

    /// <summary>
    ///     评论配图 URL 列表（最多 9 张，来自 FileDev 文件 URI）
    /// </summary>
    public List<string>? ReviewImages { get; set; }
    public DateTimeOffset ReviewTime { get; set; }
    public bool IsDeleted { get; set; }
    public int ChildReviewCount { get; set; }
    public ReviewQuoteResponse? Quote { get; set; }
}
