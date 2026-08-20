namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 创建 Markdown 文档命令
/// </summary>
public record CreateMarkdownCommand(
    Guid MarkUserGuid,
    string MarkDownName,
    string MarkDownContent,
    string? MarkDownHash = null,
    IEnumerable<string>? Tags = null,
    MarkDownAuth MarkDownAuth = MarkDownAuth.PublicMark,
    string? CoverUrl = null
) : IRequest<Guid>, ICommandRequest;
