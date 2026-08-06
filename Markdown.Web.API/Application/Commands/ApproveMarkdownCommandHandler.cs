namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 审核通过 Markdown 文档命令处理器
/// </summary>
public class ApproveMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    ILogger<ApproveMarkdownCommandHandler> logger) : NotMediator.IRequestHandler<ApproveMarkdownCommand, bool>
{
    public async Task<bool> Handler(ApproveMarkdownCommand request, CancellationToken cancellationToken)
    {
        var markdown = await markdownRepository.GetMarkDownTrackedAsync(request.MarkDownGuid)
            ?? throw new KeyNotFoundException($"Markdown 文档不存在：{request.MarkDownGuid}");

        if (markdown.IsDelete)
            throw new InvalidOperationException("已删除的文档无法审核");

        // P1-3：审核必须由管理员执行——作者自审会让审核流程形同虚设
        if (!request.IsAdmin)
            throw new UnauthorizedAccessException("仅管理员可执行审核操作");

        markdown.Approve();
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Markdown 文档审核通过：{MarkDownGuid}", request.MarkDownGuid);
        return true;
    }
}