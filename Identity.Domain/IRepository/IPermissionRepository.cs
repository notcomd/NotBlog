namespace Identity.Domain.IRepository;

/// <summary>
/// 权限仓储接口（Permission 不是聚合根，故不继承 IRepository&lt;T&gt;）
/// </summary>
public interface IPermissionRepository
{
    IUnitOfWork UnitOfWork { get; }

    ValueTask<Permission?> FindByIdAsync(Guid permissionId, CancellationToken ct = default);
    ValueTask<Permission?> FindByCodeAsync(string permissionCode, CancellationToken ct = default);
    Task<List<Permission>> GetAllAsync(CancellationToken ct = default);
    ValueTask AddAsync(Permission permission, CancellationToken ct = default);
    ValueTask UpdateAsync(Permission permission, CancellationToken ct = default);
    ValueTask<bool> DeleteAsync(Guid permissionId, CancellationToken ct = default);
    ValueTask<bool> CodeExistsAsync(string permissionCode, CancellationToken ct = default);
}
