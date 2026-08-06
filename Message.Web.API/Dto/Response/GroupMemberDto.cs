namespace Message.Web.API.Dto.Response;
public class GroupMemberDto
{
    public Guid MemberId { get; init; }
    public Guid GroupId { get; init; }
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserAvatar { get; init; }
    public GroupMemberRole Role { get; init; }
    public string? Nickname { get; init; }
    public DateTime JoinTime { get; init; }
    public bool IsMuted { get; init; }
    public bool IsBanned { get; init; }
}

