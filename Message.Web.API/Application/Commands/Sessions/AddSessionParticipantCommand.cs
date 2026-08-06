
namespace Message.Web.API.Application.Commands.Sessions;
/// <summary>
/// 向会话添加参与者命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="UserId">要添加的用户 ID</param>
public record AddSessionParticipantCommand(Guid SessionId, Guid UserId) : IRequest<bool>;

