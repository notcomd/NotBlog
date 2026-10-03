using Markdown.Infrastructure.Idempotent;
using Markdown.Web.API.Application.IntegrationEvents;

namespace Markdown.Web.API.Application.Commands.MarkReview;

/// <summary>
/// 创建 MarkReview 评论命令处理器
/// </summary>
public class CreateMarkReviewCommandHandler(
    IMarkdownRepository markdownRepository,
    IRequestManagement requestManagement,
    IEventBus eventBus,
    ILogger<CreateMarkReviewCommandHandler> logger) :  IRequestHandler<CreateMarkReviewCommand, Guid>
{
    public async Task<Guid> Handler(CreateMarkReviewCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
            // 创建评论实体（完全限定命名空间，避免与 Commands.MarkReview 命名空间同名冲突）
            var review = new global::Markdown.Domain.Entities.MarkReview(
                request.MarkDownGuid,
                request.UserId,
                request.Content,
                request.ReviewImages
            );

            // 通过聚合根添加评论
            await markdownRepository.AddReviewAsync(request.MarkDownGuid, review);
            await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // 评论发布通知（D-5：顶级评论通知博客作者；博客作者本人评论不通知，Message 侧亦会过滤）
            var markdown = await markdownRepository.FindMarkDownAsync(request.MarkDownGuid);
            if (markdown is not null && markdown.MarkUserGuid != request.UserId)
            {
                await EventPublishing.PublishSafelyAsync(eventBus, new MarkdownCommentPublishedIntegrationEvent(
                    request.MarkDownGuid,
                    markdown.MarkDownName,
                    review.MarkReviewGuid,
                    ParentReviewGuid: null,
                    CommentContent: TruncatePreview(request.Content),
                    ActorUserId: request.UserId,
                    TargetUserId: markdown.MarkUserGuid,
                    OccurredAt: review.MarkReviewTime), logger);
            }

            // 发布集成事件（总线故障不拖垮业务，P1-6）
            await EventPublishing.PublishSafelyAsync(eventBus, new MarkReviewCreatedIntegrationEvent(
                review.MarkReviewGuid,
                request.MarkDownGuid,
                request.UserId,
                string.Empty, // UserName 由消费端查询
                request.Content,
                review.MarkReviewTime
            ), logger);

            logger.LogInformation("评论已创建：{ReviewGuid} -> 文档 {MarkDownGuid}", review.MarkReviewGuid, request.MarkDownGuid);
            return review.MarkReviewGuid;
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
