namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// MarkQuote 文档交互统计响应（浏览/点赞/收藏/分享/硬币/热度）
/// </summary>
public class MarkQuoteResponse
{
    public long LoveCount { get; set; }
    public long FavoriteCount { get; set; }
    public long ShareCount { get; set; }
    public long CoinCount { get; set; }
    public long ViewCount { get; set; }
    public double HeatScore { get; set; }
    public long TotalInteractions { get; set; }
}
