namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// 统一 API 响应包装
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null) => new() { Success = true, Message = message };
    public static ApiResponse Error(string message) => new() { Success = false, Message = message };
}

