using Markdown.Infrastructure.Idempotent;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
///     取消收藏命令处理器：删除收藏记录；未收藏时幂等返回成功（用户目标状态=未收藏）
/// </summary>
public class RemoveFavoriteCommandHandler(
    IMarkFavoriteRepository favoriteRepository,
    IRequestManagement requestManagement,
    ILogger<RemoveFavoriteCommandHandler> logger) : IRequestHandler<RemoveFavoriteCommand, bool>
{
    public async Task<bool> Handler(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
            var favorite = await favoriteRepository.FindFavoriteAsync(request.UserId, request.MarkDownGuid);
            if (favorite is null)
            {
                // 未收藏：幂等返回成功（重复取消/从未收藏均视为目标状态达成）
                logger.LogInformation("未找到收藏记录，取消收藏幂等返回：用户 {UserGuid}，文章 {MarkDownGuid}",
                    request.UserId, request.MarkDownGuid);
                return true;
            }

            await favoriteRepository.RemoveAsync(favorite);
            await favoriteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("收藏已取消：{FavoriteGuid}，文章 {MarkDownGuid}",
                favorite.MarkFavoriteGuid, request.MarkDownGuid);
            return true;
        });
    }
}
