using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

/// <summary>数据卷仓储：持久化存储卷统计（NotFileVolume 表）。</summary>
public class NotFileVolumeRepository(NotFileDbContext notFileDbContext) : INotFileVolumeRepository
{
    private readonly NotFileDbContext _db = notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public IUnitOfWork UnitOfWork => notFileDbContext;

    public async Task<NotFileVolume?> GetByIdAsync(string volumeId, CancellationToken ct = default)
    {
        return await _db.NotFileVolumes.FirstOrDefaultAsync(x => x.VolumeId == volumeId, ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<NotFileVolume>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.NotFileVolumes.AsNoTracking().OrderBy(x => x.VolumeId).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<NotFileVolume>> GetByTenantAsync(string? tenantId, CancellationToken ct = default)
    {
        // 空 tenantId 查共享/默认卷（TenantId 为 null）
        return await _db.NotFileVolumes.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.VolumeId)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>全景刷新：删除现有卷记录后整体重插，保证与存储统计一致。</summary>
    public async Task ReplaceAllAsync(IEnumerable<NotFileVolume> volumes, CancellationToken ct = default)
    {
        _db.NotFileVolumes.RemoveRange(await _db.NotFileVolumes.ToListAsync(ct).ConfigureAwait(false));
        await _db.NotFileVolumes.AddRangeAsync(volumes, ct).ConfigureAwait(false);
    }

    public async Task UpsertAsync(NotFileVolume volume, CancellationToken ct = default)
    {
        var existing = await _db.NotFileVolumes
            .FirstOrDefaultAsync(x => x.VolumeId == volume.VolumeId, ct).ConfigureAwait(false);
        if (existing is null)
            await _db.NotFileVolumes.AddAsync(volume, ct).ConfigureAwait(false);
        else
            _db.Entry(existing).CurrentValues.SetValues(volume);
    }

    public async Task DeleteAsync(string volumeId, CancellationToken ct = default)
    {
        var existing = await _db.NotFileVolumes
            .FirstOrDefaultAsync(x => x.VolumeId == volumeId, ct).ConfigureAwait(false);
        if (existing is not null)
            _db.NotFileVolumes.Remove(existing);
    }
}