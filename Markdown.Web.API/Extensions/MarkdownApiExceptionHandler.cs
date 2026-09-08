using Microsoft.AspNetCore.Diagnostics;

namespace Markdown.Web.API.Extensions;

/// <summary>
///     统一业务异常 → HTTP 状态码映射（消除各端点重复 try-catch 样板）：
///     KeyNotFoundException → 404；UnauthorizedAccessException → 403；
///     InvalidOperationException / ArgumentException 家族 → 400；
///     其余异常返回 false，交由 ExceptionSanitizingMiddleware 脱敏为 500。
/// </summary>
public class MarkdownApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            InvalidOperationException or ArgumentException => StatusCodes.Status400BadRequest,
            _ => 0
        };

        // 未识别的业务异常：不处理，让管道继续传播给全局脱敏中间件
        if (statusCode == 0)
            return false;

        httpContext.Response.StatusCode = statusCode;
        // 输出统一信封（含业务异常消息），包装中间件据此透传，避免二次包装
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponseResult.Failure(exception.Message, statusCode), cancellationToken);
        return true;
    }
}
