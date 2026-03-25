using System.Security.Cryptography;
using System.Text;
using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Infrastructure.EntityFramework;
using Markdown.Web.API.Application.Commands;
using Markdown.Web.API.Application.IntegrationEventHandlers;
using Notcomd.Evenbus;
using NotMediator;

namespace Markdown.Web.API.Application.CommandHandlers;

/// <summary>
///  创建 Markdown 文档命令处理器
/// </summary>
public class CreateMarkdownCommandHandler(
    IMarkdownRepository markdownRepository,
    MarkDownDbContext dbContext,
    IEventBus eventBus) : IRequestHandler<CreateMarkdownCommand, bool>
{
    public async Task<bool> Handler(CreateMarkdownCommand request, CancellationToken cancellationToken)
    {
        // 计算 MD5 Hash（如果没有提供）
        var md5Hash = request.MarkDownHash ?? ComputeMd5(request.MarkDownContent);

        // 使用 Builder 模式创建实体
        var markdown = new MarkDown.MarkDownBuilder(
            request.MarkUserGuid,
            request.MarkDownName,
            request.MarkDownContent,
            md5Hash
        );

        // 添加标签
        if (request.Tags != null)
        {
            markdown.WithTags(request.Tags);
        }

        var markdownEntity = markdown.Build();

        // 添加到数据库
        await dbContext.Markdowns.AddAsync(markdownEntity, cancellationToken);
        await dbContext.SavaChangesAsync(cancellationToken);

        // 发布集成事件
        await eventBus.Publish("MarkdownCreated", new MarkdownCreatedEventData
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