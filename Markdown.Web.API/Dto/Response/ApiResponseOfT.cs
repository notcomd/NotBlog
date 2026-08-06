namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// 带数据的统一 API 响应包装
/// </summary>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public static ApiResponse<T> Created(T data, string? message = null) => new() { Success = true, Data = data, Message = message ?? "创建成功" };
    public static new ApiResponse<T> Error(string message) => new() { Success = false, Message = message };
    public static ApiResponse<T> NotFound(string message = "资源不存在") => new() { Success = false, Message = message };
    public static ApiResponse<T> Forbidden(string message = "无权访问此资源") => new() { Success = false, Message = message };
}
