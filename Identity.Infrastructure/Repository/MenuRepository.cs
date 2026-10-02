namespace Identity.Infrastructure.Repository;

public class MenuRepository(IdentityDbContext dbContext) : IMenuRepository
{
    public IUnitOfWork UnitOfWork => dbContext;

    public async ValueTask<Menu?> FindByIdAsync(Guid menuId, CancellationToken ct = default)
    {
        return await dbContext.Menus.FindAsync([menuId], ct);
    }

    public async Task<List<Menu>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.Menus
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Menu>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
    {
        return await dbContext.Menus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async ValueTask AddAsync(Menu menu, CancellationToken ct = default)
    {
        await dbContext.Menus.AddAsync(menu, ct);
    }

    public ValueTask UpdateAsync(Menu menu, CancellationToken ct = default)
    {
        dbContext.Menus.Update(menu);
        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> DeleteAsync(Guid menuId, CancellationToken ct = default)
    {
        var entity = await dbContext.Menus.FindAsync([menuId], ct);
        if (entity is null) return false;
        entity.SoftDelete(true);
        return true;
    }

    public async ValueTask<bool> UrlExistsAsync(string url, Guid? excludeId = null, CancellationToken ct = default)
    {
        return await dbContext.Menus
            .AnyAsync(m => m.Url == url
                           && !m.IsDeleted
                           && (excludeId == null || m.MenuId != excludeId), ct);
    }
}