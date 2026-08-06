

using Markdown.Web.API.Application.IntegrationEvents;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
///  创建 Markdown 文档命令处理器
/// </summary>
public class CreateMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    IEventBus eventBus,
    ILogger<CreateMarkdownCommandHandler> logger) : NotMediator.IRequestHandler<CreateMarkdownCommand, Guid>
{
    public async Task<Guid> Handler(CreateMarkdownCommand request, CancellationToken cancellationToken)
    {
        // 计算 MD5 Hash（如果没有提供）
        var contentHash = request.MarkDownHash ?? ComputeSha256(request.MarkDownContent);

        // 使用 Builder 模式创建实体
        var builder = new MarkDown.MarkDownBuilder(
            request.MarkUserGuid,
            request.MarkDownName,
            request.MarkDownContent,
            contentHash
        );

        // 添加标签
        if (request.Tags != null)
        {
            builder.WithTags(request.Tags);
        }

        // 设置文档权限
        builder.WithMarkDownAuth(request.MarkDownAuth);

        var markdownEntity = builder.Build();

        // 通过聚合根仓储写入（不绕过仓储直接操作 DbContext）
        await markdownRepository.AddAsync(markdownEntity);
        await markdownRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        // 发布集成事件（总线故障不拖垮业务，P1-6）
        await EventPublishing.PublishSafelyAsync(eventBus, new MarkdownCreatedEventData
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