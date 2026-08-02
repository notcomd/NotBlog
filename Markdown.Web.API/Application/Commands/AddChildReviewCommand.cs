namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 添加子评论命令（F-10.4，父评论 Guid + 内容）
/// </summary>
public record AddChildReviewCommand(
    Guid MarkDownGuid,
    Guid ParentReviewGuid,
    Guid UserId,
    string Content,
    List<ReviewImage>? ReviewImages = null,
    MarkReviewAuth ReviewAuth = MarkReviewAuth.ReviewAuthPublic,
    Guid IdempotencyKey = default
) : IRequest<Guid>;