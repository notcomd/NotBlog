namespace Video.Domain.Entities;

public class VideoBarrage
{
    private VideoBarrage()
    {
    }

    public Guid VideoBarrageGuid { get; init; } = Guid.NewGuid();

    public Guid VideoGuid { get; init; }

    public Guid UserGuid { get; init; }

    public string? VideoBarrageBody { get; init; }

    public TimeSpace TimeSpace { get; private set; } = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    public VideoControl VideoControl { get; private set; } = VideoControl.VideoControlBuilder();

    public static VideoBarrage CreateVideoBarrage(Guid videoGuid, Guid userGuid, string? videoBarrageBody)
    {
        return new VideoBarrage
        {
            VideoGuid = videoGuid,
            UserGuid = userGuid,
            VideoBarrageBody = videoBarrageBody
        };
    }
}