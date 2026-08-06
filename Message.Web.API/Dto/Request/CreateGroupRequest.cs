namespace Message.Web.API.Dto.Request;
public class CreateGroupRequest
{
    public string GroupName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int MaxMembers { get; init; } = 500;
    public bool IsPublic { get; init; }
    public HashSet<Guid>? InitialMembers { get; init; }
}

