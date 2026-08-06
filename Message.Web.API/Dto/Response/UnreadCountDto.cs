namespace Message.Web.API.Dto.Response;
public class UnreadCountDto
{
    public Guid SessionId { get; init; }
    public int UnreadCount { get; init; }
}

