namespace Video.Domain.Entities;

public record VideoControl
{

    private VideoControl() {}

    public bool VideoDelete { get; private set; }

    public bool VideoDisplay { get; private set; }

    public AuthorVideo AuthorVideo { get; private set; }

    public BarrageControl BarrageControl { get; private set; }

    public VideoProtectedTime? VideoProtectedTime { get; private set; } = VideoProtectedTime.Crate(null, null);

    public TimeSpace TimeSpace { get; private set; } = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);


    public static VideoControl VideoControlBuilder()
    {
        return new VideoControl
        {
            VideoDelete = false,
            VideoDisplay = true,
            AuthorVideo = AuthorVideo.VideoPublic,
        };
    }

    public void Barrage(BarrageControl barrageControl) => BarrageControl = barrageControl;

    public void Delete(bool delete) => VideoDelete = delete;

    public void Display(bool display) => VideoDisplay = display;

    public void Author(AuthorVideo author) => AuthorVideo = author;

    public void SetProtectedTime(DateTimeOffset startTime, DateTimeOffset endTime) => VideoProtectedTime = VideoProtectedTime.Crate(startTime, endTime);


    public bool IsVideoDelete()
    {
        return VideoDelete;
    }

    public bool IsVideoDisplay()
    {
        return VideoDisplay;
    }

    public AuthorVideo GetAuthorVideo()
    {
        return AuthorVideo;
    }
}