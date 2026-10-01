namespace FileDev.Domain.Entities;

/// <summary>
/// 数据卷聚合，持久化 Mono.FileBox.Lite 存储的卷映射记录。
/// 在应用启动或运行时由卷服务从存储统计同步，对外提供卷清单、占用统计与租户专属卷管理。
/// 卷 ID（VolumeId："v{n}" 或空串表示默认卷）是业务主键，物理路径/依赖均由 Key 唯一标识。
/// </summary>
public class NotFileVolume : Entity<Guid>, IAggregateRoot
{
    private NotFileVolume()
    {
        VolumeId = string.Empty;
        RootPath = string.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private NotFileVolume(string volumeId, string? tenantId, string rootPath, VolumeKind kind) : this()
    {
        VolumeId = volumeId;
        TenantId = tenantId;
        RootPath = rootPath;
        Kind = kind;
    }

    /// <summary>卷 ID："v{n}" 或空串表示默认卷（业务主键）。</summary>
    public string VolumeId { get; private set; }

    /// <summary>所属租户 ID；为空表示共享/默认卷。</summary>
    public string? TenantId { get; private set; }

    /// <summary>数据卷落盘根目录（用于重建后端）。</summary>
    public string RootPath { get; private set; }

    /// <summary>卷类型（默认/共享/租户专属）。</summary>
    public VolumeKind Kind { get; private set; }

    /// <summary>当前已存字节数。</summary>
    public long TotalBytes { get; private set; }

    /// <summary>当前分片总数。</summary>
    public long PartCount { get; private set; }

    /// <summary>当前对象总数。</summary>
    public long ObjectCount { get; private set; }

    /// <summary>卷记录创建时间。</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>最近一次同步刷新时间。</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>静态工厂：从存储卷记录构造领域实体。</summary>
    /// <param name="volumeId">卷 ID（"v{n}" 或空串）。</param>
    /// <param name="tenantId">所属租户 ID（可为空）。</param>
    /// <param name="rootPath">卷根目录。</param>
    /// <param name="kind">卷类型。</param>
    public static NotFileVolume FromRegistration(string volumeId, string? tenantId, string rootPath, VolumeKind kind)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        return new NotFileVolume(volumeId, tenantId, rootPath, kind);
    }

    /// <summary>以记录的持久化值重建聚合（供仓储从 DB 恢复时使用）。</summary>
    public static NotFileVolume Restore(
        string volumeId, string? tenantId, string rootPath, VolumeKind kind,
        long totalBytes, long partCount, long objectCount,
        DateTimeOffset? createdAt = null, DateTimeOffset? updatedAt = null)
    {
        var entity = new NotFileVolume(volumeId, tenantId, rootPath, kind)
        {
            TotalBytes = totalBytes,
            PartCount = partCount,
            ObjectCount = objectCount,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow
        };
        return entity;
    }

    /// <summary>用卷占用统计刷新领域实体（占用总在增长，直接覆盖即可）。</summary>
    public void SyncUsage(long totalBytes, long partCount, long objectCount)
    {
        TotalBytes = totalBytes;
        PartCount = partCount;
        ObjectCount = objectCount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}