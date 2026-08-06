namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 创建 MarkReview 评论命令
/// </summary>
public record CreateMarkReviewCommand(
    Guid MarkDownGuid,
    Guid UserId,
    string Content,
    List<ReviewImage>? ReviewImages = null,
    MarkReviewAuth ReviewAuth = MarkReviewAuth.ReviewAuthPublic,
    Guid IdempotencyKey = default
) : IRequest<Guid>, ICommandRequest;
