
namespace Message.Domain.Entities.Tweet;

/// <summary>
/// 推文媒体
/// </summary>
public class TweetMedia : Entity<Guid>
{
    private TweetMedia()
    {
        MediaGuid = Guid.CreateVersion7();
        CreateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建推文媒体</summary>
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
    /// 设置文件大小（服务端经 FileDev 解析媒体元数据后填充）
    /// </summary>
    public void SetFileSize(long fileSize)
    {
        FileSize = fileSize;
    }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreateTime { get; init; }
}
