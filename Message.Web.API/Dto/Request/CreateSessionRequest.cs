namespace Message.Web.API.Dto.Request;
public class CreateSessionRequest
{
    public SessionType SessionType { get; init; }
    public Guid? FriendId { get; init; }
    public Guid? GroupId { get; init; }
    public string? SessionName { get; init; }
    public HashSet<Guid>? InitialMembers { get; init; }
}

