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

    // ---- 与 MohuTianchi.Lite 对齐的存储元数据 ----

    /// <summary>内容 SHA-256 摘要（= Lite ObjectIndexEntry.ContentHash）。</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>存储层（Hot/Cold）。</summary>
    public StorageTier Tier { get; set; } = StorageTier.Hot;

    /// <summary>物理分片所在数据卷 ID（Lite ObjectManifest.VolumeId）。</summary>
    public string VolumeId { get; set; } = string.Empty;

    /// <summary>物理分片总数（Lite ObjectManifest.Shards 数量）。</summary>
    public int ShardCount { get; set; }

    /// <summary>TTL 过期时间（Lite ObjectManifest.ExpiresAt；为空表示永不过期）。</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Lite 清单最近更新 UTC 时间（ObjectManifest.UpdatedUtc）。</summary>
    public DateTimeOffset? UpdatedUtc { get; set; }
}