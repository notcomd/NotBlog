using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

public class NotFileGroupRepository(NotFileDbContext notFileDbContext) : INotFileGroupRepository
{
    private readonly NotFileDbContext _notFileDbContext = notFileDbContext
                                                          ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public IUnitOfWork UnitOfWork => notFileDbContext;


    public async Task InsertNotFileGroupAsync(NotFileGroup notFileGroup)
    {
        if(notFileGroup is null)
            throw new NotFileException("NotFileGroup is null");
        await _notFileDbContext.NotFileGroups.AddAsync(notFileGroup);
    }

    public async Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId)
    {
        if (Guid.Empty == notFileGroupId)
            throw new NotFileException("notFileGroupId is null");
        var data = await _notFileDbContext
            .NotFileGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NotFileGroupId.Equals(notFileGroupId) && !x.IsDeleted);

        return data ?? throw new NotFileException("NotFileGroup is null");
    }

    public async Task<IEnumerable<NotFileGroup>> GetAllNotFileGroupsAsync()
    {
        var data = await _notFileDbContext
            .NotFileGroups
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync();
        return data.AsEnumerable();
    }

    public async Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new NotFileException("userId is null");
        var data = await _notFileDbContext.NotFileGroups
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ToListAsync();
        return data.AsEnumerable();
    }

    public async Task<IEnumerable<NotFileGroup>> GetPublicNotFileGroupsAsync()
    {
        return await _notFileDbContext.NotFileGroups
            .AsNoTracking()
            .Where(x => x.FileIdentity == FileIdentity.FilePublic && !x.IsDeleted)
            .ToListAsync();
    }

    public async Task<NotFileGroup?> GetNotFileGroupByNameAsync(string fileGroupName)
    {
        return await _notFileDbContext.NotFileGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FileGroupName == fileGroupName && !x.IsDeleted);
    }

    /// <summary>
    /// 更新文件组：调用 EF Core 的 Update 将实体标记为 Modified，由 SaveChangesAsync 持久化。
    /// 修复：原实现仅查询返回，未执行任何更新操作（空操作 bug）。
    /// </summary>
    public Task<NotFileGroup?> UpdateNotFileGroupAsync(NotFileGroup notFileGroup)
    {
        _notFileDbContext.NotFileGroups.Update(notFileGroup);
        return Task.FromResult<NotFileGroup?>(notFileGroup);
    }

    public async Task DeleteNotFileGroupAsync(Guid notFileGroupId)
    {
        var data = await _notFileDbContext.NotFileGroups
            .FirstOrDefaultAsync(x => x.NotFileGroupId == notFileGroupId);
        if (data is null)
            throw new NotFileException("NotFileGroup is null");
        _notFileDbContext.NotFileGroups.Remove(data);
    }

    // ---- 树形结构查询 ----

    public async Task<bool> ExistsByNameAtSameLevelAsync(Guid userId, Guid? parentGroupId, string name, Guid? excludeId = null)
    {
        var query = _notFileDbContext.NotFileGroups
            .AsNoTracking()
            .Where(x => x.IsDeleted == false)
            .Where(x => x.UserId == userId)
            .Where(x => x.ParentGroupId == parentGroupId)
            .Where(x => x.FileGroupName == name);

        if (excludeId.HasValue)
            query = query.Where(x => x.NotFileGroupId != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<NotFileGroup>> GetChildrenAsync(Guid parentGroupId)
    {
        return await _notFileDbContext.NotFileGroups
            .AsNoTracking()
            .Where(x => x.ParentGroupId == parentGroupId && x.IsDeleted == false)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotFileGroup>> GetRootGroupsByUserIdAsync(Guid userId)
    {
        // 注意：不加 AsNoTracking — UploadNotFileEventHandler 会修改返回的实体并通过 ChangeTracker 持久化
        return await _notFileDbContext.NotFileGroups
            .Where(x => x.UserId == userId && x.ParentGroupId == null && x.IsDeleted == false)
            .ToListAsync();
    }
}
