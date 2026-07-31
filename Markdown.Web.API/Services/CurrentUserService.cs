using System.Security.Claims;

namespace Markdown.Web.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
                              .FindFirst(ClaimTypes.Email)?.Value
                          ?? _httpContextAccessor.HttpContext?.User?
                              .FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("无效的用户标识");
    }

    public string? GetUserRole()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Role)?.Value;
    }

    public string? GetClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(claimType)?.Value;
    }
}
