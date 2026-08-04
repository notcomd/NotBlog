using Message.Domain.SeedWork;

namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 媒体（图片/视频/音频）消息内容值对象。
/// 不可变，封装媒体 URI 与可选元数据。
/// </summary>
public class MediaContent : ValueObject
{
    private MediaContent(Uri mediaUri, string? thumbnailUri = null, string? caption = null, double? duration = null)
    {
        MediaUri = mediaUri ?? throw new ArgumentNullException(nameof(mediaUri));
        ThumbnailUri = thumbnailUri;
        Caption = caption;
        Duration = duration;
    }

    public Uri MediaUri { get; }
    public string? ThumbnailUri { get; }
    public string? Caption { get; }
    public double? Duration { get; }

    public static MediaContent Create(Uri mediaUri, string? thumbnailUri = null, string? caption = null,
        double? duration = null)
        => new(mediaUri, thumbnailUri, caption, duration);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MediaUri;
        yield return ThumbnailUri ?? string.Empty;
        yield return Caption ?? string.Empty;
        yield return Duration ?? 0;
    }
}
