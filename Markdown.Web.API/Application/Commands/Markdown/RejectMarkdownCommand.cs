namespace Markdown.Web.API.Application.Commands.Markdown;

/// <summary>
/// 审核驳回 Markdown 文档命令（待审核 -> 驳回，仅管理员）
/// </summary>
/// <param name="Reason">驳回原因（可空，透传并记录到文档）</param>
public record RejectMarkdownCommand(
    Guid MarkDownGuid,
    Guid UserId,
    bool IsAdmin,
    string? Reason = null
) : IRequest<bool>, ICommandRequest;