namespace Message.Web.API.Dto.Request;
public class AddGroupMemberRequest
{
    public Guid UserId { get; init; }
    public GroupMemberRole Role { get; init; } = GroupMemberRole.Member;
}

