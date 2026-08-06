namespace Message.Web.API.Dto.Response;

public class CircleDto
{
    public Guid CircleGuid { get; init; }
    public Guid OwnerGuid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? AvatarUrl { get; init; }
    public int MemberCount { get; init; }
    public int MaxMembers { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreateTime { get; init; }
    /// <summary>当前用户角色（Owner/Admin/Member，非成员为空）</summary>
    public string? MyRole { get; init; }
    public bool IsMember { get; init; }
}

