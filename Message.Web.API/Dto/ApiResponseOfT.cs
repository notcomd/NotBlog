namespace Message.Web.API.Dto;
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public int Code { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message, Code = 200 };

    public static ApiResponse<T> Created(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message, Code = 201 };

    public static ApiResponse<T> NoContent(string? message = null)
        => new() { Success = true, Message = message, Code = 204 };

    public static ApiResponse<T> BadRequest(string message, int code = 400)
        => new() { Success = false, Message = message, Code = code };

    public static ApiResponse<T> NotFound(string message = "资源不存在")
        => new() { Success = false, Message = message, Code = 404 };

    public static ApiResponse<T> Unauthorized(string message = "未授权访问")
        => new() { Success = false, Message = message, Code = 401 };

    public static ApiResponse<T> Forbidden(string message = "禁止访问")
        => new() { Success = false, Message = message, Code = 403 };

    public static ApiResponse<T> Error(string message, int code = 500)
        => new() { Success = false, Message = message, Code = code };
}

