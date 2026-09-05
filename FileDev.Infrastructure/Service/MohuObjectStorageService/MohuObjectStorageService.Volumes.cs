using FileDev.Domain.Dto.Response;
using FileDev.Domain.Enum;
using Microsoft.Extensions.Logging;
using MohuTianchi.Lite;
using LiteDirectoryInfo = MohuTianchi.Lite.DirectoryInfo;

namespace FileDev.Infrastructure.Service;

// 数据卷与目录管理（Lite 租户相关接口）：把 Lite 卷注册表/目录统计映射为领域 DTO，
// 提供默认/共享/租户专属卷的新增与全量统计查询。Lite 方法均为同步，经 Task.FromResult 包装为异步契约。
public sealed partial class MohuObjectStorageService
{
    /// <summary>将 Lite 卷记录映射为对外 DTO，并按卷归属推导卷类型。</summary>
    /// <param name="record">Lite 卷记录。</param>
    private static NotFileVolumeInfoDto MapVolume(VolumeRecord record) => new()
    {
        VolumeId = record.VolumeId ?? string.Empty,
        TenantId = record.TenantId,
        RootPath = record.RootPath ?? string.Empty,
        Kind = string.IsNullOrEmpty(record.VolumeId)
            ? VolumeKind.Default
            : string.IsNullOrEmpty(record.TenantId) ? VolumeKind.Shared : VolumeKind.Tenant,
        TotalBytes = record.TotalBytes,
        PartCount = record.PartCount,
        ObjectCount = record.ObjectCount
    };

    /// <summary>新增一个共享数据卷（卷 ID 为空串表示默认卷）。</summary>
    /// <param name="rootPath">卷落盘根目录。</param>
    /// <param name="ct">取消令牌（保留以对齐契约，Lite 同步执行）。</param>
    public Task<NotFileVolumeInfoDto> AddVolumeAsync(string rootPath, CancellationToken ct = default)
    {
        var record = _storage.AddVolume(rootPath);
        var dto = MapVolume(record);
        dto.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(dto);
    }

    /// <summary>为指定租户新增专属数据卷。</summary>
    /// <param name="tenantId">目标租户 ID。</param>
    /// <param name="rootPath">卷落盘根目录。</param>
    /// <param name="ct">取消令牌（保留以对齐契约，Lite 同步执行）。</param>
    public Task<NotFileVolumeInfoDto> AddTenantVolumeAsync(string tenantId, string rootPath,
        CancellationToken ct = default)
    {
        var record = _storage.AddTenantVolume(tenantId, rootPath);
        var dto = MapVolume(record);
        dto.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(dto);
    }

    /// <summary>列出全部数据卷统计（默认/共享/租户专属，镜像 Lite VolumeRecord）。</summary>
    /// <param name="ct">取消令牌（保留以对齐契约，Lite 同步执行）。</param>
    public Task<IEnumerable<NotFileVolumeInfoDto>> GetVolumeStatsAsync(CancellationToken ct = default)
    {
        var records = _storage.GetVolumeStats();
        return Task.FromResult<IEnumerable<NotFileVolumeInfoDto>>(records?.Select(MapVolume).ToList() ?? []);
    }

    /// <summary>将 Lite 目录信息映射为对外 DTO。</summary>
    /// <param name="info">Lite 目录统计。</param>
    private static NotFileDirectoryInfoDto MapDirectory(LiteDirectoryInfo info) => new()
    {
        Name = info.Name ?? string.Empty,
        TotalBytes = info.TotalBytes,
        PartCount = info.PartCount,
        ObjectCount = info.ObjectCount
    };

    /// <summary>列出虚拟目录统计（按 Key 首段聚合的物理占用视图，镜像 Lite DirectoryInfo）。</summary>
    /// <param name="ct">取消令牌（保留以对齐契约，Lite 同步执行）。</param>
    public Task<IEnumerable<NotFileDirectoryInfoDto>> GetDirectoryStatsAsync(CancellationToken ct = default)
    {
        var infos = _storage.GetDirectoryStats();
        return Task.FromResult<IEnumerable<NotFileDirectoryInfoDto>>(infos?.Select(MapDirectory).ToList() ?? []);
    }
}