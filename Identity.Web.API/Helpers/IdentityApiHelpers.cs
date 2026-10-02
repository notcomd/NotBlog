using System.Security.Claims;

namespace Identity.Web.API.Helpers;

/// <summary>
/// Identity HTTP API 的共享静态辅助方法
/// （GetIdempotencyKey 原在 IdentityApis、TryGetAuthenticatedUserId 原在 OAuthApis、
/// Bearer Token 解析原散落在 IdentityAuthApi 的 Logout/ChangeByPassword，统一收敛至此）。
/// </summary>
internal static class IdentityApiHelpers
{
    /// <summary>
    /// S-14：幂等键由客户端显式传入（请求头 X-Idempotency-Key）。
    /// 缺失或非合法 GUID 时回退为随机键（该请求无幂等保证，不影响其他请求）。
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>客户端幂等键；未提供或非法时回退随机键</returns>
    internal static Guid GetIdempotencyKey(HttpContext context)
    {
        var header = context.Request.Headers["X-Idempotency-Key"].ToString();
        return Guid.TryParse(header, out var key) ? key : Guid.CreateVersion7();
    }

    /// <summary>
    /// S-13：统一 userId Claim 为 NameIdentifier（不再信任 user_id / user_guid 残留 Claim）。
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>已认证用户 ID；未认证或格式非法时返回 null</returns>
    internal static Guid? TryGetAuthenticatedUserId(HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }

    /// <summary>
    /// 从 Authorization 请求头解析 Bearer Token。
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>Bearer Token；缺失或格式不符时返回 null</returns>
    internal static string? GetBearerToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        return authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..].Trim()
            : null;
    }
}