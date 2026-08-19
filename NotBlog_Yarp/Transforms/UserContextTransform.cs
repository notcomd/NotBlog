using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;

namespace NotBlog_Yarp.Transforms;

/// <summary>
/// 注册 UserContextTransform 到所有路由
/// </summary>
public class UserContextTransformProvider : ITransformProvider
{
    private readonly ILogger<UserContextTransformProvider> _logger;

    public UserContextTransformProvider(ILogger<UserContextTransformProvider> logger)
    {
        _logger = logger;
    }

    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(ApplyAsync);
    }

    /// <summary>
    /// 从 JWT Claims 中提取 UserId 和 Roles，
    /// 从 HttpContext.Items 读取 DataScope（由 PermissionFilterMiddleware 注入），
    /// 注入到下游请求的 Header: X-User-Id, X-User-Roles, X-Data-Scope
    /// </summary>
    private ValueTask ApplyAsync(RequestTransformContext context)
    {
        var user = context.HttpContext.User;
        var path = context.HttpContext.Request.Path;

        // 先清除所有可能被伪造的 Header
        context.ProxyRequest.Headers.Remove("X-User-Id");
        context.ProxyRequest.Headers.Remove("X-User-Roles");
        context.ProxyRequest.Headers.Remove("X-Data-Scope");

        if (user.Identity?.IsAuthenticated == true)
        {
            // 提取用户 GUID（支持多种 claim 类型）
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value
                      ?? user.FindFirst("id")?.Value;

            // 提取角色列表
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);

            if (!string.IsNullOrEmpty(userId))
            {
                // 注入可信 Header
                context.ProxyRequest.Headers.Add("X-User-Id", userId);
                context.ProxyRequest.Headers.Add("X-User-Roles", string.Join(",", roles));

                // 注入 DataScope（由 PermissionFilterMiddleware 存入 Items）
                // PermissionCheckResult.DataScope 为 Dictionary<string, HashSet<string>>，
                // 序列化为 "type|v1,v2,..." 格式注入 X-Data-Scope Header
                if (context.HttpContext.Items.TryGetValue(
                        PermissionFilterMiddleware.DataScopeItemKey, out var scopeObj) &&
                    scopeObj is Dictionary<string, HashSet<string>> scopeDict &&
                    scopeDict.Count > 0)
                {
                    var scopeValue = string.Join("|",
                        scopeDict.Select(kv => $"{kv.Key}|{string.Join(",", kv.Value)}"));
                    if (!string.IsNullOrEmpty(scopeValue))
                        context.ProxyRequest.Headers.Add("X-Data-Scope", scopeValue);
                }

                _logger.LogDebug(
                    "[Transform] OK   | Path={Path} | X-User-Id={UserId} | X-User-Roles={Roles}",
                    path, userId, string.Join(",", roles));
            }
            else
            {
                var claimTypes = string.Join(", ", user.Claims.Select(c => c.Type).Distinct());
                _logger.LogWarning(
                    "[Transform] NULL | Path={Path} | Auth=OK 但 UserId 为空 | ClaimTypes=[{ClaimTypes}]",
                    path, claimTypes);
            }
        }
        else
        {
            _logger.LogDebug("[Transform] SKIP | Path={Path} | Auth=未认证", path);
        }

        return ValueTask.CompletedTask;
    }
}
