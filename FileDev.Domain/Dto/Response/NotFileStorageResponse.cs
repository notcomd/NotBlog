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
    public string FullPath { get; set; }
    
    /// <summary>
    /// 错误信息（失败时返回）
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件实际哈希值（校验用）
    /// </summary>
    public string ActualHash { get; set; }
}