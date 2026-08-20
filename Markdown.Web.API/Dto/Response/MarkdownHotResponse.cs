namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// 热点榜条目响应
/// </summary>
public class MarkdownHotResponse
{
    public Guid MarkDownGuid { get; set; }
    public string Name { get; set; } = null!;
    public double HeatScore { get; set; }
    public DateTimeOffset CreateAt { get; set; }
}
