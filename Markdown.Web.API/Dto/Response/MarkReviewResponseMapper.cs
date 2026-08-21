namespace Markdown.Web.API.Dto.Response;

/// <summary>
///     MarkReview / ReviewQuote 实体 → 响应 DTO 映射
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
        ReviewImages = review.ReviewImages is { Count: > 0 }
            ? [.. review.ReviewImages.Select(i => i.ImageUrl.ToString())]
            : null,
        ReviewTime = review.MarkReviewTime,
        IsDeleted = review.IsDelete,
        ChildReviewCount = review.MarkReviews?.Count ?? 0,
        Quote = review.ReviewQuote is not null ? MapToReviewQuoteResponse(review.ReviewQuote) : null
    };

    public static ReviewQuoteResponse MapToReviewQuoteResponse(ReviewQuote quote) => new()
    {
        LoveCount = quote.LoveSome,
        ViewCount = quote.ViewSome,
        ReplyCount = quote.ReplySome,
        DislikeCount = quote.DislikeSome,
        TotalInteractions = quote.GetTotalInteractions()
    };
}
