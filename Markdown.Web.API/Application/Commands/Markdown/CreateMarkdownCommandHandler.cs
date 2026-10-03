namespace Markdown.Web.API.Application.Commands.Markdown;

/// <summary>
///  创建 Markdown 文档命令处理器（文件化：正文先存文件存储，实体只落文件元数据）
/// </summary>
public class CreateMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    IMarkdownContentStore contentStore,
    IEventBus eventBus,
    ILogger<CreateMarkdownCommandHandler> logger) :  IRequestHandler<CreateMarkdownCommand, Guid>
{
    public async Task<Guid> Handler(CreateMarkdownCommand request, CancellationToken cancellationToken)
    {
        // 1. 计算内容哈希（如果没有提供）
        var contentHash = request.MarkDownHash ?? ComputeSha256(request.MarkDownContent);

        // 2. 正文文件化：保存内容到文件存储，拿到文件标识
        // （文档 MarkDownGuid 由实体构造内部生成，创建期无法预知，故不设内容附件引用）
        var fileId = await contentStore.SaveAsync(request.MarkDownContent, contentId: null, cancellationToken);
        var fileSize = Encoding.UTF8.GetByteCount(request.MarkDownContent);

        // 3. 使用 Builder 模式创建实体（只含文件元数据，不含正文）
        var builder = new MarkDown.MarkDownBuilder(
            request.MarkUserGuid,
            request.MarkDownName,
            fileId,
            fileId,
            fileSize,
            ".md",
            contentHash
        );

        // 4. 添加标签
        if (request.Tags != null)
        {
            builder.WithTags(request.Tags);
        }

        // 5. 设置文档权限与封面
        builder.WithMarkDownAuth(request.MarkDownAuth);
        builder.WithCoverUrl(request.CoverUrl);

        var markdownEntity = builder.Build();

        // 6. 通过聚合根仓储写入（不绕过仓储直接操作 DbContext）
        await markdownRepository.AddAsync(markdownEntity);
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        // 7. 发布集成事件（总线故障不拖垮业务，P1-6）
        await EventPublishing.PublishSafelyAsync(eventBus, new MarkdownCreatedIntegrationEvent
        {
            MarkDownGuid = markdownEntity.MarkDownGuid,
            MarkUserGuid = markdownEntity.MarkUserGuid,
            FileName = markdownEntity.MarkDownName,
            CreatedAt = markdownEntity.CreateAt
        }, logger);

        return markdownEntity.MarkDownGuid;
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
