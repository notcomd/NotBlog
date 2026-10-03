namespace Markdown.Web.API.Application.Commands.Favorite;

/// <summary>
///     覆盖式更新收藏标签命令（空/ null 表示清空标签）
/// </summary>
public record UpdateFavoriteTagsCommand(
    Guid UserId,
    Guid MarkDownGuid,
    IEnumerable<string>? Tags = null,
    Guid IdempotencyKey = default
) : IRequest<bool>, ICommandRequest;
