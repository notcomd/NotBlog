using Message.Domain.SeedWork;

namespace Message.Domain.Entities.Tweet;

/// <summary>
/// 推文媒体
/// </summary>
public class TweetMedia : Entity
{
    private TweetMedia()
    {
        MediaGuid = Guid.CreateVersion7();
        CreateTime = DateTimeOffset.UtcNow;
    }

    public TweetMedia(string mediaUrl, string mediaType, int sortOrder = 0) : this()
    {
        MediaUrl = mediaUrl ?? throw new ArgumentNullException(nameof(mediaUrl));
        MediaType = mediaType ?? throw new ArgumentNullException(nameof(mediaType));
        SortOrder = sortOrder;
    }

    /// <summary>
    /// 媒体GUID
    /// </summary>
    public Guid MediaGuid { get; init; }
    /// <summary>
    /// 媒体类型
    /// </summary>
    public string MediaType { get; private set; } = null!;
    /// <summary>
    /// 媒体URL
    /// </summary>
    public string MediaUrl { get; private set; } = null!;
    /// <summary>
    /// 缩略图URL
    /// </summary>
    public string? ThumbnailUrl { get; private set; }
    /// <summary>
    /// 文件大小
    /// </summary>
    public long? FileSize { get; private set; }
    /// <summary>
    /// 宽度
    /// </summary>
    public int? Width { get; private set; }
    /// <summary>
    /// 高度
    /// </summary>
    /// <summary>
    /// 时长
    /// </summary>
    public int? Duration { get; private set; }
    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; private set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreateTime { get; init; }
}
