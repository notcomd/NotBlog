using FileDev.Infrastructure.Service;
using FileDev.Web.API.APIs;

namespace FileDev.Web.API.Middleware;

/// <summary>
/// 租户上下文填充中间件：在 JWT 认证通过后，从当前用户身份（identity/nameidentifier）解析租户 ID，
/// 写入 <see cref="AsyncLocalTenantContext"/>，供存储层多租户命名空间路由使用。
/// <para>
/// 本中间件须置于 <c>UseAuthentication()</c> 之后、文件存储相关端点之前。</para>
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextMiddleware> _logger;

    public TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public Task InvokeAsync(HttpContext context)
    {
        try
        {
            // 租户 ID = 当前用户 ID（JWT identity/nameidentifier），无则不设置（回退默认租户）
            var userId = FileApiHelpers.GetUserId(context);
            AsyncLocalTenantContext.SetTenantId(userId?.ToString("N"));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "设置租户上下文失败（回退默认租户）");
        }

        return _next(context);
    }
}