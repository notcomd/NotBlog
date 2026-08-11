namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 创建圈子命令。
/// <para>CQRS 命令侧：仅返回新圈子 ID（Guid），不返回业务实体/DTO。</para>
/// </summary>
public record CreateCircleCommand(
    Guid UserId,
    string Name,
    string? Description,
    string? AvatarUrl,
    string? CoverUrl,
    int? MaxMembers) : IRequest<Guid>;
