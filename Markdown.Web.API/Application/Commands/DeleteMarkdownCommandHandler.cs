using Markdown.Infrastructure.Idempotent;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 删除 Markdown 文档命令处理器（软删除）
/// </summary>
public class DeleteMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    IRequestManagement requestManagement,
    ILogger<DeleteMarkdownCommandHandler> logger) :  IRequestHandler<DeleteMarkdownCommand, bool>
{
    public async Task<bool> Handler(DeleteMarkdownCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
            // 加载实体
            var markdown = await markdownRepository.GetMarkDownTrackedAsync(request.MarkDownGuid);

            if (markdown is null)
            {
                logger.LogWarning("尝试删除不存在的 Markdown 文档：{MarkDownGuid}", request.MarkDownGuid);
                throw new KeyNotFoundException($"Markdown 文档不存在：{request.MarkDownGuid}");
            }

            // 验证所有权
            if (markdown.MarkUserGuid != request.MarkUserGuid)
            {
                logger.LogWarning("用户 {UserGuid} 无权删除文档 {MarkDownGuid}", request.MarkUserGuid, request.MarkDownGuid);
                throw new UnauthorizedAccessException("无权删除此文档");
            }

            // 检查是否已删除
            if (markdown.IsDelete)
            {
                throw new InvalidOperationException("文档已被删除");
            }

            // 通过聚合根方法执行软删除
            markdown.SoftDelete();
            await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Markdown 文档已软删除：{MarkDownGuid}", request.MarkDownGuid);
            return true;
        });
    }

}
