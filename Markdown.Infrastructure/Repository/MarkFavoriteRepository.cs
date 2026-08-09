namespace Markdown.Infrastructure.Repository;

/// <summary>
///     MarkFavorite 收藏聚合根仓储实现（写侧数据访问入口）
/// </summary>
public class MarkFavoriteRepository(
    MarkDownDbContext markDownDbContext,
    ILogger<MarkFavoriteRepository> logger) : IMarkFavoriteRepository
{
    public IUnitOfWork UnitOfWork => markDownDbContext;

    /// <summary>
    ///     添加收藏聚合根
    /// </summary>
    public async Task<MarkFavorite> AddAsync(MarkFavorite favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);

        await markDownDbContext.MarkFavorites.AddAsync(favorite);
        logger.LogInformation("收藏已添加：{FavoriteGuid}，文章 {MarkDownGuid}，用户 {UserGuid}",
            favorite.MarkFavoriteGuid, favorite.MarkDownGuid, favorite.UserGuid);
        return favorite;
    }

    /// <summary>
    ///     按用户 + 文章查找收藏（追踪态，用于标签合并/取消收藏/改标签）
    /// </summary>
    public async Task<MarkFavorite?> FindFavoriteAsync(Guid userGuid, Guid markDownGuid)
    {
        var favorite = await markDownDbContext.MarkFavorites
            .FirstOrDefaultAsync(x => x.UserGuid == userGuid && x.MarkDownGuid == markDownGuid);

        if (favorite is null)
            logger.LogInformation("未找到收藏记录：用户 {UserGuid}，文章 {MarkDownGuid}", userGuid, markDownGuid);

        return favorite;
    }

    /// <summary>
    ///     移除收藏（取消收藏）
    /// </summary>
    public Task RemoveAsync(MarkFavorite favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);

        markDownDbContext.MarkFavorites.Remove(favorite);
        logger.LogInformation("收藏已移除：{FavoriteGuid}，文章 {MarkDownGuid}", favorite.MarkFavoriteGuid, favorite.MarkDownGuid);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     覆盖式更新收藏标签（收藏不存在抛 KeyNotFoundException；标签校验由领域层 SetTags 完成）
    /// </summary>
    public async Task<MarkFavorite> UpdateTagsAsync(Guid userGuid, Guid markDownGuid, IEnumerable<string> tags)
    {
        var favorite = await FindFavoriteAsync(userGuid, markDownGuid)
            ?? throw new KeyNotFoundException($"收藏不存在：用户 {userGuid}，文章 {markDownGuid}");

        favorite.SetTags(tags);
        logger.LogInformation("收藏标签已覆盖更新：{FavoriteGuid}，文章 {MarkDownGuid}，用户 {UserGuid}",
            favorite.MarkFavoriteGuid, markDownGuid, userGuid);
        return favorite;
    }

    /// <summary>
    ///     记录标签使用（标签库 upsert）：一次查询批量匹配已存在标签，
    ///     已存在则 RecordUsage（计数 +1、刷新最近使用），不存在则创建（首次使用）
    /// </summary>
    public async Task RecordTagUsagesAsync(Guid userGuid, IEnumerable<string> tags)
    {
        if (tags is null)
            return;

        var validTags = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (validTags.Count == 0)
            return;

        var existingList = await markDownDbContext.MarkFavoriteTags
            .Where(x => x.UserGuid == userGuid && validTags.Contains(x.Tag))
            .ToListAsync();

        var existingMap = existingList.ToDictionary(x => x.Tag, StringComparer.Ordinal);
        foreach (var tag in validTags)
        {
            if (existingMap.TryGetValue(tag, out var record))
            {
                record.RecordUsage();
            }
            else
            {
                markDownDbContext.MarkFavoriteTags.Add(MarkFavoriteTag.Create(userGuid, tag));
            }
        }

        logger.LogInformation("标签库已记录 {Count} 个标签使用（用户 {UserGuid}）", validTags.Count, userGuid);
    }
}
