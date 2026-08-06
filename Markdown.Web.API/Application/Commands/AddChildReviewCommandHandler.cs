using Markdown.Infrastructure.Idempotent;
using Markdown.Web.API.Application.IntegrationEvents;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 添加子评论命令处理器
/// </summary>
public class AddChildReviewCommandHandler(
    IMarkdownRepository markdownRepository,
    IRequestManagement requestManagement,
    IEventBus eventBus,
    ILogger<AddChildReviewCommandHandler> logger) : NotMediator.IRequestHandler<AddChildReviewCommand, Guid>
{
    public async Task<Guid> Handler(AddChildReviewCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
            // 创建子评论实体
            var childReview = new MarkReview(
                request.MarkDownGuid,
                request.UserId,
                request.Content,
                request.ReviewImages
            );

            await childReview.UpdateByMarkReviewAuthAsync(request.ReviewAuth);

            // 通过聚合根添加子评论（内部会关联父评论并更新父评论回复计数）
            await markdownRepository.AddChildReviewAsync(request.MarkDownGuid, request.ParentReviewGuid, childReview);
            await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // 发布集成事件（总线故障不拖垮业务，P1-6）
            await EventPublishing.PublishSafelyAsync(eventBus, new ChildReviewAddedIntegrationEvent(
                request.ParentReviewGuid,
                childReview.MarkReviewGuid,
                request.MarkDownGuid,
                request.UserId,
                childReview.MarkReviewTime
            ), logger);

            logger.LogInformation("子评论已创建：{ChildGuid} -> 父评论 {ParentGuid}", childReview.MarkReviewGuid, request.ParentReviewGuid);
            return childReview.MarkReviewGuid;
        });
    }

}