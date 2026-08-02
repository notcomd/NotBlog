namespace Markdown.Web.API.Application.Dto;

/// <summary>
/// Markdown 文章摘要响应（列表/搜索使用，不返回正文）
/// </summary>
public class MarkdownSummaryResponse
{
    public Guid MarkDownGuid { get; set; }

    public string Name { get; set; } = null!;

    public List<string> Tags { get; set; } = new();

    public string Auth { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTimeOffset CreateAt { get; set; }

    public DateTimeOffset UpdateAt { get; set; }
}