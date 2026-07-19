using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace NotBlog_Yarp.Transforms;

/// <summary>
/// 注册 UserContextTransform 到所有路由
/// </summary>
public class UserContextTransformProvider : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(ApplyAsync);
    }

    /// <summary>
    /// 从 JWT Claims 中提取 UserId 和 Roles，
    /// 注入到下游请求的 Header: X-User-Id, X-User-Roles
    /// </summary>
    private static ValueTask ApplyAsync(RequestTransformContext context)
    {
        var user = context.HttpContext.User;
        var path = context.HttpContext.Request.Path;

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
                // 移除客户端可能伪造的 Header
                context.ProxyRequest.Headers.Remove("X-User-Id");
                context.ProxyRequest.Headers.Remove("X-User-Roles");

                // 注入可信 Header
                context.ProxyRequest.Headers.Add("X-User-Id", userId);
                context.ProxyRequest.Headers.Add("X-User-Roles", string.Join(",", roles));

                Console.WriteLine($"[Transform] OK   | Path={path} | X-User-Id={userId} | X-User-Roles={string.Join(",", roles)}");
            }
            else
            {
                var allClaims = string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"));
                Console.WriteLine($"[Transform] NULL | Path={path} | Auth=OK 但 UserId 为空 | Claims=[{allClaims}]");
            }
        }
        else
        {
            // 未认证请求：清除可能存在的伪造 Header
            context.ProxyRequest.Headers.Remove("X-User-Id");
            context.ProxyRequest.Headers.Remove("X-User-Roles");

            Console.WriteLine($"[Transform] SKIP | Path={path} | Auth=未认证");
        }

        return ValueTask.CompletedTask;
    }
}
