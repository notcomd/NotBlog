namespace NotBlog_Yarp.Extensions;

using System.Security.Claims;
using NotBlog_Yarp.Middlewares;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

public static class UserContextTransformExtensions
{
    /// <summary>
    /// 为 TransformBuilder 添加用户上下文 Header 注入
    /// 
    /// 注入 Header:
    ///   X-User-Id     — 用户 GUID
    ///   X-User-Roles  — 用户角色列表（逗号分隔）
    ///   X-Data-Scope  — 数据范围（由 PermissionFilterMiddleware 注入到 HttpContext.Items）
    /// 
    /// 安全: 先移除客户端可能伪造的同名 Header，再注入可信值
    /// </summary>
    public static TransformBuilderContext AddUserContextTransform(
        this TransformBuilderContext context)
    {
        context.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;

            // 先清除所有可能被伪造的 Header
            transformContext.ProxyRequest.Headers.Remove("X-User-Id");
            transformContext.ProxyRequest.Headers.Remove("X-User-Roles");
            transformContext.ProxyRequest.Headers.Remove("X-Data-Scope");

            if (user.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value
                          ?? user.FindFirst("id")?.Value;

                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);

                if (!string.IsNullOrEmpty(userId))
                {
                    transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                    transformContext.ProxyRequest.Headers.Add(
                        "X-User-Roles", string.Join(",", roles));

                    // 注入 DataScope（由 PermissionFilterMiddleware 存入 HttpContext.Items）
                    if (transformContext.HttpContext.Items.TryGetValue(
                            PermissionFilterMiddleware.DataScopeItemKey, out var scopeObj) &&
                        scopeObj is string scopeValue && !string.IsNullOrEmpty(scopeValue))
                    {
                        transformContext.ProxyRequest.Headers.Add("X-Data-Scope", scopeValue);
                    }
                }
            }

            return ValueTask.CompletedTask;
        });
        return context;
    }
}
