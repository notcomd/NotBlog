using System.Net;
using System.Text.Json;

namespace Message.Web.API.Middleware;
public class ExceptionHandlingMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // 响应已开始写出时无法替换响应体，只能重新抛出交由宿主处理
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "发生未处理的异常: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "未授权访问"),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            NotSupportedException => (HttpStatusCode.BadRequest, exception.Message),
            TimeoutException => (HttpStatusCode.RequestTimeout, "请求超时"),
            _ => (HttpStatusCode.InternalServerError, "服务器内部错误")
        };

        response.StatusCode = (int)statusCode;

        var apiResponse = ApiResponse.Error(message, (int)statusCode);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(apiResponse, options));
    }
}

