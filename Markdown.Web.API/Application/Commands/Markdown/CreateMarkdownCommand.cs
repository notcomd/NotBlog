namespace Markdown.Web.API.Application.Commands.Markdown;

/// <summary>
/// 创建 Markdown 文档命令
/// </summary>
/// <param name="AsDraft">是否仅保存为草稿（默认 true）。false 时创建后立即提交审核（进入待审核，不会直接置为通过）</param>
public record CreateMarkdownCommand(
    Guid MarkUserGuid,
    string MarkDownName,
    string MarkDownContent,
    string? MarkDownHash = null,
    IEnumerable<string>? Tags = null,
    MarkDownAuth MarkDownAuth = MarkDownAuth.PublicMark,
    string? CoverUrl = null,
    bool AsDraft = true
) : IRequest<Guid>, ICommandRequest;
