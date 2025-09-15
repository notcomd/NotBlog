using Notcomd.Token.JWT;

namespace Video.Domain.Entities;

/// <summary>
///     视频合集
/// </summary>
public class VideoCollection : IAggregateRoot
{

    

    public Guid VideoCollectionGuid { get; init; } = Guid.CreateVersion7();

    public string VideoNvid { get; init; } = NVIDGenerator.GenerateNvStyleIdWithUuid();
    public List<Guid> AffiliatedUser { get; } = new();

    public string VideoCollectionName { get; private set; }

    public string VideoCollectionBriefIntroduction { get; private set; }

    public List<Guid> VideoGuid { get; } = new();

    public TimeSpace TimeSpace { get; } = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    public VideoControl VideoControl { get; private set; } = VideoControl.VideoControlBuilder();

    public VideoQuote VideoQuote { get; private set; }



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