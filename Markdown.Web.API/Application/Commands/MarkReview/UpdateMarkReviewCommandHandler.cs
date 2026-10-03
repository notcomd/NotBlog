using Markdown.Infrastructure.Idempotent;

namespace Markdown.Web.API.Application.Commands.MarkReview;

/// <summary>
/// 更新 MarkReview 评论命令处理器
/// </summary>
public class UpdateMarkReviewCommandHandler(
    IMarkdownRepository markdownRepository,
    IRequestManagement requestManagement,
    ILogger<UpdateMarkReviewCommandHandler> logger) :  IRequestHandler<UpdateMarkReviewCommand, bool>
{
    public async Task<bool> Handler(UpdateMarkReviewCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
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
            await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("评论已更新：{ReviewGuid}", request.ReviewGuid);
            return true;
        });
    }

}
