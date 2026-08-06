namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// MarkQuote 值对象响应
/// </summary>
public class MarkQuoteResponse
{
    public long LoveCount { get; set; }
    public long ReplyCount { get; set; }
    public long CommentCount { get; set; }
    public long ShareCount { get; set; }
    public long ViewCount { get; set; }
    public long TotalInteractions { get; set; }
}
