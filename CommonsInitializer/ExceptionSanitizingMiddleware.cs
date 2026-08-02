using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CommonsInitializer;

/// <summary>
/// 全局异常脱敏中间件（S-16）。
/// <para>
/// 捕获管道中未处理的异常：完整异常与堆栈仅写入服务端日志；
/// 客户端仅收到无内部路径/堆栈/连接串的通用 500 响应。
/// </para>
/// </summary>
public class ExceptionSanitizingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionSanitizingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // 响应已开始写出时无法替换响应体，只能重新抛出交由宿主处理
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            // 不暴露任何内部信息（路径/堆栈/连接串）
            await context.Response.WriteAsJsonAsync(new
            {
                code = 500,
                message = "服务器内部错误"
            });
        }
    }
}
