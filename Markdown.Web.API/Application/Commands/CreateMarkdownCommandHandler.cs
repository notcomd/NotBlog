using System.Security.Cryptography;
using System.Text;
using Markdown.Domain.Entities;
using Markdown.Infrastructure.EntityFramework;
using Markdown.Web.API.Application.Commands;
using Markdown.Web.API.Application.IntegrationEventHandlers;
using Notcomd.Evenbus;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
///  创建 Markdown 文档命令处理器
/// </summary>
public class CreateMarkdownCommandHandler(
    MarkDownDbContext dbContext,
    IEventBus eventBus) : NotMediator.IRequestHandler<CreateMarkdownCommand, bool>
{
    public async Task<bool> Handler(CreateMarkdownCommand request, CancellationToken cancellationToken)
    {
        // 计算 MD5 Hash（如果没有提供）
        var md5Hash = request.MarkDownHash ?? ComputeMd5(request.MarkDownContent);

        // 使用 Builder 模式创建实体
        var builder = new MarkDown.MarkDownBuilder(
            request.MarkUserGuid,
            request.MarkDownName,
            request.MarkDownContent,
            md5Hash
        );

        // 添加标签
        if (request.Tags != null)
        {
            builder.WithTags(request.Tags);
        }

        // 设置文档权限
        builder.WithMarkDownAuth(request.MarkDownAuth);

        var markdownEntity = builder.Build();

        // 通过 UnitOfWork 写入
        await dbContext.Markdowns.AddAsync(markdownEntity, cancellationToken);
        await dbContext.SavaChangesAsync(cancellationToken);

        // 发布集成事件
        await eventBus.PublishAsync(new MarkdownCreatedEventData
        {
            MarkDownGuid = markdownEntity.MarkDownGuid,
            MarkUserGuid = markdownEntity.MarkUserGuid,
            FileName = markdownEntity.MarkDownName,
            CreatedAt = markdownEntity.CreateAt
        });

        return true;
    }

    /// <summary>
    ///     计算内容 MD5
    /// </summary>
    private static string ComputeMd5(string content)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = md5.ComputeHash(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}