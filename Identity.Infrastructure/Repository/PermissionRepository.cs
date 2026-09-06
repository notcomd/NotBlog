namespace Identity.Infrastructure.Repository;

public class PermissionRepository(IdentityDbContext dbContext) : IPermissionRepository
{
    public IUnitOfWork UnitOfWork => dbContext;

    public async ValueTask<Permission?> FindByIdAsync(Guid permissionId, CancellationToken ct = default)
    {
        return await dbContext.Permissions.FindAsync([permissionId], ct);
    }

    public async ValueTask<Permission?> FindByCodeAsync(string permissionCode, CancellationToken ct = default)
    {
        return await dbContext.Permissions
            .FirstOrDefaultAsync(p => p.PermissionCode == permissionCode, ct);
    }

    public async Task<List<Permission>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.Permissions
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Permission>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
    {
        return await dbContext.Permissions
            .Where(p => p.ParentId == parentId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async ValueTask AddAsync(Permission permission, CancellationToken ct = default)
    {
        await dbContext.Permissions.AddAsync(permission, ct);
    }

    public ValueTask UpdateAsync(Permission permission, CancellationToken ct = default)
    {
        dbContext.Permissions.Update(permission);
        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> DeleteAsync(Guid permissionId, CancellationToken ct = default)
    {
        var entity = await dbContext.Permissions.FindAsync([permissionId], ct);
        if (entity is null) return false;
        entity.SoftDelete(true);
        return true;
    }

    public async ValueTask<bool> CodeExistsAsync(string permissionCode, CancellationToken ct = default)
    {
        return await dbContext.Permissions
            .AnyAsync(p => p.PermissionCode == permissionCode && !p.IsDeleted, ct);
    }
}
