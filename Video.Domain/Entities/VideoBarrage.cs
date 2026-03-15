using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

public class VideoBarrage
{
    private VideoBarrage()
    {
        VideoBarrageGuid = Guid.CreateVersion7();
        TimeSpace = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        VideoControl = VideoControl.VideoControlBuilder();
        VideoImage = VideoImage.VideoImageBuilder();
        VideoQuote = VideoQuote.VideoQuoteBuilder();
        IsDelete = false;
    }

    public Guid VideoBarrageGuid { get; init; }

    public Guid VideoGuid { get; init; }

    public Guid UserGuid { get; init; }

    public string VideoBarrageBody { get; init; }

    public TimeSpace TimeSpace { get; private set; }

    public bool IsDelete { get; private set; }

    public VideoControl VideoControl { get; private set; }

    public VideoQuote VideoQuote { get; private set; }

    public VideoImage? VideoImage { get; init; }

    public VideoBarrage(Guid userGuid, string videoBarrageBody) : this()
    {
        UserGuid = userGuid;
        VideoBarrageBody = videoBarrageBody ?? throw new ArgumentNullException(nameof(videoBarrageBody));
    }

    public void ChangeByTime()
    {
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

   

    public void ChangeByVideoControl(VideoControl videoControl,bool isDelete)
    {
        VideoControl.ChangeByVideoController(videoControl);
        IsDelete = isDelete;
    }
}