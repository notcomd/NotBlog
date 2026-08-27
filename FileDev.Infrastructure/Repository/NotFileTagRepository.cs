using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

/// <summary>
/// 标签仓储。扁平一层，用户内按名称唯一。
/// </summary>
public class NotFileTagRepository(NotFileDbContext notFileDbContext) : INotFileTagRepository
{
    private readonly NotFileDbContext _notFileDbContext =
        notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public IUnitOfWork UnitOfWork => notFileDbContext;

    public async Task InsertNotFileTagAsync(NotFileTag notFileTag)
    {
        if (notFileTag is null)
            throw new NotFileException("NotFileTag is null");
        await _notFileDbContext.NotFileTags.AddAsync(notFileTag);
    }

    public async Task<NotFileTag> GetNotFileTagByIdAsync(Guid tagId)
    {
        if (tagId == Guid.Empty)
            throw new NotFileException("tagId is invalid");

        // 不加 AsNoTracking：标签归属命令（AddFile/RemoveFile）会修改实体并通过 ChangeTracker 持久化
        var data = await _notFileDbContext.NotFileTags
            .FirstOrDefaultAsync(x => x.TagId.Equals(tagId) && !x.IsDeleted);
        return data ?? throw new NotFileException("NotFileTag is null");
    }

    public async Task<IEnumerable<NotFileTag>> GetNotFileTagsByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new NotFileException("userId is invalid");
        return await _notFileDbContext.NotFileTags
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderBy(x => x.UploadTime)
            .ToListAsync();
    }

    public async Task<NotFileTag?> GetNotFileTagByNameAsync(Guid userId, string tagName)
    {
        return await _notFileDbContext.NotFileTags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.TagName == tagName && !x.IsDeleted);
    }

    public async Task<NotFileTag?> GetNotFileTagByKindAsync(Guid userId, FileDev.Domain.Enum.TagDefaultKind kind)
    {
        return await _notFileDbContext.NotFileTags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.DefaultKind == kind && !x.IsDeleted);
    }

    public async Task<bool> ExistsByUserIdAndNameAsync(Guid userId, string tagName, Guid? excludeId = null)
    {
        var query = _notFileDbContext.NotFileTags
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.UserId == userId)
            .Where(x => x.TagName == tagName);
        if (excludeId.HasValue)
            query = query.Where(x => x.TagId != excludeId.Value);
        return await query.AnyAsync();
    }

    public Task<NotFileTag?> UpdateNotFileTagAsync(NotFileTag notFileTag)
    {
        _notFileDbContext.NotFileTags.Update(notFileTag);
        return Task.FromResult<NotFileTag?>(notFileTag);
    }
}