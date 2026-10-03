
namespace Video.Domain.Entities;

/// <summary>
/// 视频收藏
/// </summary>
public class VideoCollection : Entity<Guid>, IAggregateRoot
{

    public Guid VideoCollectionGuid { get; init; } = Guid.CreateVersion7();

    public string VideoNvid { get; init; } = NvidGenerator.GenerateNvStyleIdWithUuid();
    
    public List<Guid> AffiliatedUser { get; }

    public string VideoCollectionName { get; private set; } = null!;

    public string VideoCollectionBriefIntroduction { get; private set; } = null!;

    public List<Guid> VideoGuid { get; }

    public TimeSpace TimeSpace { get; }

    public VideoControl VideoControl { get; private set; }

    public VideoQuote VideoQuote { get; private set; }
    private VideoCollection()
    {
        VideoGuid = new();
        AffiliatedUser = new();
        TimeSpace = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        VideoControl = VideoControl.VideoControlBuilder();
        VideoQuote = VideoQuote.VideoQuoteBuilder();
    }




    /// <summary>添加归属用户列表并刷新更新时间。</summary>
    public void AddByBelongs(List<Guid> videoBelongs)
    {
        AffiliatedUser.AddRange(videoBelongs);
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    /// <summary>向收藏夹追加视频列表并刷新更新时间。</summary>
    public void AddByVideoCollection(List<Guid> videoCollection)
    {
        VideoGuid.AddRange(videoCollection);
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    /// <summary>重命名收藏夹并刷新更新时间。</summary>
    public void ResetByVideoCollectionName(string CollectionName)
    {
        VideoCollectionName = CollectionName;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }
}