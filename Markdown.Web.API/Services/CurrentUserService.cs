using System.Security.Claims;
using Commons.Security;

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
        // Identity 签发的 JWT 将用户 id 放入 ClaimTypes.NameIdentifier（序列化为 nameid），
        // 同时兼容 sub / user_guid 等第三方或历史 claim 名
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
                              .FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? _httpContextAccessor.HttpContext?.User?
                              .FindFirst("sub")?.Value
                          ?? _httpContextAccessor.HttpContext?.User?
                              .FindFirst("user_guid")?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("无效的用户标识");
    }

    public string? GetUserRole()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// 判断当前用户是否为管理员。
    /// 统一口径：兼容 Root / Administrator / Admin（大小写不敏感），并支持逗号拼接的多角色 claim，
    /// 详见 <see cref="AdminRoleExtensions"/>（原实现认 "Admin"/"Root" 而漏 "Administrator"）。
    /// </summary>
    public bool IsAdmin() =>
        _httpContextAccessor.HttpContext?.User.HasAdminRole() ?? false;

    public string? GetClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(claimType)?.Value;
    }
}
