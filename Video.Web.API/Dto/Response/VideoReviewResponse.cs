namespace Video.Web.API.Dto.Response;

public record VideoReviewResponse(
    Guid VideoReviewGuid,
    Guid VideoGuid,
    Guid UserGuid,
    Guid? RootReview,
    string? VideoReviewBody,
    DateTimeOffset CreateAt,
    DateTimeOffset UpdateAt,
    long Upvote,
    long Stars,
    long Watch);
