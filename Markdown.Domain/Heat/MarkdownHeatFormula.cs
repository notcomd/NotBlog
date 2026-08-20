namespace Markdown.Domain.Heat;

/// <summary>
///     Markdown 文档热点分计算公式（用户定稿：互动优先 50% / 浏览 30% / 时间衰减 20%）。
///     <para>
///     HeatScore = 0.5 × Interaction + 0.3 × View + 0.2 × Freshness
///     Interaction = log10(1 + Love×1 + Favorite×2 + Share×3 + Coin×5)  （互动子项权重：收藏/分享/硬币高于点赞）
///     View = log10(1 + ViewCount)                                       （浏览量大，log 压缩）
///     Freshness = exp(-ageDays / 7)                                     （7 天半衰期：新文档权重高）
///     </para>
/// </summary>
public static class MarkdownHeatFormula
{
    /// <summary>互动权重（50%）</summary>
    public const double InteractionWeight = 0.5;

    /// <summary>浏览权重（30%）</summary>
    public const double ViewWeight = 0.3;

    /// <summary>时间衰减权重（20%）</summary>
    public const double FreshnessWeight = 0.2;

    /// <summary>时间衰减半衰期（天）</summary>
    public const double HalfLifeDays = 7;

    /// <summary>
    ///     计算热点分（输入为文档当前计数与创建时间）
    /// </summary>
    public static double Calculate(MarkQuote quote, DateTimeOffset createdAt, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(quote);

        var interaction = Math.Log10(1 + quote.LoveSome * 1
                                       + quote.FavoriteSome * 2
                                       + quote.ShareSome * 3
                                       + quote.CoinSome * 5);

        var view = Math.Log10(1 + quote.ViewSome);

        var ageDays = Math.Max(0, (now - createdAt).TotalDays);
        var freshness = Math.Exp(-ageDays / HalfLifeDays);

        return InteractionWeight * interaction
               + ViewWeight * view
               + FreshnessWeight * freshness;
    }
}
