

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 评论点赞/取消点赞命令 — 带幂等性保护。
/// Field 支持: upvote（点赞）、down（踩）、ballot（投票）、share（分享）
/// </summary>
public record QuoteVideoReviewCommand(
    Guid RequestId,
    Guid VideoGuid,
    Guid UserGuid,
    Guid ReviewGuid,
    string Field,
    bool IsLike) : IRequest<QuoteVideoReviewResult>, IIdempotentRequest;

/// <summary>评论点赞操作结果。</summary>
public record QuoteVideoReviewResult(bool Success, long NewCount, string? ErrorMessage);
