namespace Message.Web.API.Dto.Response;
public class SessionDto
{
    public Guid SessionId { get; init; }
    public SessionType SessionType { get; init; }
    public string? SessionName { get; init; }
    public Guid? GroupId { get; init; }
    public Guid? CircleId { get; init; }
    public Guid CreatorId { get; init; }
    public List<Guid> Participants { get; init; } = new();
    public Guid? LastMessageId { get; init; }
    public string? LastMessageContent { get; init; }
    public DateTime? LastMessageTime { get; init; }
    public int UnreadCount { get; init; }
    public DateTime CreatedTime { get; init; }
    public bool IsPinned { get; init; }
    public bool IsMuted { get; init; }
}

