using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

/// <summary>
/// 用户存储额度仓储。每用户一条记录（UserId 主键），供配额校验与额度调整。
/// 占用/释放走数据库端原子 UPDATE，避免并发上传下的丢失更新。
/// </summary>
public class UserFileInfoRepository(NotFileDbContext notFileDbContext) : IUserFileInfoRepository
{
    private readonly NotFileDbContext _notFileDbContext =
        notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public IUnitOfWork UnitOfWork => notFileDbContext;

    public async Task<UserFileInfo?> GetByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new NotFileException("userId is invalid");

        // 不加 AsNoTracking：兼容额度调整等读改写场景（Occupy/Release/AdjustQuota）
        return await _notFileDbContext.UserFileInfos
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task InsertUserFileInfoAsync(UserFileInfo userFileInfo)
    {
        if (userFileInfo is null)
            throw new NotFileException("UserFileInfo is null");
        await _notFileDbContext.UserFileInfos.AddAsync(userFileInfo);
    }

    public async Task<UserFileInfo> EnsureUserFileInfoAsync(UserFileInfo userFileInfo)
    {
        if (userFileInfo is null)
            throw new NotFileException("UserFileInfo is null");

        await _notFileDbContext.UserFileInfos.AddAsync(userFileInfo);
        try
        {
            // 立即落库以暴露主键冲突；后续整体事务回滚时该插入一并回滚
            await _notFileDbContext.SaveChangesAsync();
            return userFileInfo;
        }
        catch (DbUpdateException)
        {
            // 并发首插主键冲突：移除本地残留的 Added 实体（避免后续 SaveChanges 重复插入），
            // 返回已存在记录（UserId 主键唯一，必然存在）
            _notFileDbContext.Entry(userFileInfo).State = EntityState.Detached;
            var existing = await _notFileDbContext.UserFileInfos
                .FirstOrDefaultAsync(x => x.UserId == userFileInfo.UserId);
            if (existing is not null)
                return existing;
            throw;
        }
    }

    public async Task<bool> TryOccupyAsync(Guid userId, long bytes, CancellationToken cancellationToken = default)
    {
        if (bytes <= 0)
            return true;

        var rows = await _notFileDbContext.UserFileInfos
            .Where(x => x.UserId == userId &&
                        (x.TotalQuotaBytes <= 0 || x.UsedBytes + bytes <= x.TotalQuotaBytes))
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UsedBytes, x => x.UsedBytes + bytes)
                    .SetProperty(x => x.UpdateTime, DateTimeOffset.UtcNow),
                cancellationToken);

        // 0 行 = 记录不存在或配额不足（配额不足按超限处理）
        return rows > 0;
    }

    public async Task<bool> ReleaseAsync(Guid userId, long bytes, CancellationToken cancellationToken = default)
    {
        if (bytes <= 0)
            return true;

        var rows = await _notFileDbContext.UserFileInfos
            .Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UsedBytes, x => x.UsedBytes - bytes < 0 ? 0 : x.UsedBytes - bytes)
                    .SetProperty(x => x.UpdateTime, DateTimeOffset.UtcNow),
                cancellationToken);

        // 0 行 = 记录不存在（正常释放/删除场景不应出现，忽略）
        return rows > 0;
    }
}
