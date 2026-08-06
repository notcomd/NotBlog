namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 解散圈子命令（仅圈主）。
/// </summary>
public record DissolveCircleCommand(Guid OperatorGuid, Guid CircleGuid) : IRequest<bool>;
