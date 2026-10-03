namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 媒体消息内容抽象基类（图片/视频/音频，不可变）。
/// <para>承载媒体 URI 与可选元数据；具体媒体类型由子类判别，见 <see cref="ImageContent"/>、<see cref="VideoContent"/>、<see cref="AudioContent"/>。</para>
/// </summary>
public abstract class MediaContent : MessageContent
{
    private protected MediaContent(Uri mediaUri, string? thumbnailUri = null, string? caption = null,
        double? duration = null)
    {
        MediaUri = mediaUri ?? throw new ArgumentNullException(nameof(mediaUri));
        ThumbnailUri = thumbnailUri;
        Caption = caption;
        Duration = duration;
    }

    /// <summary>媒体 URI</summary>
    public Uri MediaUri { get; }

    /// <summary>缩略图 URI（可选）</summary>
    public string? ThumbnailUri { get; }

    /// <summary>媒体描述（可选）</summary>
    public string? Caption { get; }

    /// <summary>媒体时长（秒，图片为 null）</summary>
    public double? Duration { get; }

    protected sealed override IEnumerable<object> GetAtomicValues()
    {
        yield return MediaUri;
        yield return ThumbnailUri ?? string.Empty;
        yield return Caption ?? string.Empty;
        yield return Duration ?? 0;
    }
}

/// <summary>
/// 图片消息内容值对象。 <see cref="MediaContent"/> 的图片特化，时长恒为 null。
/// </summary>
public class ImageContent : MediaContent
{
    private ImageContent(Uri mediaUri, string? thumbnailUri = null, string? caption = null)
        : base(mediaUri, thumbnailUri, caption, null)
    {
    }

    /// <summary>内容业务类型，恒为图片消息。</summary>
    public override MessageType MessageType => MessageType.MessageImage;

    /// <summary>创建图片内容值对象（无时长）。</summary>
    public static ImageContent Create(Uri mediaUri, string? thumbnailUri = null, string? caption = null)
        => new(mediaUri, thumbnailUri, caption);

    /// <summary>生成会话侧栏摘要，固定返回「[图片]」。</summary>
    public override string ToSessionSummary() => "[图片]";
}

/// <summary>
/// 视频消息内容值对象。 <see cref="MediaContent"/> 的视频特化。
/// </summary>
public class VideoContent : MediaContent
{
    private VideoContent(Uri mediaUri, double duration, string? thumbnailUri = null, string? caption = null)
        : base(mediaUri, thumbnailUri, caption, duration)
    {
    }

    /// <summary>内容业务类型，恒为视频消息。</summary>
    public override MessageType MessageType => MessageType.MessageVideo;

    /// <summary>创建视频内容值对象（需指定时长，单位秒）。</summary>
    public static VideoContent Create(Uri mediaUri, double duration, string? thumbnailUri = null, string? caption = null)
        => new(mediaUri, duration, thumbnailUri, caption);

    /// <summary>生成会话侧栏摘要，固定返回「[视频]」。</summary>
    public override string ToSessionSummary() => "[视频]";
}

/// <summary>
/// 音频消息内容值对象。 <see cref="MediaContent"/> 的音频特化。
/// </summary>
public class AudioContent : MediaContent
{
    private AudioContent(Uri mediaUri, double duration, string? caption = null)
        : base(mediaUri, null, caption, duration)
    {
    }

    /// <summary>内容业务类型，恒为音频消息。</summary>
    public override MessageType MessageType => MessageType.MessageAudio;

    /// <summary>创建音频内容值对象（需指定时长，单位秒）。</summary>
    public static AudioContent Create(Uri mediaUri, double duration, string? caption = null)
        => new(mediaUri, duration, caption);

    /// <summary>生成会话侧栏摘要，固定返回「[音频]」。</summary>
    public override string ToSessionSummary() => "[音频]";
}