namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 解除群组成员封禁命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">被解除封禁的用户 ID</param>
public record UnbanGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest<bool>;

