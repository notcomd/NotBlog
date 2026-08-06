
namespace Message.Web.API.Application.Commands.Sessions;
/// <summary>
/// 解散会话命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
public record DismissSessionCommand(Guid SessionId) : IRequest<bool>;

