namespace FileDev.Domain.Dto.Request;

public record NotFileStorageRequest
{
    /// <summary>
    /// 文件相对路径（如：docs/2026/test.txt）
    /// </summary>
    public string FileRelativePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件内容（文本/二进制）
    /// </summary>
    public byte[] FileContent { get; set; } = [];
    
    /// <summary>
    /// 是否覆盖已存在的文件
    /// </summary>
    public bool Overwrite { get; set; } = true;
    
    /// <summary>
    /// 存储类型（默认本地）
    /// </summary>
    public StorageType StorageType { get; set; } = StorageType.Local;
    
    /// <summary>
    /// 文本文件编码（默认UTF8）
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// 预期的文件哈希值（用于校验）
    /// </summary>
    public string? ExpectedHash { get; set; }

    /// <summary>
    /// 文件类别（个人文件 / 内容附件）。用于存储层按类别路由到独立物理池；null=默认个人文件。
    /// </summary>
    public FileSource? Source { get; set; }

    /// <summary>
    /// 租户 ID。用于存储层命名空间逻辑隔离；null=默认租户。
    /// </summary>
    public string? TenantId { get; set; }
}