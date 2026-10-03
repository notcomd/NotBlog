
namespace Message.Infrastructure.Services;

/// <summary>当前用户服务实现，从 HTTP 上下文或显式设置中解析当前用户标识、角色与声明。</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid _userId;
    private string[] _roles = [];
    private string? _accessToken;

    /// <summary>初始化 <see cref="CurrentUserService"/> 实例。</summary>
    /// <param name="httpContextAccessor">HTTP 上下文访问器。</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>获取当前访问令牌。</summary>
    public string? AccessToken => _accessToken;

    /// <summary>设置当前访问令牌。</summary>
    public void SetAccessToken(string? accessToken) => _accessToken = accessToken;

    /// <summary>获取当前用户是否已认证。</summary>
    public bool IsAuthenticated =>
        _userId != Guid.Empty ||
        (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);

    /// <summary>获取当前用户标识；无法解析时抛出未授权异常。</summary>
    public Guid GetUserId()
    {
        if (_userId != Guid.Empty)
            return _userId;

        var userIdClaim = _httpContextAccessor.HttpContext?.User?
                              .FindFirst("sub")?.Value
                          ?? _httpContextAccessor.HttpContext?.User?
                              .FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? _httpContextAccessor.HttpContext?.User?
                              .FindFirst("user_guid")?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("无效的用户标识");
    }

    /// <summary>获取当前用户的首个角色，不存在时返回 null。</summary>
    public string? GetUserRole()
    {
        if (_roles.Length > 0)
            return _roles[0];

        // Identity 将多个角色以逗号拼接为单个 Role Claim，故需拆分后取首个角色
        var roleClaim = _httpContextAccessor.HttpContext?.User?
            .FindAll(ClaimTypes.Role)
            .SelectMany(c => c.Value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .FirstOrDefault();

        return roleClaim;
    }

    /// <summary>获取指定类型的声明值，不存在时返回 null。</summary>
    public string? GetClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(claimType)?.Value;
    }

    /// <summary>判断当前用户是否具有管理员的角色。</summary>
    public bool IsAdmin()
    {
        if (_roles.Length > 0)
            return _roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);

        // Identity 将多个角色以逗号拼接为单个 Role Claim，故需拆分后判断是否包含 Admin
        var roleClaim = _httpContextAccessor.HttpContext?.User?
            .FindAll(ClaimTypes.Role)
            .SelectMany(c => c.Value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        return roleClaim ?? false;
    }

    /// <summary>显式设置当前用户标识与角色。</summary>
    public void SetUser(Guid userId, string[] roles)
    {
        _userId = userId;
        _roles = roles ?? [];
    }
}
