
namespace Video.Infrastructure.Service;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{

    private Guid _userId;
    private string[] _roles = Array.Empty<string>();
    private string? _accessToken;
    public string? AccessToken => _accessToken;
    public bool IsAuthenticated => _userId != Guid.Empty;

    public Guid GetUserId()
    {
        if(_userId != Guid.Empty)
            return _userId;
        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                          ?? httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? httpContextAccessor.HttpContext?.User?.FindFirst("user_guid")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : throw new UnauthorizedAccessException("无效的用户标识");
    }

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

    public string? GetClaim(string claimType)
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
    }

    public bool IsAdmin()
    {
        if (_roles.Length > 0)
            return _roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);

        // Identity 将多个角色以逗号拼接为单个 Role Claim，故需拆分后判断是否包含 Admin
        var roleClaim = httpContextAccessor.HttpContext?.User?
            .FindAll(System.Security.Claims.ClaimTypes.Role)
            .SelectMany(c => c.Value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        return roleClaim ?? false;
    }

    public void SetUser(Guid userId, string[] roles)
    {
        _userId = userId;
        _roles = roles;
    }



    public void SetAccessToken(string? accessToken)
    {
        _accessToken = accessToken;
    }
}