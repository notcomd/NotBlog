using Markdown.Infrastructure.Idempotent;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 更新 MarkReview 评论命令处理器
/// </summary>
public class UpdateMarkReviewCommandHandler(
    IMarkdownRepository markdownRepository,
    IRequestManagement requestManagement,
    ILogger<UpdateMarkReviewCommandHandler> logger) : NotMediator.IRequestHandler<UpdateMarkReviewCommand, bool>
{
    public async Task<bool> Handler(UpdateMarkReviewCommand request, CancellationToken cancellationToken)
    {
        // 幂等性检查
        await requestManagement.CreateRequestForCommandAsync<UpdateMarkReviewCommand>(request.IdempotencyKey);

        // 获取评论验证所有权
        var review = await markdownRepository.GetReviewByIdAsync(request.ReviewGuid)
            ?? throw new KeyNotFoundException($"评论不存在：{request.ReviewGuid}");

        if (review.IsDelete)
            throw new InvalidOperationException("已删除的评论无法修改");

        if (review.UserId != request.UserId)
        {
            logger.LogWarning("用户 {UserGuid} 无权修改评论 {ReviewGuid}", request.UserId, request.ReviewGuid);
            throw new UnauthorizedAccessException("无权修改此评论");
        }

        // 更新评论内容
        await markdownRepository.UpdateReviewAsync(request.ReviewGuid, request.Content);
        await markdownRepository.UnitOfWork.SavaChangesAsync(cancellationToken);

        logger.LogInformation("评论已更新：{ReviewGuid}", request.ReviewGuid);
        return true;
    }
}
