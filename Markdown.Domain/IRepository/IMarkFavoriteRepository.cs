namespace Markdown.Domain.IRepository;

/// <summary>
///     MarkFavorite 收藏聚合根仓储接口（写侧：添加 / 查询 / 移除 / 改标签；列表查询由读侧直接投影）
/// </summary>
public interface IMarkFavoriteRepository : IRepository<MarkFavorite, IUnitOfWork>
{
    /// <summary>
    ///     添加收藏聚合根
    /// </summary>
    Task<MarkFavorite> AddAsync(MarkFavorite favorite);

    /// <summary>
    ///     按用户 + 文章查找收藏（追踪态，用于标签合并/取消收藏/改标签）
    /// </summary>
    Task<MarkFavorite?> FindFavoriteAsync(Guid userGuid, Guid markDownGuid);

    /// <summary>
    ///     移除收藏（取消收藏）
    /// </summary>
    Task RemoveAsync(MarkFavorite favorite);

    /// <summary>
    ///     覆盖式更新收藏标签（收藏不存在时抛 KeyNotFoundException；标签校验由领域层完成）
    /// </summary>
    Task<MarkFavorite> UpdateTagsAsync(Guid userGuid, Guid markDownGuid, IEnumerable<string> tags);

    /// <summary>
    ///     记录标签使用（标签库 upsert：已存在则计数 +1 并刷新最近使用，不存在则创建），
    ///     供"常用标签"复用建议
    /// </summary>
    Task RecordTagUsagesAsync(Guid userGuid, IEnumerable<string> tags);
}
