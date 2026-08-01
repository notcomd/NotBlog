namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 更新 MarkReview 评论命令
/// </summary>
public record UpdateMarkReviewCommand(
    Guid ReviewGuid,
    Guid UserId,
    string Content,
    Guid IdempotencyKey
) : IRequest<bool>;
