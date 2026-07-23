using NotMediator;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 更新 Markdown 文档命令
/// </summary>
public record UpdateMarkdownCommand(
    Guid MarkDownGuid,
    Guid MarkUserGuid,
    string MarkDownName,
    string MarkDownContent,
    string? MarkDownHash = null,
    IEnumerable<string>? Tags = null
) : IRequest<bool>;
