using FileDev.Domain.Dto.Request;


namespace FileDev.Domain.IServices;

public interface INotFileStorageService
{
    /// <summary>
    /// 保存文件
    /// </summary>
    Task<NotFileStorageResponse> SaveAsync(NotFileStorageRequest request);

    /// <summary>
    /// 删除文件
    /// </summary>
    Task<NotFileStorageResponse> DeleteAsync(string fileRelativePath);

    /// <summary>
    /// 获取文件内容
    /// </summary>
    Task<(byte[] Content, NotFileStorageResponse Response)> GetContentAsync(string fileRelativePath);

    /// <summary>
    /// 流式获取文件内容（避免大文件整读入内存，S-09）
    /// </summary>
    Task<(Stream? Content, NotFileStorageResponse Response)> GetContentStreamAsync(string fileRelativePath);

    /// <summary>
    /// 清理某上传任务的临时分片文件（S-09）
    /// </summary>
    Task CleanupChunksAsync(string fileKey);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    Task<bool> ExistsAsync(string fileRelativePath);

    /// <summary>
    /// 上传单个分片
    /// </summary>
    Task<NotFileStorageResponse> UploadChunkAsync(string fileKey, int chunkIndex, byte[] chunkContent,
        string? chunkHash = null);

    /// <summary>
    /// 合并分片为完整文件
    /// </summary>
    Task<NotFileStorageResponse> MergeChunksAsync(string fileKey, int totalChunks, string? expectedFileHash = null,
        bool overwrite = true);

    /// <summary>
    /// 获取总分片数
    /// </summary>
    Task<int> GetTotalChunkCountAsync(long fileSize);

    /// <summary>
    /// 读取对象清单（由 FileBox 索引条目映射 + ContentHash），用于对齐文件存储元数据；对象不存在返回 null。
    /// </summary>
    Task<StorageManifestDto?> GetManifestAsync(string fileRelativePath, CancellationToken ct = default);

    /// <summary>
    /// 调整对象存储层（Hot→Cold），并返回改动后的清单；对象不存在返回 null。
    /// </summary>
    Task<StorageManifestDto?> ChangeStorageTierAsync(string fileRelativePath, StorageTier tier,
        CancellationToken ct = default);

    /// <summary>新增一个共享数据卷（返回新卷记录，卷 ID 为空串表示默认卷）。</summary>
    Task<NotFileVolumeInfoDto> AddVolumeAsync(string rootPath, CancellationToken ct = default);

    /// <summary>为指定租户新增专属数据卷。</summary>
    Task<NotFileVolumeInfoDto> AddTenantVolumeAsync(string tenantId, string rootPath,
        CancellationToken ct = default);

    /// <summary>列出全部数据卷统计（FileBox 磁盘池映射的卷记录）。</summary>
    Task<IEnumerable<NotFileVolumeInfoDto>> GetVolumeStatsAsync(CancellationToken ct = default);

    /// <summary>列出虚拟目录统计（按 Key 首段聚合的物理占用视图）。</summary>
    Task<IEnumerable<NotFileDirectoryInfoDto>> GetDirectoryStatsAsync(CancellationToken ct = default);
}