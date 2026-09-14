using System.Net;

namespace Identity.Web.API.Extensions;


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
