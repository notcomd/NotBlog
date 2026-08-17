
namespace Video.Domain.ValueObjects;

/// <summary>
/// 评论内容值对象 — 支持文本、图片、视频、富文本四种内容类型。
/// 所有验证参数通过 ReviewContentOptions 配置（静态 Options 属性），
/// 启动时从 appsettings.json 的 "ReviewContent" 节绑定即可。
/// </summary>
public class ReviewContent : ValueObject
{
    // ── 可配置选项 ──

    /// <summary>
    /// 全局配置实例。启动时可通过 appsettings.json 覆盖，未设置使用 Default。
    /// </summary>
    public static ReviewContentOptions Options { get; set; } = ReviewContentOptions.Default;

    // ── 属性 ──
    /// <summary>内容类型 (text/image/video/richtext)</summary>
    public string ContentType { get; }

    /// <summary>
    /// 内容主体：Text 为纯文本，RichText 为 HTML，Image/Video 为描述。
    /// </summary>
    public string? Body { get; }

    /// <summary>媒体项列表（图片/视频的 URL 引用）</summary>
    public List<VideoImage> MediaItems { get; }

    // ── 构造器 ──

    private ReviewContent(string contentType, string? body, List<VideoImage>? mediaItems)
    {
        ContentType = ReviewContentType.Normalize(contentType);
        Body = body;
        MediaItems = mediaItems ?? [];
    }

    // ── 工厂方法 ──

    /// <summary>创建纯文本评论</summary>
    public static ReviewContent CreateText(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Text content must not be empty.", nameof(body));

        var opt = GetOptions();
        if (body.Length > opt.MaxTextLength)
            throw new ArgumentException(
                $"Text content exceeds maximum length of {opt.MaxTextLength} characters.", nameof(body));

        return new ReviewContent(ReviewContentType.Text, body.Trim(), null);
    }

    /// <summary>创建图片评论</summary>
    public static ReviewContent CreateImage(List<VideoImage> images, string? caption = null)
    {
        var opt = GetOptions();

        if (images is null || images.Count == 0)
            throw new ArgumentException("Image content must contain at least one image.", nameof(images));
        if (images.Count > opt.MaxImageCount)
            throw new ArgumentException(
                $"Image count exceeds maximum of {opt.MaxImageCount}.", nameof(images));
        if (caption is { Length: > 0 } && caption.Length > opt.MaxTextLength)
            throw new ArgumentException(
                $"Image caption exceeds maximum length of {opt.MaxTextLength} characters.", nameof(caption));

        ValidateImageFormats(images, opt);

        return new ReviewContent(ReviewContentType.Image, caption?.Trim(), images);
    }

    /// <summary>创建视频评论</summary>
    public static ReviewContent CreateVideo(List<VideoImage> videoItems, string? description = null)
    {
        var opt = GetOptions();

        if (videoItems is null || videoItems.Count == 0)
            throw new ArgumentException("Video content must contain at least one video.", nameof(videoItems));
        if (description is { Length: > 0 } && description.Length > opt.MaxTextLength)
            throw new ArgumentException(
                $"Video description exceeds maximum length of {opt.MaxTextLength} characters.", nameof(description));

        ValidateVideoFormats(videoItems, opt);

        return new ReviewContent(ReviewContentType.Video, description?.Trim(), videoItems);
    }

    /// <summary>创建富文本评论</summary>
    public static ReviewContent CreateRichText(string richTextBody)
    {
        if (string.IsNullOrWhiteSpace(richTextBody))
            throw new ArgumentException("Rich text content must not be empty.", nameof(richTextBody));

        var opt = GetOptions();
        if (richTextBody.Length > opt.MaxRichTextLength)
            throw new ArgumentException(
                $"Rich text content exceeds maximum length of {opt.MaxRichTextLength} characters.", nameof(richTextBody));

        var sanitized = SanitizeRichText(richTextBody.Trim(), opt);

        return new ReviewContent(ReviewContentType.RichText, sanitized, null);
    }

    /// <summary>兼容构造：不指定类型时的自动推断</summary>
    public static ReviewContent CreateDefault(string? body, List<VideoImage>? images)
    {
        var hasImages = images is { Count: > 0 };
        var hasBody = !string.IsNullOrWhiteSpace(body);

        if (hasImages && !hasBody) return CreateImage(images!);
        if (hasBody && !hasImages) return CreateText(body!);
        if (hasImages && hasBody)
            return new ReviewContent(ReviewContentType.RichText, body?.Trim(), images);

        throw new ArgumentException("Content must have either body text or images.");
    }

    

    /// <summary>验证图片格式是否符合允许的</summary>
    private static void ValidateImageFormats(List<VideoImage> images, ReviewContentOptions opt)
    {
        var allowed = opt.GetImageExtensionSet();
        foreach (var img in images)
        {
            var ext = Path.GetExtension(img.ImageUrl.AbsolutePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(ext) && !allowed.Contains(ext))
                throw new ArgumentException(
                    $"Unsupported image format: '{ext}'. Allowed: {string.Join(", ", allowed)}.");
        }
    }

    /// <summary>验证视频格式是否符合允许的</summary>
    private static void ValidateVideoFormats(List<VideoImage> videos, ReviewContentOptions opt)
    {
        var allowed = opt.GetVideoExtensionSet();
        foreach (var vid in videos)
        {
            var ext = Path.GetExtension(vid.ImageUrl.AbsolutePath) ?? string.Empty;
            if (!allowed.Contains(ext))
                throw new ArgumentException(
                    $"Unsupported video format: '{ext}'. Allowed: {string.Join(", ", allowed)}.");
        }
    }

    /// <summary>简单的富文本安全清洗（防 XSS 基础级别）</summary>
    private static string SanitizeRichText(string richText, ReviewContentOptions opt)
    {
        var allowedTags = opt.GetHtmlTagSet();

        var result = System.Text.RegularExpressions.Regex.Replace(
            richText,
            @"<(?!/?(" + string.Join('|', allowedTags) + @")\b)[^>]+>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"\s+on\w+\s*=\s*[""'][^""']*[""']",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return result;
    }

    // ── 内部工具 ──

    private static ReviewContentOptions GetOptions() => Options ?? ReviewContentOptions.Default;

    // ── 类型判定 ──

    public bool IsText => ContentType == ReviewContentType.Text;
    public bool IsImage => ContentType == ReviewContentType.Image;
    public bool IsVideo => ContentType == ReviewContentType.Video;
    public bool IsRichText => ReviewContentType.RichText == ContentType;
    public bool HasMedia => MediaItems.Count > 0;

    // ── ValueObject 等价性 ──

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ContentType;
        yield return Body ?? string.Empty;
        yield return MediaItems.Count;
    }

    public override string ToString()
    {
        return $"{ContentType}: {(Body?.Length > 100 ? Body[..100] + "..." : Body ?? "(empty)")} " +
               $"({MediaItems.Count} media)";
    }
}
