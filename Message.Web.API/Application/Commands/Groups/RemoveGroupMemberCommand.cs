namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 移除群组成员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">要移除的用户 ID</param>
public record RemoveGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest<bool>;

