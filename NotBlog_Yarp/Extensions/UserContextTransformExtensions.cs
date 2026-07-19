namespace NotBlog_Yarp.Extensions;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using System.Security.Claims;
public static class UserContextTransformExtensions
{
    /// <summary>
    /// 为 TransformBuilder 添加用户上下文 Header 注入
    /// </summary>
    public static TransformBuilderContext AddUserContextTransform(
        this TransformBuilderContext context)
    {
        context.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value
                          ?? user.FindFirst("id")?.Value;

                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);

                if (!string.IsNullOrEmpty(userId))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Id");
                    transformContext.ProxyRequest.Headers.Remove("X-User-Roles");
                    transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                    transformContext.ProxyRequest.Headers.Add("X-User-Roles", string.Join(",", roles));
                }
            }
            else
            {
                transformContext.ProxyRequest.Headers.Remove("X-User-Id");
                transformContext.ProxyRequest.Headers.Remove("X-User-Roles");
            }

            return ValueTask.CompletedTask;
        });
        return context;
    }
}
