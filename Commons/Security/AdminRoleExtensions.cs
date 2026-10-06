using System.Security.Claims;

namespace Commons.Security;

/// <summary>
/// 管理员角色判定扩展（全站统一口径）。
/// <para>
/// 口径说明：Identity 密码登录 / OAuth 授权服务器签发的 role claim 取「角色名 RoleName」
/// （见 <c>Roles.RoleFactory</c> / <c>IdentityDbSeeder</c>）：ROOT → <c>Root</c>、
/// ADMIN → <c>Administrator</c>、USER → <c>User</c>；而 OAuth 登录历史上曾签发短名 <c>Admin</c>。
/// 为兼容两种签发来源与已签发的历史 token，管理端判定统一同时接受
/// <c>Root</c> / <c>Administrator</c> / <c>Admin</c> 三种取值（大小写不敏感），
/// 并兼容单个 role claim 以逗号拼接多角色的情况（Identity 密码登录即此形式）。
/// </para>
/// <para>
/// 注意：判定依据是「角色名」，不是角色 Code（<c>ROOT</c>/<c>ADMIN</c>），不要用 Code 形式比较。
/// </para>
/// </summary>
public static class AdminRoleExtensions
{
    /// <summary>系统根角色名（ROOT 角色的 RoleName）。</summary>
    public const string RootRoleName = "Root";

    /// <summary>管理员角色名（ADMIN 角色的 RoleName，密码登录 / OAuth 授权服务器签发值）。</summary>
    public const string AdministratorRoleName = "Administrator";

    /// <summary>管理员角色历史短名（OAuth 登录曾签发，仅用于兼容历史 token 的判定）。</summary>
    public const string LegacyAdminRoleName = "Admin";

    /// <summary>视为管理员的角色名集合（Root / Administrator 为正式名，Admin 为历史短名）。</summary>
    private static readonly string[] AdminRoleNames =
        [RootRoleName, AdministratorRoleName, LegacyAdminRoleName];

    /// <summary>判断单个角色名是否为管理员角色（大小写不敏感；RoleName 口径，非 RoleCode）。</summary>
    public static bool IsAdminRoleName(string? roleName) =>
        !string.IsNullOrWhiteSpace(roleName) &&
        Array.Exists(AdminRoleNames, n => n.Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>判断角色名集合是否包含任一管理员角色（大小写不敏感）。</summary>
    public static bool ContainsAdminRole(this IEnumerable<string>? roles) =>
        roles is not null && roles.Any(IsAdminRoleName);

    /// <summary>
    /// 从 <see cref="ClaimsPrincipal"/> 的 Role claim 判断当前用户是否为管理员；
    /// 单个 claim 值可为逗号拼接的多角色，逐个拆分后判定（与 Identity 多角色签发形式一致）。
    /// </summary>
    public static bool HasAdminRole(this ClaimsPrincipal? principal)
    {
        if (principal is null)
            return false;

        return principal.FindAll(ClaimTypes.Role)
            .SelectMany(c => c.Value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(IsAdminRoleName);
    }
}
