namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// Markdown 文章摘要响应（列表/搜索使用，不返回正文）
/// </summary>
public class MarkdownSummaryResponse
{
    public Guid MarkDownGuid { get; set; }

    public string Name { get; set; } = null!;

    public List<string> Tags { get; set; } = new();

    public string? CoverUrl { get; set; }

    public string Auth { get; set; } = null!;

    public string Status { get; set; } = null!;

    /// <summary>
    ///     审核驳回原因（仅当前处于驳回状态时有值，其余状态为 null）
    /// </summary>
    public string? RejectReason { get; set; }

    public DateTimeOffset CreateAt { get; set; }

    public DateTimeOffset UpdateAt { get; set; }
}