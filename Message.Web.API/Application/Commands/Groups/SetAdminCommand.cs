namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 设置/取消群组成员管理员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">目标用户 ID</param>
/// <param name="IsAdmin">true 设为管理员，false 取消管理员</param>
public record SetAdminCommand(Guid GroupId, Guid UserId, bool IsAdmin) : IRequest<bool>;

