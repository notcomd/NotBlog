namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 撤销圈子邀请命令（圈主/管理员；直邀被邀请人可拒绝）。
/// </summary>
public record RevokeCircleInvitationCommand(Guid OperatorGuid, Guid InviteGuid) : IRequest<bool>;
