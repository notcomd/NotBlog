namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 提交 Markdown 文档审核命令处理器
/// </summary>
public class SubmitMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    ILogger<SubmitMarkdownCommandHandler> logger) :  IRequestHandler<SubmitMarkdownCommand, bool>
{
    public async Task<bool> Handler(SubmitMarkdownCommand request, CancellationToken cancellationToken)
    {
        var markdown = await markdownRepository.GetMarkDownTrackedAsync(request.MarkDownGuid)
            ?? throw new KeyNotFoundException($"Markdown 文档不存在：{request.MarkDownGuid}");

        if (markdown.IsDelete)
            throw new InvalidOperationException("已删除的文档无法提交审核");

        if (markdown.MarkUserGuid != request.UserId)
            throw new UnauthorizedAccessException("仅作者可提交审核");

        markdown.SubmitForReview();
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Markdown 文档已提交审核：{MarkDownGuid}", request.MarkDownGuid);
        return true;
    }
}