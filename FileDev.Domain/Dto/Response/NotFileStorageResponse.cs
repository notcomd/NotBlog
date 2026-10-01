namespace FileDev.Domain.Dto.Response;

public record NotFileStorageResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 文件完整路径/访问URL
    /// </summary>
    public string FullPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 错误信息（失败时返回）
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件实际哈希值（校验用）
    /// </summary>
    public string ActualHash { get; set; } = string.Empty;

    // ---- 与 Mono.FileBox.Lite 对齐的存储元数据 ----

    /// <summary>内容 SHA-256 摘要（= FileBox IndexEntry.ContentHash）。</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>存储层（Hot/Cold）。</summary>
    public StorageTier Tier { get; set; } = StorageTier.Hot;

    /// <summary>物理分片所在数据卷 ID（默认卷 "default"，与 FileBox 磁盘池对齐）。</summary>
    public string VolumeId { get; set; } = string.Empty;

    /// <summary>物理分片总数（FileBox 内容寻址分块后的分片数。默认单块为 1）。</summary>
    public int ShardCount { get; set; }

    /// <summary>TTL 过期时间（为空表示永不过期）。</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>FileBox 清单最近更新 UTC 时间（IndexEntry.ModifiedAt）。</summary>
    public DateTimeOffset? UpdatedUtc { get; set; }
}