namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 移出圈子成员命令（圈主/管理员）。
/// </summary>
public record RemoveCircleMemberCommand(Guid OperatorGuid, Guid CircleGuid, Guid UserGuid) : IRequest<bool>;
