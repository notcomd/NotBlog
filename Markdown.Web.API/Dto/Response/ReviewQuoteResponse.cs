namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// ReviewQuote 评论交互统计响应（点赞/查看/回复数/踩）
/// </summary>
public class ReviewQuoteResponse
{
    public long LoveCount { get; set; }
    public long ViewCount { get; set; }
    public long ReplyCount { get; set; }
    public long DislikeCount { get; set; }
    public long TotalInteractions { get; set; }
}
