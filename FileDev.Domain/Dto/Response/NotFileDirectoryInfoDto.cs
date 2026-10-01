namespace FileDev.Domain.Dto.Response;

/// <summary>虚拟目录统计信息，供管理 API 返回（FileBox 索引按 ObjectKey 首段聚合）。</summary>
public record NotFileDirectoryInfoDto
{
    /// <summary>目录名（Key 首段路径；无 '/' 为 "(root)"）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>已存字节数。</summary>
    public long TotalBytes { get; set; }

    /// <summary>分片总数。</summary>
    public long PartCount { get; set; }

    /// <summary>对象总数。</summary>
    public long ObjectCount { get; set; }
}