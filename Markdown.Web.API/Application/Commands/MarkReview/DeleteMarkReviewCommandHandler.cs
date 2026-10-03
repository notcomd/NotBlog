using Markdown.Infrastructure.Idempotent;
using Markdown.Web.API.Application.IntegrationEvents;

namespace Markdown.Web.API.Application.Commands.MarkReview;

/// <summary>
/// 删除 MarkReview 评论命令处理器（软删除）
/// </summary>
public class DeleteMarkReviewCommandHandler(
    IMarkdownRepository markdownRepository,
    IRequestManagement requestManagement,
    IEventBus eventBus,
    ILogger<DeleteMarkReviewCommandHandler> logger) :  IRequestHandler<DeleteMarkReviewCommand, bool>
{
    public async Task<bool> Handler(DeleteMarkReviewCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
            // 获取评论验证所有权
            var review = await markdownRepository.GetReviewByIdAsync(request.ReviewGuid)
                ?? throw new KeyNotFoundException($"评论不存在：{request.ReviewGuid}");

            if (review.IsDelete)
                throw new InvalidOperationException("评论已被删除");

            if (review.UserId != request.UserId)
            {
                logger.LogWarning("用户 {UserGuid} 无权删除评论 {ReviewGuid}", request.UserId, request.ReviewGuid);
                throw new UnauthorizedAccessException("无权删除此评论");
            }

            // 通过聚合根删除评论
            await markdownRepository.DeleteReviewAsync(request.ReviewGuid);
            await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // 发布集成事件（总线故障不拖垮业务，P1-6）
            await EventPublishing.PublishSafelyAsync(eventBus, new MarkReviewDeletedIntegrationEvent(
                request.ReviewGuid,
                review.MarkDownGuid,
                DateTimeOffset.UtcNow
            ), logger);

            logger.LogInformation("评论已软删除：{ReviewGuid}", request.ReviewGuid);
            return true;
        });
    }

}
