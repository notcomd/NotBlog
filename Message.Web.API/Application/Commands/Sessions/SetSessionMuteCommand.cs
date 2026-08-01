namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 静音/取消静音会话命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="Mute">true 静音，false 取消静音</param>
public record SetSessionMuteCommand(Guid SessionId, bool Mute) : IRequest<bool>;

/// <summary>
/// 静音/取消静音会话命令处理程序。
/// </summary>
public class SetSessionMuteCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<SetSessionMuteCommandHandler> logger) : IRequestHandler<SetSessionMuteCommand, bool>
{
    public async Task<bool> Handler(SetSessionMuteCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        if (command.Mute)
            session.Mute();
        else
            session.Unmute();

        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("会话 {SessionId} 静音状态已设置为 {Mute}", command.SessionId, command.Mute);
        return true;
    }
}
