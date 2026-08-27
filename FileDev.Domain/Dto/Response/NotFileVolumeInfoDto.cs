namespace FileDev.Domain.Dto.Response;

/// <summary>数据卷对外信息，镜像 Lite VolumeRecord，供管理 API 返回。</summary>
public record NotFileVolumeInfoDto
{
    /// <summary>卷 ID（"v{n}" 或空串表示默认卷）。</summary>
    public string VolumeId { get; set; } = string.Empty;

    /// <summary>归属租户 ID；为空表示共享/默认卷。</summary>
    public string? TenantId { get; set; }

    /// <summary>卷落盘根目录。</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>卷类型（默认/共享/租户专属）。</summary>
    public VolumeKind Kind { get; set; }

    /// <summary>已存字节数。</summary>
    public long TotalBytes { get; set; }

    /// <summary>分片总数。</summary>
    public long PartCount { get; set; }

    /// <summary>对象总数。</summary>
    public long ObjectCount { get; set; }

    /// <summary>最近同步时间。</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}