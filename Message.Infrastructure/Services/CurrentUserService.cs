using System.Security.Claims;
using Message.Domain.IServices;
using Microsoft.AspNetCore.Http;

namespace Message.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid GetUserId()
    {
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
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Role)?.Value;
    }

    public string? GetClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(claimType)?.Value;
    }
}