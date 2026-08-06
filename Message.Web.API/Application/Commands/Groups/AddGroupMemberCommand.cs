namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 添加群组成员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">要添加的用户 ID</param>
/// <param name="Role">成员角色</param>
public record AddGroupMemberCommand(Guid GroupId, Guid UserId, GroupMemberRole Role) : IRequest<bool>;

