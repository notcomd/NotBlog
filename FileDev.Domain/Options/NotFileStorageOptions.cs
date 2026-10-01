namespace FileDev.Domain.Options;

public class NotFileStorageOptions
{
    /// <summary>存储根路径</summary>
    public string StoragePath { get; set; } = "FileStorage";

    /// <summary>分块文件大小（默认5MB）</summary>
    public int ChunkFileSize { get; set; } = 5242880;

    /// <summary>最大文件大小（默认100MB，与 Kestrel 请求体大小限制保持一致）</summary>
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024;

    /// <summary>临时文件存储路径</summary>
    public string TempPath { get; set; } = "temp_chunks";

    /// <summary>默认编码</summary>
    public string DefaultEncoding { get; set; } = "utf-8";

    /// <summary>允许的文件扩展名（全类型）。
    /// S-17：.html/.htm/.svg 可从浏览器直接渲染（存在 XSS 风险），已从白名单移除。</summary>
    public HashSet<string> AllowedExtensions { get; set; } =
    [
        // 图片
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico",
        // 文档
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".md", ".csv", ".json", ".xml",
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

    /// <summary>下载内容缓存大小阈值（字节）。超过该上限的大文件不进入 Redis 缓存，直接回源 FileBox。默认5MB。</summary>
    public long DownloadCacheMaxBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>下载内容缓存有效期（秒）。读时惰性回填，到期自动失效。默认10分钟。</summary>
    public int DownloadCacheTtlSeconds { get; set; } = 600;
}
