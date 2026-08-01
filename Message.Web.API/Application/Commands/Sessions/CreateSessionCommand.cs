namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 创建会话命令（私聊或群聊）。
/// <para>CQRS 命令侧：仅返回新会话的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="UserId">发起创建的用户 ID</param>
/// <param name="SessionType">会话类型（私聊/群聊）</param>
/// <param name="FriendId">私聊对象用户 ID（私聊必填）</param>
/// <param name="GroupId">群聊关联的群组 ID（群聊必填）</param>
/// <param name="SessionName">会话名称（群聊时可为空则使用群组名）</param>
/// <param name="InitialMembers">初始成员集合</param>
public record CreateSessionCommand(
    Guid UserId,
    SessionType SessionType,
    Guid? FriendId,
    Guid? GroupId,
    string? SessionName,
    HashSet<Guid>? InitialMembers) : IRequest<Guid>;

/// <summary>
/// 创建会话命令处理程序。
/// </summary>
public class CreateSessionCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<CreateSessionCommandHandler> logger) : IRequestHandler<CreateSessionCommand, Guid>
{
    public async Task<Guid> Handler(CreateSessionCommand command, CancellationToken cancellationToken)
    {
        ChatSession session;
        if (command.SessionType == SessionType.Private)
        {
            var existing = await sessionRepository.GetPrivateSessionAsync(command.UserId, command.FriendId!.Value);
            if (existing != null)
            {
                logger.LogInformation("私聊会话已存在：{SessionId}", existing.SessionId);
                return existing.SessionId;
            }

            session = ChatSession.CreatePrivateSession(command.UserId, command.FriendId!.Value);
        }
        else
        {
            session = ChatSession.CreateGroupSession(
                command.GroupId!.Value,
                command.UserId,
                command.SessionName!,
                command.InitialMembers ?? new HashSet<Guid>());
        }

        await sessionRepository.AddAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("创建会话成功：{SessionId}，类型={SessionType}",
            session.SessionId, session.SessionType);
        return session.SessionId;
    }
}
