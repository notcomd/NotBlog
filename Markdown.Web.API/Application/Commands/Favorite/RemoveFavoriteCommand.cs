namespace Markdown.Web.API.Application.Commands.Favorite;

/// <summary>
///     取消收藏命令（未收藏时幂等返回成功）
/// </summary>
public record RemoveFavoriteCommand(
    Guid UserId,
    Guid MarkDownGuid,
    Guid IdempotencyKey
) : IRequest<bool>, ICommandRequest;
