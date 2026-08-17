
namespace Video.Domain.Entities;

/// <summary>
/// 视频弹幕实体 — 支持文本、图片及混合内容。
///
/// 设计要点：
/// - BarrageType 枚举区分纯文本/纯图片/混合三种模式
/// - VideoImages 集合支持最多 4 张图片，适用于表情包、贴图等场景
/// - 内置内容验证：至少提供文本或图片之一
/// - DisplayUrl 优先使用缩略图，优化弹幕渲染性能
/// - 保留 VideoBarrageBody 属性确保与现有文本弹幕系统完全兼容
/// </summary>
public class VideoBarrage
{
  
    public Guid VideoBarrageGuid { get; init; }

    public Guid VideoGuid { get; init; }

    public Guid UserGuid { get; init; }

    /// <summary>弹幕内容类型</summary>
    public BarrageType BarrageType { get; private set; }

    /// <summary>文本内容（纯文本/混合模式下有效）</summary>
    public string? VideoBarrageBody { get; private set; }

    /// <summary>图片列表（纯图片/混合模式下有效），最多 4 张</summary>
    public IReadOnlyCollection<VideoImage>? VideoImages { get; private set; }

    public TimeSpace TimeSpace { get; private set; }

    public bool IsDelete { get; private set; }

    public VideoControl VideoControl { get; private set; }

    public VideoQuote VideoQuote { get; private set; }

    // ── 便捷属性 ──

    /// <summary>是否包含文本内容</summary>
    public bool HasText => !string.IsNullOrWhiteSpace(VideoBarrageBody);

    /// <summary>是否包含图片内容</summary>
    public bool HasImages => VideoImages is { Count: > 0 };

    /// <summary>图片数量</summary>
    public int ImageCount => VideoImages?.Count ?? 0;

    /// <summary>获取用于前端展示的主要图片URL（取第一张图片的展示URL）</summary>
    public Uri? PrimaryImageUrl => VideoImages?.FirstOrDefault()?.DisplayUrl;

    // ── 构造器 ──

    private VideoBarrage()
    {
        VideoBarrageGuid = Guid.CreateVersion7();
        TimeSpace = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        VideoControl = VideoControl.VideoControlBuilder();
        VideoQuote = VideoQuote.VideoQuoteBuilder();
        IsDelete = false;
    }

    /// <summary>
    /// [向后兼容] 纯文本弹幕构造器。
    /// </summary>
    public VideoBarrage(Guid userGuid, string videoBarrageBody) : this()
    {
        UserGuid = userGuid;
        VideoBarrageBody = videoBarrageBody ?? throw new ArgumentNullException(nameof(videoBarrageBody));
        if (string.IsNullOrWhiteSpace(videoBarrageBody))
            throw new ArgumentException("弹幕文本内容不能为空", nameof(videoBarrageBody));
        BarrageType = BarrageType.Text;
    }

    /// <summary>
    /// 纯图片弹幕构造器。
    /// </summary>
    public VideoBarrage(Guid userGuid, List<VideoImage> videoImages) : this()
    {
        UserGuid = userGuid;
        SetImages(videoImages);
        BarrageType = BarrageType.Image;
    }

    /// <summary>
    /// 文本+图片混合弹幕构造器。
    /// </summary>
    public VideoBarrage(Guid userGuid, string videoBarrageBody, List<VideoImage> videoImages) : this()
    {
        UserGuid = userGuid;
        VideoBarrageBody = videoBarrageBody ?? throw new ArgumentNullException(nameof(videoBarrageBody));
        SetImages(videoImages);
        BarrageType = string.IsNullOrWhiteSpace(videoBarrageBody) ? BarrageType.Image : BarrageType.Mixed;
    }

    // ── 工厂方法 ──

    /// <summary>创建纯文本弹幕</summary>
    public static VideoBarrage CreateText(Guid userGuid, string body)
        => new(userGuid, body);

    /// <summary>创建纯图片弹幕</summary>
    public static VideoBarrage CreateImage(Guid userGuid, List<VideoImage> images)
        => new(userGuid, images);

    /// <summary>创建文本+图片混合弹幕</summary>
    public static VideoBarrage CreateMixed(Guid userGuid, string body, List<VideoImage> images)
        => new(userGuid, body, images);

    // ── 行为方法 ──

    public void ChangeByVideoControl(VideoControl videoControl, bool isDelete)
    {
        VideoControl.ChangeByVideoController(videoControl);
        IsDelete = isDelete;
    }

    /// <summary>更新文本内容（仅对文本/混合类型有效）</summary>
    public void UpdateBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("弹幕文本内容不能为空", nameof(body));
        VideoBarrageBody = body;
        if (BarrageType == BarrageType.Image)
            BarrageType = BarrageType.Mixed;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    /// <summary>替换图片列表（仅对图片/混合类型有效）</summary>
    public void UpdateImages(List<VideoImage> images)
    {
        SetImages(images);
        if (BarrageType == BarrageType.Text)
            BarrageType = BarrageType.Mixed;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }


    public void SoftDelete()=>this.IsDelete=true;
  
    // ── 内部方法 ──

    private void SetImages(List<VideoImage>? images)
    {
        if (images is null || images.Count == 0)
            throw new ArgumentException("弹幕图片列表不能为空", nameof(images));
        VideoImages = images.OrderBy(i => i.SortOrder).ToList().AsReadOnly();
    }
}
