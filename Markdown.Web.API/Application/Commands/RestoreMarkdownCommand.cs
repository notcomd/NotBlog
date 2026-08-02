namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 从历史版本还原 Markdown 文档命令（仅作者，F-10.3）
/// </summary>
public record RestoreMarkdownCommand(
    Guid MarkDownGuid,
    Guid OldMarkDownGuid,
    Guid UserId
) : IRequest<bool>;