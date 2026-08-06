
namespace Message.Web.API.Application.Commands.Sessions;
/// <summary>
/// 置顶/取消置顶会话命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="Pin">true 置顶，false 取消置顶</param>
public record SetSessionPinCommand(Guid SessionId, bool Pin) : IRequest<bool>;

