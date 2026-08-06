namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 封禁群组成员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">被封禁的用户 ID</param>
public record BanGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest<bool>;

