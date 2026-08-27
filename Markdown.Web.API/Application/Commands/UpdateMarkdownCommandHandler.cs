namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 更新 Markdown 文档命令处理器（文件化：更新前从文件流读取旧内容做历史快照，正文变更走文件存储）
/// </summary>
public class UpdateMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    IMarkdownContentStore contentStore,
    ILogger<UpdateMarkdownCommandHandler> logger) :  IRequestHandler<UpdateMarkdownCommand, bool>
{
    public async Task<bool> Handler(UpdateMarkdownCommand request, CancellationToken cancellationToken)
    {
        // 1. 加载实体
        var markdown = await markdownRepository.GetMarkDownTrackedAsync(request.MarkDownGuid);

        if (markdown is null)
        {
            logger.LogWarning("尝试更新不存在的 Markdown 文档：{MarkDownGuid}", request.MarkDownGuid);
            throw new KeyNotFoundException($"Markdown 文档不存在：{request.MarkDownGuid}");
        }

        // 2. 验证所有权（垂直权限控制）
        if (markdown.MarkUserGuid != request.MarkUserGuid)
        {
            logger.LogWarning("用户 {UserGuid} 无权修改文档 {MarkDownGuid}", request.MarkUserGuid, request.MarkDownGuid);
            throw new UnauthorizedAccessException("无权修改此文档");
        }

        // 3. 检查软删除
        if (markdown.IsDelete)
        {
            throw new InvalidOperationException("已删除的文档无法修改");
        }

        // 4. 创建历史版本快照（正文已文件化：从文件存储读取当前旧内容作为快照）
        var oldFileId = markdown.FileId;
        var oldContent = await contentStore.ReadAsync(oldFileId, cancellationToken);
        if (oldContent is not null)
        {
            markdown.CreateHistorySnapshot(oldContent);
        }
        else
        {
            logger.LogWarning("文档 {MarkDownGuid} 正文文件 {FileId} 不存在，跳过历史快照",
                request.MarkDownGuid, oldFileId);
        }

        // 5. 计算内容哈希
        var contentHash = request.MarkDownHash ?? ComputeSha256(request.MarkDownContent);

        // 6. 保存新正文文件，更新元数据（文件引用）
        var fileId = await contentStore.SaveAsync(request.MarkDownContent, request.MarkDownGuid, cancellationToken);
        var fileSize = Encoding.UTF8.GetByteCount(request.MarkDownContent);

        await markdown.UpdateByMarkDownAsync(
            request.MarkDownName,
            fileId,
            fileId,
            fileSize,
            ".md",
            contentHash);

        // 7. 更新标签（如果提供）与封面（null 不修改，空串清除）
        if (request.Tags is not null)
        {
            markdown.ClearTags();
            markdown.AddTags(request.Tags);
        }

        markdown.UpdateCoverUrl(request.CoverUrl);

        // 8. 通过 UnitOfWork 保存更改
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        // 9. 清理旧正文文件（历史快照已存 DB 全文，旧文件无引用价值；删除失败不影响业务）
        try
        {
            await contentStore.DeleteAsync(oldFileId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "清理旧正文文件失败：{FileId}", oldFileId);
        }

        logger.LogInformation("Markdown 文档已更新：{MarkDownGuid}", request.MarkDownGuid);
        return true;
    }

    /// <summary>
    ///     计算内容 SHA-256 哈希（P2-4：MD5 仅适合校验不适合内容指纹语义，升级为 SHA-256）
    /// </summary>
    private static string ComputeSha256(string content)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(hashBytes);
    }
}
