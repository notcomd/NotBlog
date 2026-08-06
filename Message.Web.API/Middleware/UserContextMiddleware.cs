
namespace Message.Web.API.Middleware;
/// <summary>
/// 用户上下文中间件。
/// <para>
/// 安全策略（修复 S-02）：身份<b>仅</b>来自已验证 JWT 的 Claim（sub / NameIdentifier / user_guid），
/// <b>不再无条件信任</b>客户端 X-User-Id / X-User-Roles 请求头，杜绝绕过网关直连端口时的身份伪造。
/// - 未认证（无有效 JWT）时不做任何身份注入，受保护端点由 [Authorize] 统一返回 401；
/// - 已认证时，X-User-Id 头仅在网关注入且与 JWT Claim 一致时视为冗余信息，不一致时忽略并记录告警；
/// - 角色仅从 JWT Role Claim 解析（Identity 将多个角色以逗号拼接为单个 claim，故需拆分）。
/// </para>
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            // 身份来源必须是已验证 JWT 的 Claim（兼容 sub / NameIdentifier / user_guid 三种签发方式）
            var userIdClaim = user.FindFirst("sub")?.Value
                              ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("user_guid")?.Value;

            if (Guid.TryParse(userIdClaim, out var userGuid))
            {
                // 网关注入的 X-User-Id 与 JWT 身份不一致 → 忽略请求头，仅以 JWT 为准（防直连伪造）
                if (Guid.TryParse(context.Request.Headers["X-User-Id"].FirstOrDefault(), out var headerGuid)
                    && headerGuid != userGuid)
                {
                    _logger.LogWarning(
                        "请求头 X-User-Id={HeaderGuid} 与 JWT 身份 {UserGuid} 不一致，已忽略请求头",
                        headerGuid, userGuid);
                }

                // 角色仅从 JWT Role Claim 解析（Identity 将角色列表以逗号拼接为单个 claim，故需拆分）
                var roles = user.FindAll(ClaimTypes.Role)
                    .SelectMany(c => c.Value.Split(',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                currentUser.SetUser(userGuid, roles);
            }
        }

        await _next(context);
    }
}

