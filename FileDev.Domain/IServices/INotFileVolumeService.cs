using FileDev.Domain.Dto.Response;

namespace FileDev.Domain.IServices;

/// <summary>
/// 数据卷管理服务：桥接 Mono.FileBox.Lite 存储与 DB 卷表。
/// 负责把 FileBox 存储的卷/目录统计同步落库，并提供共享/租户专属卷的新增与查询能力。
/// </summary>
public interface INotFileVolumeService
{
    
    /// <summary>从存储卷统计全量同步到 DB（返回同步后的卷数）。</summary>
    Task<int> SyncVolumesAsync(CancellationToken ct = default);


    /// <summary>查询全部卷（DB 侧，通常已同步）。</summary>
    Task<IEnumerable<NotFileVolumeInfoDto>> GetVolumesAsync(CancellationToken ct = default);


    /// <summary>按卷 ID 查询单一卷。</summary>
    Task<NotFileVolumeInfoDto?> GetVolumeAsync(string volumeId, CancellationToken ct = default);


    /// <summary>新增一个共享数据卷并同步记录。</summary>
    Task<NotFileVolumeInfoDto> AddSharedVolumeAsync(string rootPath, CancellationToken ct = default);


    /// <summary>为指定租户新增专属数据卷并同步记录（tenantId = 用户 ID 字符串）。</summary>
    Task<NotFileVolumeInfoDto> AddTenantVolumeAsync(string tenantId, string rootPath, CancellationToken ct = default);


    /// <summary>列出虚拟目录统计（按 Key 首段聚合，物理占用视图）。</summary>
    Task<IEnumerable<NotFileDirectoryInfoDto>> GetDirectoryStatsAsync(CancellationToken ct = default);
}