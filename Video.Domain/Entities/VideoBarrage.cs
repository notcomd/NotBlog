using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

public class VideoBarrage
{
    private VideoBarrage()
    {
        VideoBarrageGuid = Guid.CreateVersion7();
        TimeSpace = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        VideoControl = VideoControl.VideoControlBuilder();
    }

    public Guid VideoBarrageGuid { get; init; }

    public Guid VideoGuid { get; init; }

    public Guid UserGuid { get; init; }

    public string VideoBarrageBody { get; init; }
    
    public TimeSpace TimeSpace { get; private set; }

    public required VideoControl VideoControl { get; init; }
    
    public VideoBarrage( Guid userGuid, string videoBarrageBody):this()
    {
        UserGuid = userGuid;
        VideoBarrageBody = videoBarrageBody ?? throw new ArgumentNullException(nameof(videoBarrageBody));
    }

    public void  ChangeByTime()
    {
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

}