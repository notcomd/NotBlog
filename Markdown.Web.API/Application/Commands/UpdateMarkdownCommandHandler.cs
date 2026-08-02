namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 更新 Markdown 文档命令处理器
/// </summary>
public class UpdateMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    ILogger<UpdateMarkdownCommandHandler> logger) : NotMediator.IRequestHandler<UpdateMarkdownCommand, bool>
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

        // 4. 创建历史版本快照（聚合根内部管理，EF Core 级联持久化）
        markdown.CreateHistorySnapshot();

        // 5. 计算内容哈希
        var md5Hash = request.MarkDownHash ?? ComputeMd5(request.MarkDownContent);

        // 6. 更新文档内容
        await markdown.UpDataByMarkDownAsync(
            request.MarkDownName,
            request.MarkDownContent,
            md5Hash);

        // 7. 更新标签（如果提供）
        if (request.Tags is not null)
        {
            markdown.ClearTags();
            markdown.AddTags(request.Tags);
        }

        // 8. 通过 UnitOfWork 保存更改
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Markdown 文档已更新：{MarkDownGuid}", request.MarkDownGuid);
        return true;
    }

    /// <summary>
    /// 计算内容 MD5
    /// </summary>
    private static string ComputeMd5(string content)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = md5.ComputeHash(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
