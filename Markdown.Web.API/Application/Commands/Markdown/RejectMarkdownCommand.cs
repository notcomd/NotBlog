namespace Markdown.Web.API.Application.Commands.Markdown;

/// <summary>
/// 审核驳回 Markdown 文档命令（待审核 -> 驳回，仅作者/管理员）
/// </summary>
public record RejectMarkdownCommand(
    Guid MarkDownGuid,
    Guid UserId,
    bool IsAdmin
) : IRequest<bool>, ICommandRequest;