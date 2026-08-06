namespace Message.Web.API.Dto.Response;
public class FriendRequestDto
{
    public Guid FriendshipId { get; init; }
    public Guid RequesterId { get; init; }
    public string? RequesterName { get; init; }
    public string? RequesterAvatar { get; init; }
    public FriendshipStatus Status { get; init; }
    public DateTime CreatedTime { get; init; }
}

