namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 设置/取消圈子管理员命令（仅圈主）。role = admin | member
/// </summary>
public record SetCircleMemberRoleCommand(Guid OperatorGuid, Guid CircleGuid, Guid UserGuid, string Role) : IRequest<bool>;
