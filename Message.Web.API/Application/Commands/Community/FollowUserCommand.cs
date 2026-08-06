namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 关注用户命令。
/// </summary>
public record FollowUserCommand(Guid FollowerGuid, Guid FolloweeGuid) : IRequest<bool>;
