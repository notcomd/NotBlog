namespace Markdown.Web.API.Dto.Response;

/// <summary>
///     MarkDown 实体 → 响应 DTO 映射（元数据 + 交互统计；正文经 GET /content 获取）
/// </summary>
public static class MarkdownResponseMapper
{
    public static MarkdownResponse MapToMarkdownResponse(MarkDown markdown) => new()
    {
        MarkDownGuid = markdown.MarkDownGuid,
        Name = markdown.MarkDownName,
        Hash = markdown.MarkDownHash,
        FileId = markdown.FileId,
        FileSize = markdown.FileSize,
        FileExt = markdown.FileExt,
        CoverUrl = markdown.CoverUrl,
        Tags = [.. markdown.MarkDownTagboard],
        Auth = markdown.MarkDownAuth.ToString(),
        CreateAt = markdown.CreateAt,
        UpdateAt = markdown.UpdateAt,
        Quote = markdown.MarkQuote is not null ? MapToMarkQuoteResponse(markdown.MarkQuote) : null
    };

    public static MarkQuoteResponse MapToMarkQuoteResponse(MarkQuote quote) => new()
    {
        LoveCount = quote.LoveSome,
        FavoriteCount = quote.FavoriteSome,
        ShareCount = quote.ShareSome,
        CoinCount = quote.CoinSome,
        ViewCount = quote.ViewSome,
        HeatScore = quote.HeatScore,
        TotalInteractions = quote.GetTotalInteractions()
    };
}
