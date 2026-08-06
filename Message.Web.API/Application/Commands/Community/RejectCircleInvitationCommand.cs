namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 拒绝直邀命令。
/// </summary>
public record RejectCircleInvitationCommand(Guid UserId, Guid InviteGuid) : IRequest<bool>;
