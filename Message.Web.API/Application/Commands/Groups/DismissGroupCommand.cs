namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 解散群组命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
public record DismissGroupCommand(Guid GroupId) : IRequest<bool>;

