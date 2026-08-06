namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 凭邀请码 / 邀请链接 token 加入圈子命令。
/// </summary>
public record JoinCircleCommand(Guid UserId, string? Code, Guid? Token) : IRequest<Guid>;
