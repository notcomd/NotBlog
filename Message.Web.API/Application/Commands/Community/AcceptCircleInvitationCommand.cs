namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 接受直邀加入圈子命令。
/// </summary>
public record AcceptCircleInvitationCommand(Guid UserId, Guid InviteGuid) : IRequest<Guid>;
