using FileDev.Domain.Dto.Response;
using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 数据卷管理服务：桥接 Mono.FileBox.Lite 存储与 DB 卷表。
/// 启动/手动全量同步把 FileBox 存储的卷与目录统计落到 NotFileVolume 表，供查询与租户卷管理。
/// </summary>
public class NotFileVolumeService(
    INotFileStorageService storageService,
    INotFileVolumeRepository volumeRepository) : INotFileVolumeService
{
    private readonly INotFileStorageService _storage = storageService
        ?? throw new ArgumentNullException(nameof(storageService));
    private readonly INotFileVolumeRepository _volumeRepository = volumeRepository
        ?? throw new ArgumentNullException(nameof(volumeRepository));

    /// <summary>从存储卷统计转换为领域实体（保留占用统计）。</summary>
    private static NotFileVolume ToVolume(NotFileVolumeInfoDto dto)
    {
        var volume = NotFileVolume.FromRegistration(dto.VolumeId, dto.TenantId, dto.RootPath, dto.Kind);
        volume.SyncUsage(dto.TotalBytes, dto.PartCount, dto.ObjectCount);
        return volume;
    }

    /// <summary>领域实体转换为对外 DTO。</summary>
    private static NotFileVolumeInfoDto ToDto(NotFileVolume volume) => new()
    {
        VolumeId = volume.VolumeId,
        TenantId = volume.TenantId,
        RootPath = volume.RootPath,
        Kind = volume.Kind,
        TotalBytes = volume.TotalBytes,
        PartCount = volume.PartCount,
        ObjectCount = volume.ObjectCount,
        UpdatedAt = volume.UpdatedAt
    };

    public async Task<int> SyncVolumesAsync(CancellationToken ct = default)
    {
        var records = await _storage.GetVolumeStatsAsync(ct).ConfigureAwait(false);
        var volumes = records.Select(ToVolume).ToList();
        await _volumeRepository.ReplaceAllAsync(volumes, ct).ConfigureAwait(false);
        await _volumeRepository.UnitOfWork.SaveEntitiesAsync(ct).ConfigureAwait(false);
        return volumes.Count;
    }

    public async Task<IEnumerable<NotFileVolumeInfoDto>> GetVolumesAsync(CancellationToken ct = default)
    {
        var volumes = await _volumeRepository.GetAllAsync(ct).ConfigureAwait(false);
        return volumes.Select(ToDto).ToList();
    }

    public async Task<NotFileVolumeInfoDto?> GetVolumeAsync(string volumeId, CancellationToken ct = default)
    {
        var volume = await _volumeRepository.GetByIdAsync(volumeId, ct).ConfigureAwait(false);
        return volume is null ? null : ToDto(volume);
    }

    public async Task<NotFileVolumeInfoDto> AddSharedVolumeAsync(string rootPath, CancellationToken ct = default)
    {
        var dto = await _storage.AddVolumeAsync(rootPath, ct).ConfigureAwait(false);
        await _volumeRepository.UpsertAsync(ToVolume(dto), ct).ConfigureAwait(false);
        await _volumeRepository.UnitOfWork.SaveEntitiesAsync(ct).ConfigureAwait(false);
        return dto;
    }

    public async Task<NotFileVolumeInfoDto> AddTenantVolumeAsync(string tenantId, string rootPath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("租户 ID 不能为空", nameof(tenantId));
        var dto = await _storage.AddTenantVolumeAsync(tenantId, rootPath, ct).ConfigureAwait(false);
        await _volumeRepository.UpsertAsync(ToVolume(dto), ct).ConfigureAwait(false);
        await _volumeRepository.UnitOfWork.SaveEntitiesAsync(ct).ConfigureAwait(false);
        return dto;
    }

    public async Task<IEnumerable<NotFileDirectoryInfoDto>> GetDirectoryStatsAsync(CancellationToken ct = default)
    {
        return await _storage.GetDirectoryStatsAsync(ct).ConfigureAwait(false);
    }
}