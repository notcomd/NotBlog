namespace Markdown.Web.API.Application.Dto;

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
    public DateTimeOffset ReviewTime { get; set; }
    public bool IsDeleted { get; set; }
    public int ChildReviewCount { get; set; }
    public MarkQuoteResponse? Quote { get; set; }
}

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
