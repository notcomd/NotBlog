namespace FileDev.Domain.Options;

public class NotFileStorageOptions
{
    /// <summary>
    ///  存储路径
    /// </summary>
    public string StoragePath { get; set; } = "/User/Load";

    /// <summary>
    ///  分块文件大小
    /// </summary>
    public long ChunkFileSize { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    ///  最大文件大小
    /// </summary>
    public long MaxFileSize { get; set; } = 1024 * 1024 * 1024;

    /// <summary>
    ///  临时文件存储路径
    /// </summary>
    public string TempPath { get; set; } = "/temp/cachat";

    /// <summary>
    /// 默认编码
    /// </summary>
    public string DefaultEncoding { get; set; } = "utf-8";

    /// <summary>
    /// 允许的文件扩展名
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [".jpg", ".png", ".gif", ".jpeg"];
    
    public string HashAlgorithm { get; set; } = "SHA256";
}