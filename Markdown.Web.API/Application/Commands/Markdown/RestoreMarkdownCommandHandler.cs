namespace Markdown.Web.API.Application.Commands.Markdown;

/// <summary>
/// 从历史版本还原 Markdown 文档命令处理器（文件化：还原前快照当前文件内容，历史内容重新保存为文件）
/// </summary>
public class RestoreMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    IMarkdownContentStore contentStore,
    ILogger<RestoreMarkdownCommandHandler> logger) :  IRequestHandler<RestoreMarkdownCommand, bool>
{
    public async Task<bool> Handler(RestoreMarkdownCommand request, CancellationToken cancellationToken)
    {
        // 1. 加载文档并校验
        var markdown = await markdownRepository.GetMarkDownTrackedAsync(request.MarkDownGuid)
            ?? throw new KeyNotFoundException($"Markdown 文档不存在：{request.MarkDownGuid}");

        if (markdown.IsDelete)
            throw new InvalidOperationException("已删除的文档无法还原");

        if (markdown.MarkUserGuid != request.UserId)
            throw new UnauthorizedAccessException("仅作者可执行历史还原");

        // 1.1 状态门禁：还原属写操作，审核中/已发布不可还原（仅草稿/驳回可还原）
        markdown.EnsureEditable();

        // 2. 加载历史版本并校验归属
        var oldVersion = await markdownRepository.GetOldMarkDownByGuidAsync(request.OldMarkDownGuid)
            ?? throw new KeyNotFoundException($"历史版本不存在：{request.OldMarkDownGuid}");

        if (oldVersion.IsDelete)
            throw new InvalidOperationException("历史版本已被删除");

        if (oldVersion.MarkDownGuid != request.MarkDownGuid)
            throw new InvalidOperationException("历史版本不属于该文档");

        // 3. 还原前先为当前版本创建历史快照，避免当前内容丢失（当前内容从文件流读取）
        var oldFileId = markdown.FileId;
        var oldContent = await contentStore.ReadAsync(oldFileId, cancellationToken);
        if (oldContent is not null)
        {
            markdown.CreateHistorySnapshot(oldContent);
        }
        else
        {
            logger.LogWarning("文档 {MarkDownGuid} 正文文件 {FileId} 不存在，跳过还原前快照",
                request.MarkDownGuid, markdown.FileId);
        }

        // 4. 历史版本内容重新保存为文件，更新元数据（文件引用）
        var fileId = await contentStore.SaveAsync(oldVersion.OldMarkDownContent, request.MarkDownGuid, cancellationToken);
        var fileSize = Encoding.UTF8.GetByteCount(oldVersion.OldMarkDownContent);

        await markdown.RestoreFromHistory(oldVersion, fileId, fileId, fileSize, ".md");
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        // 清理还原前旧正文文件（还原前已快照到 DB 全文；删除失败不影响业务）
        try
        {
            await contentStore.DeleteAsync(oldFileId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "清理旧正文文件失败：{FileId}", oldFileId);
        }

        logger.LogInformation("Markdown 文档 {MarkDownGuid} 已从历史版本 {OldGuid} 还原", request.MarkDownGuid, request.OldMarkDownGuid);
        return true;
    }
}
