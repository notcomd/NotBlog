
namespace Video.Domain.Entities;

/// <summary>
/// 视频收藏
/// </summary>
public class VideoCollection : Entity<int>, IAggregateRoot
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




    public void AddByBelongs(List<Guid> videoBelongs)
    {
        AffiliatedUser.AddRange(videoBelongs);
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    public void AddByVideoCollection(List<Guid> videoCollection)
    {
        VideoGuid.AddRange(videoCollection);
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    public void ResetByVideoCollectionName(string CollectionName)
    {
        VideoCollectionName = CollectionName;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }
}