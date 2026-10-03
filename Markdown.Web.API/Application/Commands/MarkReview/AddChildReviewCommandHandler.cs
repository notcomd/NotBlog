using Markdown.Infrastructure.Idempotent;
using Markdown.Web.API.Application.IntegrationEvents;

namespace Markdown.Web.API.Application.Commands.MarkReview;

/// <summary>
/// 添加子评论命令处理器
/// </summary>
public class AddChildReviewCommandHandler(
    IMarkdownRepository markdownRepository,
    IRequestManagement requestManagement,
    IEventBus eventBus,
    ILogger<AddChildReviewCommandHandler> logger) :  IRequestHandler<AddChildReviewCommand, Guid>
{
    public async Task<Guid> Handler(AddChildReviewCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
            // 创建子评论实体（完全限定命名空间，避免与 Commands.MarkReview 命名空间同名冲突）
            var childReview = new global::Markdown.Domain.Entities.MarkReview(
                request.MarkDownGuid,
                request.UserId,
                request.Content,
                request.ReviewImages
            );

            await childReview.UpdateByMarkReviewAuthAsync(request.ReviewAuth);

            // 通过聚合根添加子评论（内部会关联父评论并更新父评论回复计数）
            await markdownRepository.AddChildReviewAsync(request.MarkDownGuid, request.ParentReviewGuid, childReview);
            await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // 回复通知（D-5：子评论通知被回复的父评论作者；本人回复自己不通知，Message 侧亦会过滤）
            var markdown = await markdownRepository.FindMarkDownAsync(request.MarkDownGuid);
            var parentReview = await markdownRepository.GetReviewByIdAsync(request.ParentReviewGuid);
            if (markdown is not null && parentReview is not null && parentReview.UserId != request.UserId)
            {
                await EventPublishing.PublishSafelyAsync(eventBus, new MarkdownCommentPublishedIntegrationEvent(
                    request.MarkDownGuid,
                    markdown.MarkDownName,
                    childReview.MarkReviewGuid,
                    ParentReviewGuid: request.ParentReviewGuid,
                    CommentContent: TruncatePreview(request.Content),
                    ActorUserId: request.UserId,
                    TargetUserId: parentReview.UserId,
                    OccurredAt: childReview.MarkReviewTime), logger);
            }

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

    /// <summary>
    ///     评论内容预览截断（供通知文案使用；空内容/图片评论回退占位符）
    /// </summary>
    private static string TruncatePreview(string? content, int max = 50)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "[图片评论]";

        return content.Length <= max ? content : content[..max] + "…";
    }

}