using System.Security.Claims;
using Video.Domain.IServices;

namespace Video.Web.API.Services;

/// <summary>
/// 从 HttpContext.User 的 Claim 解析当前用户 Guid。
/// 兼容 Identity 签发的 JWT：优先 user_guid，其次 NameIdentifier(sub)。
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserGuid
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return Guid.Empty;

            var claim = user.FindFirst("user_guid")
                        ?? user.FindFirst(ClaimTypes.NameIdentifier)
                        ?? user.FindFirst("sub");

            return claim is not null && Guid.TryParse(claim.Value, out var userGuid)
                ? userGuid
                : Guid.Empty;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
