namespace Message.Web.API.Dto.Response;
public class FriendDto
{
    public Guid FriendshipId { get; init; }
    public Guid FriendId { get; init; }
    public string? FriendName { get; init; }
    public string? FriendAvatar { get; init; }
    public FriendshipStatus Status { get; init; }
    public string? Remark { get; init; }
    public string? FriendGroupName { get; init; }
    public bool IsBlocked { get; init; }
    public bool IsMuted { get; init; }
    public bool IsStarred { get; init; }
    public DateTime CreatedTime { get; init; }
    public DateTime? LastInteractionTime { get; init; }
}

