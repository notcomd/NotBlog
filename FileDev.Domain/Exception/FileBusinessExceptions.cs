namespace FileDev.Domain.Exception;

/// <summary>
/// 文件/上传任务不存在（可预期业务性失败）。
/// 统一异常拦截器将其映射为 gRPC <c>NotFound</c> 状态码。
/// </summary>
public class NotFileNotFoundException : NotFileException
{
    public NotFileNotFoundException() : base("文件不存在")
    {
    }

    public NotFileNotFoundException(string message) : base(message)
    {
    }

    public NotFileNotFoundException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// 无权访问/操作文件（越权访问，S-08）。
/// 统一异常拦截器将其映射为 gRPC <c>PermissionDenied</c> 状态码。
/// </summary>
public class FilePermissionDeniedException : NotFileException
{
    public FilePermissionDeniedException() : base("无权访问此文件")
    {
    }

    public FilePermissionDeniedException(string message) : base(message)
    {
    }

    public FilePermissionDeniedException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// 用户存储配额不足（S-09）。
/// 统一异常拦截器将其映射为 gRPC <c>ResourceExhausted</c> 状态码。
/// </summary>
public class FileQuotaExceededException : NotFileException
{
    public FileQuotaExceededException() : base("用户存储配额不足")
    {
    }

    public FileQuotaExceededException(string message) : base(message)
    {
    }

    public FileQuotaExceededException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}
