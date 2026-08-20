namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// Markdown 文章响应（元数据 + 交互统计；正文经 GET /content 流式获取）
/// </summary>
public class MarkdownResponse
{
    public Guid MarkDownGuid { get; set; }
    public string Name { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public string FileId { get; set; } = null!;
    public long FileSize { get; set; }
    public string FileExt { get; set; } = null!;
    public string? CoverUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Auth { get; set; } = null!;
    public DateTimeOffset CreateAt { get; set; }
    public DateTimeOffset UpdateAt { get; set; }

    /// <summary>
    ///     文档交互统计（浏览/点赞/收藏/分享/硬币/热度）
    /// </summary>
    public MarkQuoteResponse? Quote { get; set; }
}
