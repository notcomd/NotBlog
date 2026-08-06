namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 生成圈子邀请命令（圈主/管理员）：type = code | link | direct。
/// </summary>
public record GenerateCircleInvitationCommand(
    Guid OperatorGuid,
    Guid CircleGuid,
    string Type,
    Guid? InviteeGuid,
    int? TtlHours) : IRequest<CircleInvitationResult>;
