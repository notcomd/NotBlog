
namespace Message.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid _userId;
    private string[] _roles = [];
    private string? _accessToken;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? AccessToken => _accessToken;

    public void SetAccessToken(string? accessToken) => _accessToken = accessToken;

    public bool IsAuthenticated =>
        _userId != Guid.Empty ||
        (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);

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

    public string? GetClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(claimType)?.Value;
    }

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

    public void SetUser(Guid userId, string[] roles)
    {
        _userId = userId;
        _roles = roles ?? [];
    }
}
