namespace FileDev.Domain.Dto.Response;

/// <summary>对象清单对外信息，由 FileBox 索引条目映射的关键字段。</summary>
public record StorageManifestDto
{
    /// <summary>物理分片所在卷 ID。</summary>
    public string VolumeId { get; set; } = string.Empty;

    /// <summary>物理分片总数。</summary>
    public int ShardCount { get; set; }

    /// <summary>内容 SHA-256 摘要。</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>存储层（Hot/Cold）。</summary>
    public StorageTier Tier { get; set; } = StorageTier.Hot;

    /// <summary>TTL 过期时间（为空表示永不过期）。</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>清单最近更新 UTC 时间。</summary>
    public DateTimeOffset? UpdatedUtc { get; set; }
}