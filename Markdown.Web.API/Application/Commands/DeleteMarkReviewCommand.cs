namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 删除 MarkReview 评论命令（软删除）
/// </summary>
public record DeleteMarkReviewCommand(
    Guid ReviewGuid,
    Guid UserId,
    Guid IdempotencyKey
) : IRequest<bool>;
