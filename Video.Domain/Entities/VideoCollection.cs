using Notcomd.Token.JWT.Security;
using Video.Domain.SeedWork;
using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

public class VideoCollection : Entity, IAggregateRoot
{
    private VideoCollection()
    {
        VideoGuid = new();
        AffiliatedUser = new();
        TimeSpace = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        VideoControl = VideoControl.VideoControlBuilder();
    }

    public Guid VideoCollectionGuid { get; init; } = Guid.CreateVersion7();

    public string VideoNvid { get; init; } = NvidGenerator.GenerateNvStyleIdWithUuid();
    public List<Guid> AffiliatedUser { get; }

    public string VideoCollectionName { get; private set; }

    public string VideoCollectionBriefIntroduction { get; private set; }

    public List<Guid> VideoGuid { get; }

    public TimeSpace TimeSpace { get; }

    public VideoControl VideoControl { get; private set; }

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