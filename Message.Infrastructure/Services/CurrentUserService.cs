using System.Security.Claims;
using Message.Domain.IServices;
using Microsoft.AspNetCore.Http;

namespace Message.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid _userId;
    private string[] _roles = [];

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

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
                              .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("无效的用户标识");
    }

    public string? GetUserRole()
    {
        if (_roles.Length > 0)
            return _roles[0];

        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Role)?.Value;
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

        var roleClaim = _httpContextAccessor.HttpContext?.User?
            .FindAll(ClaimTypes.Role)
            .Any(c => c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        return roleClaim ?? false;
    }

    public void SetUser(Guid userId, string[] roles)
    {
        _userId = userId;
        _roles = roles ?? [];
    }
}
