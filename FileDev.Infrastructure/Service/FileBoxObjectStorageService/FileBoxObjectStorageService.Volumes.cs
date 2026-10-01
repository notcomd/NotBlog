using FileDev.Domain.Dto.Response;
using FileDev.Domain.Enum;
using Microsoft.Extensions.Logging;
using Mono.FileBox.Lite.Abstractions.Index;

namespace FileDev.Infrastructure.Service;

// 数据卷与目录管理（FileBox 无卷注册表，基于索引聚合）：映射磁盘池为默认卷记录，
// 目录统计按 ObjectKey 首段聚合。FileBox 池运行期不热增，新增卷由上层 NotFileVolumeService 记录。
public sealed partial class FileBoxObjectStorageService
{
    /// <summary>默认卷 ID（FileBox 磁盘池单一池，映射为默认卷）。</summary>
    private const string DefaultVolumeId = "default";

    /// <summary>列出 ObjectKey 首段（无 '/' 视为 "(root)"）。</summary>
    private static string DirectoryOf(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return "(root)";
        var idx = objectKey.IndexOf('/');
        return idx < 0 ? "(root)" : objectKey[..idx];
    }

    /// <summary>列出全部已存对象（用于卷/目录统计聚合）。</summary>
    private async Task<IReadOnlyList<IndexEntry>> ListAllIndexedAsync(CancellationToken ct)
    {
        var results = new List<IndexEntry>();
        string? cursor = null;
        do
        {
            var page = await _index.QueryAsync(new IndexQuery
            {
                NamespaceId = NamespaceId,
                Page = new PageRequest { Size = 500, Cursor = cursor }
            }, ct).ConfigureAwait(false);
            results.AddRange(page.Items);
            cursor = page.HasMore ? page.NextCursor : null;
        } while (cursor is not null);
        return results;
    }

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
    /// <param name="tenantId">目标租户 ID。</param>
    /// <param name="rootPath">卷落盘根目录（保留以对齐契约）。</param>
    /// <param name="ct">取消令牌。</param>
    public Task<NotFileVolumeInfoDto> AddTenantVolumeAsync(string tenantId, string rootPath,
        CancellationToken ct = default)
        => Task.FromResult(new NotFileVolumeInfoDto
        {
            VolumeId = DefaultVolumeId,
            TenantId = tenantId,
            RootPath = rootPath,
            Kind = VolumeKind.Tenant,
            UpdatedAt = DateTimeOffset.UtcNow
        });

    /// <summary>列出全部数据卷统计（FileBox 单一磁盘池，聚合成单条默认卷）。</summary>
    /// <param name="ct">取消令牌。</param>
    public async Task<IEnumerable<NotFileVolumeInfoDto>> GetVolumeStatsAsync(CancellationToken ct = default)
    {
        var entries = await ListAllIndexedAsync(ct).ConfigureAwait(false);
        return
        [
            new NotFileVolumeInfoDto
            {
                VolumeId = DefaultVolumeId,
                TenantId = null,
                RootPath = string.Empty,
                Kind = VolumeKind.Default,
                TotalBytes = entries.Sum(e => e.SizeBytes),
                ObjectCount = entries.Count,
                PartCount = entries.Count,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        ];
    }

    /// <summary>列出虚拟目录统计（按 ObjectKey 首段聚合的物理占用视图）。</summary>
    /// <param name="ct">取消令牌。</param>
    public async Task<IEnumerable<NotFileDirectoryInfoDto>> GetDirectoryStatsAsync(CancellationToken ct = default)
    {
        var entries = await ListAllIndexedAsync(ct).ConfigureAwait(false);
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