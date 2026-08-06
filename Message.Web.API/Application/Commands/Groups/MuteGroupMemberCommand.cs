namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 禁言群组成员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">被禁言的用户 ID</param>
/// <param name="DurationMinutes">禁言时长（分钟）</param>
public record MuteGroupMemberCommand(Guid GroupId, Guid UserId, int DurationMinutes) : IRequest<bool>;

