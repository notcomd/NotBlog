namespace Message.Web.API.Dto.Response;
public class OnlineStatusDto
{
    public Guid UserId { get; init; }
    public bool IsOnline { get; init; }
    public DateTime? LastOnlineTime { get; init; }
}