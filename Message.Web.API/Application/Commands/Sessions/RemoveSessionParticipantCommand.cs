
namespace Message.Web.API.Application.Commands.Sessions;
/// <summary>
/// 从会话移除参与者命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="UserId">要移除的用户 ID</param>
public record RemoveSessionParticipantCommand(Guid SessionId, Guid UserId) : IRequest<bool>;

