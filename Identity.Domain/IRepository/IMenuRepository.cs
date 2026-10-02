namespace Identity.Domain.IRepository;

/// <summary>
/// 菜单仓储接口（Menu 为聚合根）
/// </summary>
public interface IMenuRepository : IRepository<Menu, IUnitOfWork>
{
    ValueTask<Menu?> FindByIdAsync(Guid menuId, CancellationToken ct = default);

    /// <summary>
    /// 获取全部未删除菜单（按 SortOrder、CreatedAt 升序）
    /// </summary>
    Task<List<Menu>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取指定父节点下的直接子菜单（含已删除，供删除前校验/树管理使用）
    /// </summary>
    Task<List<Menu>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);

    ValueTask AddAsync(Menu menu, CancellationToken ct = default);
    ValueTask UpdateAsync(Menu menu, CancellationToken ct = default);
    ValueTask<bool> DeleteAsync(Guid menuId, CancellationToken ct = default);

    /// <summary>
    /// 指定 Url 是否已存在（未删除；excludeId 用于更新时排除自身）
    /// </summary>
    ValueTask<bool> UrlExistsAsync(string url, Guid? excludeId = null, CancellationToken ct = default);
}