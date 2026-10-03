namespace Markdown.Web.API.Application.Commands.Favorite;

/// <summary>
///     添加收藏命令（已收藏时合并标签并幂等返回）
/// </summary>
public record AddFavoriteCommand(
    Guid UserId,
    Guid MarkDownGuid,
    IEnumerable<string>? Tags = null,
    Guid IdempotencyKey = default
) : IRequest<Guid>, ICommandRequest;
