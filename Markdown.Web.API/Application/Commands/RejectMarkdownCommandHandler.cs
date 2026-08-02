namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 审核驳回 Markdown 文档命令处理器
/// </summary>
public class RejectMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    ILogger<RejectMarkdownCommandHandler> logger) : NotMediator.IRequestHandler<RejectMarkdownCommand, bool>
{
    public async Task<bool> Handler(RejectMarkdownCommand request, CancellationToken cancellationToken)
    {
        var markdown = await markdownRepository.GetMarkDownTrackedAsync(request.MarkDownGuid)
            ?? throw new KeyNotFoundException($"Markdown 文档不存在：{request.MarkDownGuid}");

        if (markdown.IsDelete)
            throw new InvalidOperationException("已删除的文档无法审核");

        if (markdown.MarkUserGuid != request.UserId && !request.IsAdmin)
            throw new UnauthorizedAccessException("仅作者或管理员可执行审核操作");

        markdown.Reject();
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Markdown 文档审核驳回：{MarkDownGuid}", request.MarkDownGuid);
        return true;
    }
}