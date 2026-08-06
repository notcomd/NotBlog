namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 转让群主命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="NewOwnerId">新群主用户 ID</param>
public record TransferOwnershipCommand(Guid GroupId, Guid NewOwnerId) : IRequest<bool>;

