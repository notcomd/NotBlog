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

