namespace FileDev.Domain.Options;

public class NotFileStorageOptions
{
    /// <summary>存储根路径</summary>
    public string StoragePath { get; set; } = "FileStorage";

    /// <summary>分块文件大小（默认5MB）</summary>
    public long ChunkFileSize { get; set; } = 5 * 1024 * 1024;

    /// <summary>最大文件大小（默认10GB）</summary>
    public long MaxFileSize { get; set; } = 10L * 1024 * 1024 * 1024;

    /// <summary>临时文件存储路径</summary>
    public string TempPath { get; set; } = "temp_chunks";

    /// <summary>默认编码</summary>
    public string DefaultEncoding { get; set; } = "utf-8";

    /// <summary>允许的文件扩展名（全类型）</summary>
    public HashSet<string> AllowedExtensions { get; set; } =
    [
        // 图片
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico",
        // 文档
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".md", ".csv", ".json", ".xml", ".html", ".htm",
        // 音频
        ".mp3", ".wav", ".ogg", ".flac", ".aac", ".wma", ".m4a",
        // 视频
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm",
        // 压缩包
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2"
    ];

    /// <summary>哈希算法</summary>
    public string HashAlgorithm { get; set; } = "SHA256";

    /// <summary>分片过期时间（小时，默认24小时）</summary>
    public int ChunkExpirationHours { get; set; } = 24;

    /// <summary>用户最大存储配额（字节，默认50GB）</summary>
    public long UserStorageQuota { get; set; } = 50L * 1024 * 1024 * 1024;
}
