namespace Markdown.Web.API.Application.Commands.Markdown;

/// <summary>
/// 审核通过 Markdown 文档命令（待审核 -> 通过，仅作者/管理员）
/// </summary>
public record ApproveMarkdownCommand(
    Guid MarkDownGuid,
    Guid UserId,
    bool IsAdmin
) : IRequest<bool>, ICommandRequest;