namespace Markdown.Web.API.Application.Dto;

/// <summary>
/// Markdown 文章响应
/// </summary>
public class MarkdownResponse
{
    public Guid MarkDownGuid { get; set; }
    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public string Auth { get; set; } = null!;
    public DateTime CreateAt { get; set; }
    public DateTime UpdateAt { get; set; }
}
