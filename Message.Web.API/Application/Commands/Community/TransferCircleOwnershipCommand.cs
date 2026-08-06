namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 转移圈主命令（仅现任圈主）。
/// </summary>
public record TransferCircleOwnershipCommand(Guid OperatorGuid, Guid CircleGuid, Guid NewOwnerGuid) : IRequest<bool>;
