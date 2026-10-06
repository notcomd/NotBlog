using Commons.Security;

namespace Video.Infrastructure.Services;

/// <summary>
/// 当前登录用户服务实现 — 优先使用网关中间件注入的用户上下文（SetUser/SetAccessToken），
/// 未注入时回退解析 HttpContext 的 JWT Claim（sub/NameIdentifier/user_guid）。
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{

    private Guid _userId;
    private string[] _roles = Array.Empty<string>();
    private string? _accessToken;

    /// <summary>当前请求/连接的原始 Bearer token。</summary>
    public string? AccessToken => _accessToken;

    /// <summary>当前请求是否已认证。</summary>
    public bool IsAuthenticated => _userId != Guid.Empty;

    /// <summary>获取当前登录用户 Guid；无法解析时抛 UnauthorizedAccessException。</summary>
    public Guid GetUserId()
    {
        if(_userId != Guid.Empty)
            return _userId;
        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                          ?? httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? httpContextAccessor.HttpContext?.User?.FindFirst("user_guid")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : throw new UnauthorizedAccessException("无效的用户标识");
    }

    /// <summary>获取当前用户首个角色名（Identity 以逗号拼接多角色）；无角色返回 null。</summary>
    public string? GetUserRole()
    {
        if (_roles.Length > 0)
            return _roles[0];

        // Identity 将多个角色以逗号拼接为单个 Role Claim，故需拆分后取首个角色
        var roleClaim = httpContextAccessor.HttpContext?.User?
            .FindAll(System.Security.Claims.ClaimTypes.Role)
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .FirstOrDefault();

        return roleClaim;
    }

    /// <summary>读取当前用户指定 Claim 的值；不存在返回 null。</summary>
    public string? GetClaim(string claimType)
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
    }

    /// <summary>
    /// 判断当前用户是否具备管理员角色。
    /// 统一口径：兼容 Root / Administrator / Admin（大小写不敏感），并支持逗号拼接的多角色 claim，
    /// 详见 <see cref="AdminRoleExtensions"/>（原实现仅认 "Admin"，导致密码登录的 Administrator 账号被误判 403）。
    /// </summary>
    public bool IsAdmin()
    {
        if (_roles.Length > 0)
            return _roles.ContainsAdminRole();
        return httpContextAccessor.HttpContext?.User.HasAdminRole() ?? false;
    }

    /// <summary>由网关 Middleware 注入当前用户上下文（用户标识 + 角色）。</summary>
    public void SetUser(Guid userId, string[] roles)
    {
        _userId = userId;
        _roles = roles;
    }



    /// <summary>设置当前请求/连接的原始 Bearer token（供转发到 FileDev gRPC 认证）。</summary>
    public void SetAccessToken(string? accessToken)
    {
        _accessToken = accessToken;
    }
}