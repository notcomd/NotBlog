namespace Markdown.Web.API.Dto.Response;

/// <summary>
///     MarkReview / MarkQuote 实体 → 响应 DTO 映射
/// </summary>
public static class MarkReviewResponseMapper
{
    public static MarkReviewResponse MapToMarkReviewResponse(MarkReview review) => new()
    {
        MarkReviewGuid = review.MarkReviewGuid,
        MarkDownGuid = review.MarkDownGuid,
        UserId = review.UserId,
        Content = review.MarkReviewContent ?? string.Empty,
        Auth = review.MarkReviewAuth.ToString(),
        ReviewTime = review.MarkReviewTime,
        IsDeleted = review.IsDelete,
        ChildReviewCount = review.MarkReviews?.Count ?? 0,
        Quote = review.MarkQuote is not null ? MapToMarkQuoteResponse(review.MarkQuote) : null
    };

    public static MarkQuoteResponse MapToMarkQuoteResponse(MarkQuote quote) => new()
    {
        LoveCount = quote.LoveSome,
        ReplyCount = quote.ReviewSome,
        CommentCount = quote.CommentSome,
        ShareCount = quote.ShareSome,
        ViewCount = quote.ViewSome,
        TotalInteractions = quote.GetTotalInteractions()
    };
}
