using FileDev.Domain.Entities;

namespace FileDev.Domain.IRepository;

/// <summary>数据卷仓储：从 DB 持久化卷注册表（镜像 Lite VolumeRecord）。</summary>
public interface INotFileVolumeRepository : IRepository<NotFileVolume, IUnitOfWork>
{
    /// <summary>按卷 ID 查询（"v{n}" 或空串为默认卷）。</summary>
    Task<NotFileVolume?> GetByIdAsync(string volumeId, CancellationToken ct = default);

    /// <summary>查询全部卷。</summary>
    Task<IEnumerable<NotFileVolume>> GetAllAsync(CancellationToken ct = default);

    /// <summary>查询归属某租户的卷（为空查共享/默认卷）。</summary>
    Task<IEnumerable<NotFileVolume>> GetByTenantAsync(string? tenantId, CancellationToken ct = default);

    /// <summary>全景刷新：全量替换卷注册表（启动或手动全量同步时调用）。</summary>
    Task ReplaceAllAsync(IEnumerable<NotFileVolume> volumes, CancellationToken ct = default);

    /// <summary>新增或更新一条卷记录。</summary>
    Task UpsertAsync(NotFileVolume volume, CancellationToken ct = default);

    /// <summary>删除卷记录。</summary>
    Task DeleteAsync(string volumeId, CancellationToken ct = default);
}