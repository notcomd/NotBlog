namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 更新群组信息命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="GroupName">新的群名称</param>
/// <param name="Description">新的群描述（可为空）</param>
public record UpdateGroupInfoCommand(Guid GroupId, string GroupName, string? Description) : IRequest<bool>;

