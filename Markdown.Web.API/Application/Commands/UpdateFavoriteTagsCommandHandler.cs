using Markdown.Infrastructure.Idempotent;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
///     覆盖式更新收藏标签命令处理器：更新标签并同步记录标签库使用（复用建议）；
///     收藏不存在抛 KeyNotFoundException（404）
/// </summary>
public class UpdateFavoriteTagsCommandHandler(
    IMarkFavoriteRepository favoriteRepository,
    IRequestManagement requestManagement,
    ILogger<UpdateFavoriteTagsCommandHandler> logger) : NotMediator.IRequestHandler<UpdateFavoriteTagsCommand, bool>
{
    public async Task<bool> Handler(UpdateFavoriteTagsCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
            var tags = request.Tags ?? [];

            // 覆盖式更新（收藏不存在抛 KeyNotFoundException；标签校验由领域层 SetTags 完成）
            var favorite = await favoriteRepository.UpdateTagsAsync(request.UserId, request.MarkDownGuid, tags);

            // 同步记录标签库使用（标签复用建议）
            await favoriteRepository.RecordTagUsagesAsync(request.UserId, tags);
            await favoriteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("收藏标签已更新：{FavoriteGuid}，文章 {MarkDownGuid}，用户 {UserGuid}",
                favorite.MarkFavoriteGuid, request.MarkDownGuid, request.UserId);
            return true;
        });
    }
}
