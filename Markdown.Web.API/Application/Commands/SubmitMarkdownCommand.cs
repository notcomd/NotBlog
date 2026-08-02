namespace Markdown.Web.API.Application.Commands;

/// <summary>
/// 提交 Markdown 文档审核命令（草稿/驳回 -> 待审核，仅作者）
/// </summary>
public record SubmitMarkdownCommand(
    Guid MarkDownGuid,
    Guid UserId
) : IRequest<bool>;