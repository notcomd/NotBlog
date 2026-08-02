using Markdown.Infrastructure.Idempotent;
using Markdown.Web.API.Application.IntegrationEvents;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 创建 MarkReview 评论命令处理器
/// </summary>
public class CreateMarkReviewCommandHandler(
    IMarkdownRepository markdownRepository,
    IRequestManagement requestManagement,
    IEventBus eventBus,
    ILogger<CreateMarkReviewCommandHandler> logger) : NotMediator.IRequestHandler<CreateMarkReviewCommand, Guid>
{
    public async Task<Guid> Handler(CreateMarkReviewCommand request, CancellationToken cancellationToken)
    {
        // 幂等性检查
        await requestManagement.CreateRequestForCommandAsync<CreateMarkReviewCommand>(request.IdempotencyKey);

        // 创建评论实体
        var review = new MarkReview(
            request.MarkDownGuid,
            request.UserId,
            request.Content,
            request.ReviewImages
        );

        // 通过聚合根添加评论
        await markdownRepository.AddReviewAsync(request.MarkDownGuid, review);
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        // 发布集成事件
        await eventBus.PublishAsync(new MarkReviewCreatedIntegrationEvent(
            review.MarkReviewGuid,
            request.MarkDownGuid,
            request.UserId,
            string.Empty, // UserName 由消费端查询
            request.Content,
            review.MarkReviewTime
        ));

        logger.LogInformation("评论已创建：{ReviewGuid} -> 文档 {MarkDownGuid}", review.MarkReviewGuid, request.MarkDownGuid);
        return review.MarkReviewGuid;
    }
}
