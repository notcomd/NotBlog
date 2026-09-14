namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 成员离开圈子命令：OperatorGuid 与 UserGuid 相同表示成员主动退出，否则为圈主/管理员移出成员。
/// </summary>
public record RemoveCircleMemberCommand(Guid OperatorGuid, Guid CircleGuid, Guid UserGuid) : IRequest<bool>;
