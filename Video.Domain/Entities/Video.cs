namespace Notcomd.Video.Server.Entities;

public class Video
{

    public Guid VideoGuid { get; init; } = Guid.NewGuid();

    public string VideoName { get; set; }

    public List<string> VideoTitle { get; set; }
}