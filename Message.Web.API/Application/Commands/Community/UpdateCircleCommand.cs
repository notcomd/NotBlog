namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 更新圈子信息命令（圈主/管理员）。
/// </summary>
public record UpdateCircleCommand(
    Guid OperatorGuid,
    Guid CircleGuid,
    string Name,
    string? Description,
    string? AvatarUrl) : IRequest<bool>;
