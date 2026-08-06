namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 取消关注用户命令。
/// </summary>
public record UnfollowUserCommand(Guid FollowerGuid, Guid FolloweeGuid) : IRequest<bool>;
