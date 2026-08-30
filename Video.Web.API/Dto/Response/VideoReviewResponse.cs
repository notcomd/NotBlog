namespace Video.Web.API.Dto.Response;

/// <summary>
/// 视频评论响应（互动计数为评论专属 ReviewQuote 的 Like/Dislike）。
/// </summary>
public record VideoReviewResponse(
    Guid VideoReviewGuid,
    Guid VideoGuid,
    Guid UserGuid,
    Guid? RootReview,
    string? VideoReviewBody,
    DateTimeOffset CreateAt,
    DateTimeOffset UpdateAt,
    long Like,
    long Dislike);