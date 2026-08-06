namespace Message.Web.API.Dto.Request;
public class SendFriendRequestRequest
{
    public Guid FriendId { get; init; }
    public string? Message { get; init; }
}

