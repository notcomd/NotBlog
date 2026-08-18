namespace Video.Domain.Options;

/// <summary>
/// ReviewContent 的可配置选项。
/// 通过 appsettings.json 的 "ReviewContent" 节绑定，支持运行时切换参数。
/// 所有属性均有合理默认值，省略配置时等同于当前硬编码行为。
/// </summary>
public record ReviewContentOptions
{
    /// <summary>文本评论最大长度</summary>
    public int MaxTextLength { get; init; } = 2000;

    /// <summary>图片评论最大数量</summary>
    public int MaxImageCount { get; init; } = 9;

    /// <summary>单张图片最大大小 (字节)</summary>
    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>图片允许的扩展名（逗号分隔）</summary>
    public string AllowedImageExtensions { get; init; } = ".jpg,.jpeg,.png,.webp,.gif,.bmp";

    /// <summary>视频评论最大时长 (秒)</summary>
    public int MaxVideoDurationSeconds { get; init; } = 300;

    /// <summary>视频允许的扩展名（逗号分隔）</summary>
    public string AllowedVideoExtensions { get; init; } = ".mp4,.webm,.mov,.avi,.mkv";

    /// <summary>富文本最大长度（含 HTML 标签）</summary>
    public int MaxRichTextLength { get; init; } = 10000;

    /// <summary>富文本允许的 HTML 标签（逗号分隔）</summary>
    public string AllowedHtmlTags { get; init; } = "b,i,u,strong,em,s,p,br,a,ul,ol,li,h1,h2,h3,h4,blockquote,code,pre,span,div,img";

    /// <summary>
    /// 内置默认值（用于未配置时回退）
    /// </summary>
    public static readonly ReviewContentOptions Default = new();

    /// <summary>
    /// 将逗号分隔的扩展名字符串解析为 HashSet
    /// </summary>
    public HashSet<string> GetImageExtensionSet()
    {
        return ParseCsv(AllowedImageExtensions);
    }

    /// <summary>
    /// 将逗号分隔的扩展名字符串解析为 HashSet
    /// </summary>
    public HashSet<string> GetVideoExtensionSet()
    {
        return ParseCsv(AllowedVideoExtensions);
    }

    /// <summary>
    /// 将逗号分隔的 HTML 标签字符串解析为 HashSet
    /// </summary>
    public HashSet<string> GetHtmlTagSet()
    {
        return ParseCsv(AllowedHtmlTags);
    }

    /// <summary>
    /// 将逗号分隔的字符串解析为 HashSet
    /// </summary>
    /// <param name="csv">逗号分隔的字符串</param>
    /// <returns>解析后的 HashSet</returns>
    private static HashSet<string> ParseCsv(string csv)
    {
        return new HashSet<string>(
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }
}
