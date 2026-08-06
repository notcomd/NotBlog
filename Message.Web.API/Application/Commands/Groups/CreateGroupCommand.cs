namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 创建群组命令。
/// <para>CQRS 命令侧：仅返回新群组的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="UserId">群主（创建者）用户 ID</param>
/// <param name="GroupName">群名称</param>
/// <param name="MaxMembers">最大成员数</param>
/// <param name="IsPublic">是否公开群组</param>
/// <param name="InitialMembers">初始成员集合（可选）</param>
public record CreateGroupCommand(
    Guid UserId,
    string GroupName,
    int MaxMembers,
    bool IsPublic,
    HashSet<Guid>? InitialMembers) : IRequest<Guid>;

