namespace Message.Web.API.Dto.Response;
public class GroupDto
{
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid OwnerId { get; init; }
    public int MaxMembers { get; init; }
    public int MemberCount { get; init; }
    public bool IsPublic { get; init; }
    public DateTime CreatedTime { get; init; }
    public bool IsDismissed { get; init; }
}

