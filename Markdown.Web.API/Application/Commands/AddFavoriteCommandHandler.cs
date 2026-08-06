using Markdown.Infrastructure.Idempotent;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Markdown.Web.API.Application.Commands;

/// <summary>
///     添加收藏命令处理器：未收藏则创建；已收藏则合并标签（幂等）；
///     并发重复收藏由 (UserGuid, MarkDownGuid) 唯一约束兜底
/// </summary>
public class AddFavoriteCommandHandler(
    IMarkFavoriteRepository favoriteRepository,
    IRequestManagement requestManagement,
    ILogger<AddFavoriteCommandHandler> logger) : NotMediator.IRequestHandler<AddFavoriteCommand, Guid>
{
    public async Task<Guid> Handler(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        // 幂等执行：原子占位（唯一约束防并发重复）→ 赢家执行业务并写入响应 → 输家返回首次执行结果
        return await requestManagement.ExecuteIdempotentAsync(request.IdempotencyKey, async () =>
        {
            var tags = request.Tags ?? [];
            Guid favoriteGuid;

            var existing = await favoriteRepository.FindFavoriteAsync(request.UserId, request.MarkDownGuid);
            if (existing is not null)
            {
                // 已收藏：合并新标签后返回（幂等，标签管理语义：重复收藏 = 追加分类）
                existing.AddTags(tags);
                await favoriteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation("收藏已存在，标签已合并：{FavoriteGuid}，文章 {MarkDownGuid}",
                    existing.MarkFavoriteGuid, request.MarkDownGuid);
                favoriteGuid = existing.MarkFavoriteGuid;
            }
            else
            {
                var favorite = MarkFavorite.Create(request.UserId, request.MarkDownGuid, tags);
                try
                {
                    await favoriteRepository.AddAsync(favorite);
                    await favoriteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
                    favoriteGuid = favorite.MarkFavoriteGuid;
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    // 并发重复收藏（不同 IdempotencyKey 同时请求）：唯一约束兜底，返回已存在记录
                    logger.LogInformation("并发重复收藏，唯一约束兜底：用户 {UserGuid}，文章 {MarkDownGuid}",
                        request.UserId, request.MarkDownGuid);

                    var concurrent = await favoriteRepository.FindFavoriteAsync(request.UserId, request.MarkDownGuid)
                        ?? throw new InvalidOperationException("收藏记录状态异常，请重试");
                    concurrent.AddTags(tags);
                    await favoriteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
                    favoriteGuid = concurrent.MarkFavoriteGuid;
                }

                logger.LogInformation("收藏已添加：{FavoriteGuid}，文章 {MarkDownGuid}",
                    favoriteGuid, request.MarkDownGuid);
            }

            // 同步记录标签库使用（标签复用建议：常用标签排序）
            await favoriteRepository.RecordTagUsagesAsync(request.UserId, tags);
            await favoriteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return favoriteGuid;
        });
    }

    /// <summary>
    ///     判断 DbUpdateException 是否为唯一约束冲突（PostgreSQL 23505）
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
