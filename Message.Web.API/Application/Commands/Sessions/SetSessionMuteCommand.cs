
namespace Message.Web.API.Application.Commands.Sessions;
/// <summary>
/// 静音/取消静音会话命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="Mute">true 静音，false 取消静音</param>
public record SetSessionMuteCommand(Guid SessionId, bool Mute) : IRequest<bool>;

