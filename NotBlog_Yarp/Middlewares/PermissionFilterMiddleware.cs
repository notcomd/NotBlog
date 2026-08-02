using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NotBlog_Yarp.Permission;

namespace NotBlog_Yarp.Middlewares;

/// <summary>
/// 网关权限过滤中间件
/// 
/// 在 JWT 验证通过后、YARP 转发前执行，检查用户是否拥有访问目标路由的权限。
/// 
/// 处理流程:
///   1. 匹配公开路径 → 直接放行
///   2. URL→PermissionCode 映射 → 未配置则放行（白名单模式）
///   3. 未认证 → 返回 401（带 WWW-Authenticate）
///   4. 调用权限服务组合查询（CheckAndGetScopeAsync，单次往返）
///   5. 无权限 → 403 JSON；有权限 → DataScope 存入 Items，放行
/// </summary>
public class PermissionFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PermissionRouteMap _routeMap;
    private readonly ILogger<PermissionFilterMiddleware> _logger;
    private readonly IOptions<PermissionOptions> _permissionOptions;

    /// <summary>HttpContext.Items 中存储 DataScope 的键</summary>
    public const string DataScopeItemKey = "NotBlog.DataScope";

    public PermissionFilterMiddleware(
        RequestDelegate next,
        PermissionRouteMap routeMap,
        ILogger<PermissionFilterMiddleware> logger,
        IOptions<PermissionOptions> permissionOptions)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _routeMap = routeMap ?? throw new ArgumentNullException(nameof(routeMap));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionOptions = permissionOptions ?? throw new ArgumentNullException(nameof(permissionOptions));
    }

    public async Task InvokeAsync(
        HttpContext context, IPermissionServiceClient permissionClient)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        // 1. 公开路径直接放行
        if (_routeMap.IsPublicPath(path))
        {
            _logger.LogDebug("[PermissionFilter] 公开路径放行 Path={Path}", path);
            await _next(context);
            return;
        }

        // 2. 需要鉴权的路径
        if (_routeMap.TryMatch(path, method, out var permissionCode))
        {
            // 2.1 必须已认证 — 触发 JWT Bearer Challenge（设置 WWW-Authenticate 头）
            if (context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("[PermissionFilter] 未认证拒绝 Path={Path}", path);

                if (!context.Response.HasStarted)
                {
                    await context.ChallengeAsync();
                }
                return;
            }

            // 2.2 获取并验证 userId
            var userId = GetUserId(context.User);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning(
                    "[PermissionFilter] 无法提取有效 UserId Path={Path} Raw={RawUserId}", path, userId);
                await WriteForbiddenAsync(context);
                return;
            }

            // 2.3 组合查询：权限检查 + DataScope（单次往返，减少延迟）
            PermissionCheckResult result;
            try
            {
                result = await permissionClient.CheckAndGetScopeAsync(
                    userGuid, permissionCode, context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[PermissionFilter] 权限服务调用异常 UserId={UserId} Code={Code}（FailPolicy={Policy}）",
                    userId, permissionCode, _permissionOptions.Value.FailPolicy);

                // F-12：FailPolicy=Open 时降级放行（记录日志并继续），Closed 时拒绝 403
                if (_permissionOptions.Value.FailOpen)
                {
                    _logger.LogWarning(
                        "[PermissionFilter] 权限服务异常，按 FailPolicy=Open 放行 Path={Path}",
                        path);
                    await _next(context);
                    return;
                }

                await WriteForbiddenAsync(context);
                return;
            }

            if (!result.HasPermission)
            {
                _logger.LogWarning(
                    "[PermissionFilter] 权限拒绝 UserId={UserId} Code={Code} Path={Path} Method={Method}",
                    userId, permissionCode, path, method);
                await WriteForbiddenAsync(context);
                return;
            }

            // 存入 DataScope 供 UserContextTransform 注入 Header
            context.Items[DataScopeItemKey] = result.DataScope;

            _logger.LogInformation(
                "[PermissionFilter] 通过 UserId={UserId} Code={Code} Path={Path}",
                userId, permissionCode, path);
        }
        else
        {
            // 3. 未配置权限映射的路径 → 白名单模式：默认放行（仅记录 Debug 日志）
            _logger.LogDebug(
                "[PermissionFilter] 未映射路径放行 Path={Path} Method={Method}", path, method);
        }

        await _next(context);
    }

    /// <summary>
    /// 从 ClaimsPrincipal 提取用户 ID
    /// </summary>
    private static string? GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value;
    }

    /// <summary>
    /// 写入统一的 403 Forbidden JSON 响应
    /// </summary>
    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            "{\"error\":\"Forbidden\",\"message\":\"Insufficient permissions\"}",
            context.RequestAborted);
    }
}
