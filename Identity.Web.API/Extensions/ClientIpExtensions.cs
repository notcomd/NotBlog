using System.Net;

namespace Identity.Web.API.Extensions;

/// <summary>
/// 客户端 IP 解析（P9：修复限流在 YARP 网关后失效的问题）
///
/// 部署形态：客户端 → YARP 网关 → Identity。网关透传/追加 X-Forwarded-For，
/// 取首个 IP（最接近客户端、由可信代理链写入的地址）。
/// 注意：直连服务（绕过网关）时 X-Forwarded-For 可被客户端伪造，生产环境必须将本服务
/// 只暴露在网关之后。
/// </summary>
public static class ClientIpExtensions
{
    public static string GetClientIp(this HttpContext httpContext)
    {
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first) && IPAddress.TryParse(first, out _))
                return first;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
