

namespace Video.Domain.ValueObjects;

public record VideoControl
{
    private VideoControl()
    {
        VideoDelete = false;
        VideoDisplay = false;
        AuthorVideo = AuthorVideo.VideoPublic;
        BarrageControl = BarrageControl.BarrageOn;
        TimeSpace = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        VideoProtectedTime = VideoProtectedTime.Create(null, null);
    }
    
    public static VideoControl VideoControlBuilder()
    {
        return new VideoControl();
    }

    public bool VideoDelete { get; private set; }

    public bool VideoDisplay { get; private set; }

    public AuthorVideo AuthorVideo { get; private set; }

    public BarrageControl BarrageControl { get; private set; }

    public VideoProtectedTime? VideoProtectedTime { get; private set; }

    public TimeSpace TimeSpace { get; private set; }

    
    public void ChangeByVideoController(VideoControl videoControl)
    {
        VideoDelete = videoControl.VideoDelete;
        VideoDisplay = videoControl.VideoDisplay;
        AuthorVideo = videoControl.AuthorVideo;
        BarrageControl = videoControl.BarrageControl;
        if (videoControl.VideoProtectedTime is not null)
            VideoProtectedTime = videoControl.VideoProtectedTime;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }
    
    

    public void Barrage(BarrageControl barrageControl)
    {
        BarrageControl = barrageControl;
    }

    public void Delete(bool delete)
    {
        VideoDelete = delete;
    }

    public void Push()
    {
        if(!IsVideoDisplay())
            VideoDisplay = true;
    }

    public void Author(AuthorVideo author)
    {
        AuthorVideo = author;
    }

    public void SetProtectedTime(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        VideoProtectedTime = VideoProtectedTime.Create(startTime, endTime);
    }


    public bool IsVideoDelete()
    {
        return VideoDelete;
    }

    public bool IsVideoDisplay()
    {
        return VideoDisplay;
    }

}