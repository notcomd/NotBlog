using FileDev.Domain.Dto.Response;
using FileDev.Domain.Enum;
using FileDev.Domain.IServices;
using Microsoft.Extensions.Logging;
using Mono.FileBox.Lite.Abstractions.Index;

namespace FileDev.Infrastructure.Service;

// 数据卷与目录管理（FileBox 无卷注册表，基于索引聚合）。
// 统计口径（用户级文件存储）：以 FileBox 命名空间（= 用户）为卷的记录单位，按用户逐条产出，
// 默认命名空间产出默认卷；数据来源为 IEntryStore 全量索引条目，不依赖请求上下文，
// 因此后台同步任务（无 HttpContext）也能可靠统计到各用户命名空间。
public sealed partial class FileBoxObjectStorageService
{
    /// <summary>默认卷 ID（默认命名空间对应）。</summary>
    private const string DefaultVolumeId = "default";

    /// <summary>租户命名空间前缀（与 <see cref="NsOf"/> 保持一致）。</summary>
    private const string TenantNamespacePrefix = "tenant:";

    /// <summary>列出 ObjectKey 首段（无 '/' 视为 "(root)"）。</summary>
    private static string DirectoryOf(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return "(root)";
        var idx = objectKey.IndexOf('/');
        return idx < 0 ? "(root)" : objectKey[..idx];
    }

    /// <summary>解析命名空间归属，得到（租户 ID / 卷类型 / 卷 ID）。</summary>
    /// <param name="namespaceId">FileBox 命名空间。</param>
    private static (string? TenantId, VolumeKind Kind, string VolumeId) ParseNamespace(string namespaceId)
    {
        // 默认命名空间（含空值）：默认卷，无归属租户
        if (string.IsNullOrWhiteSpace(namespaceId) || namespaceId == FallbackNamespace)
            return (null, VolumeKind.Default, DefaultVolumeId);

        // 租户命名空间 tenant:{userId}：租户卷，卷 ID 取命名空间本身（保证唯一）
        if (namespaceId.StartsWith(TenantNamespacePrefix, StringComparison.Ordinal))
            return (namespaceId[TenantNamespacePrefix.Length..], VolumeKind.Tenant, namespaceId);

        // 未预期命名空间：仍作为租户卷呈现，避免统计遗漏
        return (namespaceId, VolumeKind.Tenant, namespaceId);
    }

    /// <summary>按统计口径取索引条目：指定租户取该用户命名空间，未指定取全部命名空间。</summary>
    /// <param name="tenantId">租户 ID（= 用户 ID）；为空表示统计全部命名空间。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task<IReadOnlyList<IndexEntry>> ListEntriesAsync(string? tenantId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
            return await _entries.ListByNamespaceAsync(NsOf(tenantId), ct).ConfigureAwait(false);
        return await _entries.ListAllAsync(ct).ConfigureAwait(false);
    }

    /// <summary>由索引条目构建单条卷记录 DTO。</summary>
    private static NotFileVolumeInfoDto BuildVolume(string volumeId, string? tenantId, VolumeKind kind,
        IReadOnlyCollection<IndexEntry> entries) => new()
    {
        VolumeId = volumeId,
        TenantId = tenantId,
        RootPath = string.Empty,
        Kind = kind,
        TotalBytes = entries.Sum(e => e.SizeBytes),
        ObjectCount = entries.Count,
        PartCount = entries.Count,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    /// <summary>新增一个共享数据卷（FileBox 单一磁盘池，映射为默认卷返回，不真正热增池）。</summary>
    /// <param name="rootPath">卷落盘根目录（保留以对齐契约）。</param>
    /// <param name="ct">取消令牌。</param>
    public Task<NotFileVolumeInfoDto> AddVolumeAsync(string rootPath, CancellationToken ct = default)
        => Task.FromResult(new NotFileVolumeInfoDto
        {
            VolumeId = DefaultVolumeId,
            TenantId = null,
            RootPath = rootPath,
            Kind = VolumeKind.Default,
            UpdatedAt = DateTimeOffset.UtcNow
        });

    /// <summary>为指定租户新增专属数据卷（FileBox 单一磁盘池，记录归属租户返回默认卷）。</summary>
    /// <param name="tenantId">目标租户 ID（= 用户 ID）。</param>
    /// <param name="rootPath">卷落盘根目录（保留以对齐契约）。</param>
    /// <param name="ct">取消令牌。</param>
    public Task<NotFileVolumeInfoDto> AddTenantVolumeAsync(string tenantId, string rootPath,
        CancellationToken ct = default)
        => Task.FromResult(new NotFileVolumeInfoDto
        {
            VolumeId = NsOf(tenantId),
            TenantId = tenantId,
            RootPath = rootPath,
            Kind = VolumeKind.Tenant,
            UpdatedAt = DateTimeOffset.UtcNow
        });

    /// <summary>
    /// 列出卷占用统计。指定租户时返回该用户的单条卷记录；未指定时按命名空间逐条产出
    /// （每个用户一条租户卷 + 默认命名空间一条默认卷），不受请求上下文影响。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <param name="tenantId">租户 ID（= 用户 ID）；为空表示统计全部用户命名空间。</param>
    public async Task<IEnumerable<NotFileVolumeInfoDto>> GetVolumeStatsAsync(CancellationToken ct = default,
        string? tenantId = null)
    {
        var entries = await ListEntriesAsync(tenantId, ct).ConfigureAwait(false);

        // 指定租户：单条卷记录
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var (tid, kind, vid) = ParseNamespace(NsOf(tenantId));
            return [BuildVolume(vid, tid, kind, entries)];
        }

        // 未指定：按命名空间（= 用户）逐条
        return entries
            .GroupBy(e => e.NamespaceId ?? string.Empty)
            .Select(g =>
            {
                var (tid, kind, vid) = ParseNamespace(g.Key);
                return BuildVolume(vid, tid, kind, g.ToList());
            })
            .OrderBy(v => v.Kind)
            .ThenBy(v => v.VolumeId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 列出虚拟目录统计（按 ObjectKey 首段聚合的物理占用视图）。
    /// 指定租户时统计该用户命名空间；未指定时跨全部命名空间聚合。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <param name="tenantId">租户 ID（= 用户 ID）；为空表示统计全部用户命名空间。</param>
    public async Task<IEnumerable<NotFileDirectoryInfoDto>> GetDirectoryStatsAsync(CancellationToken ct = default,
        string? tenantId = null)
    {
        var entries = await ListEntriesAsync(tenantId, ct).ConfigureAwait(false);
        return entries
            .GroupBy(e => DirectoryOf(e.ObjectKey ?? string.Empty))
            .Select(g => new NotFileDirectoryInfoDto
            {
                Name = g.Key,
                TotalBytes = g.Sum(e => e.SizeBytes),
                PartCount = g.Count(),
                ObjectCount = g.Count()
            })
            .OrderByDescending(d => d.TotalBytes)
            .ToList();
    }
}