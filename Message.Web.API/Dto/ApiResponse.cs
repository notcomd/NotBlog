namespace Message.Web.API.Dto;
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string? message = null)
        => new() { Success = true, Message = message, Code = 200 };

    public new static ApiResponse NoContent(string? message = null)
        => new() { Success = true, Message = message, Code = 204 };

    public new static ApiResponse BadRequest(string message, int code = 400)
        => new() { Success = false, Message = message, Code = code };

    public new static ApiResponse NotFound(string message = "资源不存在")
        => new() { Success = false, Message = message, Code = 404 };

    public new static ApiResponse Unauthorized(string message = "未授权访问")
        => new() { Success = false, Message = message, Code = 401 };

    public new static ApiResponse Forbidden(string message = "禁止访问")
        => new() { Success = false, Message = message, Code = 403 };

    public new static ApiResponse Error(string message, int code = 500)
        => new() { Success = false, Message = message, Code = code };
}