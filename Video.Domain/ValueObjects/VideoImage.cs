namespace Video.Domain.ValueObjects;

/// <summary>
/// 视频图片值对象 — 适用于弹幕、评论等场景。
/// 包含图片URL、尺寸、格式信息，支持缩略图优化加载策略。
/// </summary>
public record VideoImage
{
    /// <summary>原始图片URL（必填）</summary>
    public Uri ImageUrl { get; } = null!;

    /// <summary>缩略图URL，用于列表/弹幕等轻量展示场景（可选）</summary>
    public Uri? ThumbnailUrl { get; }

    /// <summary>图片宽度（像素），可为空</summary>
    public int? Width { get; }

    /// <summary>图片高度（像素），可为空</summary>
    public int? Height { get; }

    /// <summary>图片格式，如 "png" "jpg" "webp" "gif"</summary>
    public string? Format { get; }

    /// <summary>排序序号，用于多图场景下的展示顺序</summary>
    public int SortOrder { get; }

    /// <summary>图片描述/alt文本（可选）</summary>
    public string? Description { get; }

    /// <summary>图片文件大小（字节），用于性能预算控制</summary>
    public long? FileSize { get; }

    // ── 常量 ──
    public const int MaxWidth = 3840;
    public const int MaxHeight = 2160;
    public const long MaxFileSize = 10 * 1024 * 1024; // 10MB per image for barrage
    public static readonly HashSet<string> SupportedFormats =
        new(StringComparer.OrdinalIgnoreCase) { "png", "jpg", "jpeg", "webp", "gif", "bmp" };

    private VideoImage()
    {
        SortOrder = 0;
    }

    public VideoImage(Uri imageUrl, int sortOrder = 0, string? description = null,
        int? width = null, int? height = null, string? format = null,
        long? fileSize = null, Uri? thumbnailUrl = null)
    {
        ImageUrl = imageUrl ?? throw new ArgumentException("图片URL不能为空", nameof(imageUrl));

        if (width.HasValue && (width.Value <= 0 || width.Value > MaxWidth))
            throw new ArgumentOutOfRangeException(nameof(width), $"图片宽度必须在 1-{MaxWidth} 之间");
        if (height.HasValue && (height.Value <= 0 || height.Value > MaxHeight))
            throw new ArgumentOutOfRangeException(nameof(height), $"图片高度必须在 1-{MaxHeight} 之间");
        if (fileSize.HasValue && fileSize.Value > MaxFileSize)
            throw new ArgumentOutOfRangeException(nameof(fileSize), $"弹幕图片大小不能超过 {MaxFileSize / 1024 / 1024}MB");
        if (format is not null && !SupportedFormats.Contains(format))
            throw new ArgumentException($"不支持的图片格式: '{format}'，支持的格式: {string.Join(", ", SupportedFormats)}", nameof(format));

        Width = width;
        Height = height;
        Format = format;
        FileSize = fileSize;
        SortOrder = sortOrder;
        Description = description;
        ThumbnailUrl = thumbnailUrl;
    }

    // ── 工厂方法 ──

    /// <summary>创建构建器占位实例（EF Core 内部使用）</summary>
    public static VideoImage VideoImageBuilder() => new();

    /// <summary>创建仅带有URL的基础图片（向后兼容）</summary>
    public static VideoImage FromUrl(Uri imageUrl, string? description = null)
        => new(imageUrl, description: description);

    /// <summary>创建带完整元数据的图片</summary>
    public static VideoImage FromFull(Uri imageUrl, int width, int height, string format,
        long? fileSize = null, Uri? thumbnailUrl = null, string? description = null, int sortOrder = 0)
        => new(imageUrl, sortOrder, description, width, height, format, fileSize, thumbnailUrl);

    // ── 便捷属性 ──

    /// <summary>获取用于展示的URL（优先使用缩略图）</summary>
    public Uri DisplayUrl => ThumbnailUrl ?? ImageUrl;

    /// <summary>是否包含完整元数据</summary>
    public bool HasMetadata => Width.HasValue && Height.HasValue && Format is not null;

    /// <summary>获取宽高比（当宽高均已知时）</summary>
    public double? AspectRatio => Width.HasValue && Height.HasValue && Height.Value != 0
        ? (double)Width.Value / Height.Value
        : null;

    /// <summary>以可读字符串返回尺寸</summary>
    public string DimensionString => HasMetadata ? $"{Width}x{Height}" : "未知尺寸";
}