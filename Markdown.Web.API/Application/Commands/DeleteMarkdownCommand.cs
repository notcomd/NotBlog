namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 删除 Markdown 文档命令（软删除）
/// </summary>
public record DeleteMarkdownCommand(
    Guid MarkDownGuid,
    Guid MarkUserGuid,
    Guid IdempotencyKey
) : IRequest<bool>, ICommandRequest;
