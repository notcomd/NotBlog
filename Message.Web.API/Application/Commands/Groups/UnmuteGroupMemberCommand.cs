namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 解除群组成员禁言命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">被解除禁言的用户 ID</param>
public record UnmuteGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest<bool>;

