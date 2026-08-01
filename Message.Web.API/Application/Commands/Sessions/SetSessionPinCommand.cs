namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 置顶/取消置顶会话命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="Pin">true 置顶，false 取消置顶</param>
public record SetSessionPinCommand(Guid SessionId, bool Pin) : IRequest<bool>;

/// <summary>
/// 置顶/取消置顶会话命令处理程序。
/// </summary>
public class SetSessionPinCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<SetSessionPinCommandHandler> logger) : IRequestHandler<SetSessionPinCommand, bool>
{
    public async Task<bool> Handler(SetSessionPinCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        if (command.Pin)
            session.Pin();
        else
            session.Unpin();

        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("会话 {SessionId} 置顶状态已设置为 {Pin}", command.SessionId, command.Pin);
        return true;
    }
}
