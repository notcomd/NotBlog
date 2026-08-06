using Identity.Domain.Entities.PermissionAggregate;
using Microsoft.Extensions.Caching.Memory;

namespace Identity.Infrastructure.Services;

/// <summary>
/// 权限检查器 — 核心权限判定逻辑实现
/// 
/// 判定流程:
///   1. 从 User 获取 UserRoleGuid 列表
///   2. 查询匹配的 Roles（过滤已删除/禁用）
///   3. 收集直连 Permissions（过滤已删除）
///   4. 从 Roles 中收集 RoleGroupGuids → 查询 RoleGroups → Permissions（组继承，过滤已删除）
///   5. 合并去重 → 判断是否包含目标 permission_code
/// 
/// 数据范围判定:
///   Root/Admin 角色 → DataScopeType.All
///   其他角色 → DataScopeType.Own
/// 
/// 性能（P4）：权限集合与数据范围结果做 1 秒进程内缓存（网关每请求调用，原实现每请求 2~4 次 DB 查询）。
/// 1 秒 TTL 意味着权限变更最多 1 秒后生效，无需主动失效；多实例各自缓存，延迟同样 ≤1 秒。
/// </summary>
public class PermissionChecker : IPermissionChecker
{
    /// <summary>权限集合缓存前缀</summary>
    private const string PermCachePrefix = "identity:perm:";

    /// <summary>数据范围缓存前缀</summary>
    private const string ScopeCachePrefix = "identity:scope:";

    /// <summary>缓存 TTL：1 秒（权限变更最多延迟 1 秒生效）</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(1);

    private readonly IdentityDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionChecker> _logger;

    public PermissionChecker(IdentityDbContext db, IMemoryCache cache, ILogger<PermissionChecker> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> CheckPermissionAsync(
        Guid userId, string permissionCode, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("[PermissionChecker] CheckPermission 参数无效: userId 为空");
            return false;
        }

        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            _logger.LogWarning("[PermissionChecker] CheckPermission 参数无效: permissionCode 为空, UserId={UserId}", userId);
            return false;
        }

        try
        {
            // P4：统一走缓存后的权限集合（直连 + 组继承，去重），一次缓存命中替代多次 DB 查询
            var permissions = await GetUserPermissionsAsync(userId, ct);
            var hasPermission = permissions.Contains(permissionCode);

            _logger.LogDebug(
                "[PermissionChecker] Result={Result} UserId={UserId} Code={PermissionCode}",
                hasPermission, userId, permissionCode);

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PermissionChecker] CheckPermission 异常 UserId={UserId} Code={PermissionCode}",
                userId, permissionCode);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(
        Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("[PermissionChecker] GetUserPermissions 参数无效: userId 为空");
            return new HashSet<string>();
        }

        try
        {
            var cacheKey = $"{PermCachePrefix}{userId}";
            if (_cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached is not null)
                return cached;

            var userRoles = await GetUserActiveRolesAsync(userId, ct);
            if (userRoles is null || userRoles.Count == 0)
            {
                _logger.LogDebug("[PermissionChecker] GetUserPermissions 用户无角色 UserId={UserId}", userId);
                return new HashSet<string>();
            }

            // 1. 角色直连权限
            var directCodes = userRoles
                .SelectMany(r => r.Permissions)
                .Where(p => !p.IsDeleted)
                .Select(p => p.PermissionCode);

            // 2. 角色组继承权限
            var roleGroupGuids = userRoles
                .SelectMany(r => r.RoleGroupGuids)
                .ToHashSet();

            var groupCodes = roleGroupGuids.Count > 0
                ? await _db.RoleGroups
                    .AsNoTracking()
                    .Where(g => roleGroupGuids.Contains(g.RoleGroupGuid)
                                && !g.IsDeleted)
                    .SelectMany(g => g.Permissions
                        .Where(p => !p.IsDeleted)
                        .Select(p => p.PermissionCode))
                    .ToListAsync(ct)
                : [];

            var allCodes = new HashSet<string>(directCodes);
            foreach (var code in groupCodes)
                allCodes.Add(code);

            _logger.LogDebug(
                "[PermissionChecker] GetUserPermissions UserId={UserId} Direct={DirectCount} Group={GroupCount} Total={TotalCount}",
                userId, directCodes.Count(), groupCodes.Count, allCodes.Count);

            _cache.Set(cacheKey, allCodes, CacheTtl);
            return allCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PermissionChecker] GetUserPermissions 异常 UserId={UserId}", userId);
            return new HashSet<string>();
        }
    }

    /// <inheritdoc />
    public async Task<DataScope> GetUserDataScopeAsync(
        Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("[PermissionChecker] GetUserDataScope 参数无效: userId 为空");
            return DataScope.Own();
        }

        try
        {
            var cacheKey = $"{ScopeCachePrefix}{userId}";
            if (_cache.TryGetValue(cacheKey, out DataScope? cached) && cached is not null)
                return cached;

            var userRoleGuids = await GetUserRoleGuidsAsync(userId, ct);
            if (userRoleGuids is null || userRoleGuids.Count == 0)
            {
                _logger.LogDebug("[PermissionChecker] GetUserDataScope 用户无角色 UserId={UserId}", userId);
                return DataScope.Own();
            }

            var roleAuthorities = await _db.Roles
                .AsNoTracking()
                .Where(r => userRoleGuids.Contains(r.RoleGuid)
                            && !r.IsDeleted
                            && r.RoleStatus == RoleStatus.Normal)
                .Select(r => r.RoleAuthority)
                .ToListAsync(ct);

            DataScope result;
            if (roleAuthorities.Count == 0)
            {
                result = DataScope.Own();
            }
            else if (roleAuthorities.Contains(RoleAuthority.Root) ||
                     roleAuthorities.Contains(RoleAuthority.Admin))
            {
                _logger.LogDebug("[PermissionChecker] GetUserDataScope UserId={UserId} Scope=All", userId);
                result = DataScope.All();
            }
            else
            {
                _logger.LogDebug("[PermissionChecker] GetUserDataScope UserId={UserId} Scope=Own", userId);
                result = DataScope.Own();
            }

            _cache.Set(cacheKey, result, CacheTtl);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PermissionChecker] GetUserDataScope 异常 UserId={UserId}", userId);
            return DataScope.Own();
        }
    }

    /// <summary>
    /// 从 User 表获取角色 GUID 列表
    /// </summary>
    private async Task<List<Guid>?> GetUserRoleGuidsAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserGuid == userId)
            .Select(u => new { u.UserRoleGuid })
            .FirstOrDefaultAsync(ct);

        return user?.UserRoleGuid;
    }

    /// <summary>
    /// 获取用户的有效角色（含直连权限，已过滤已删除/禁用角色）
    /// </summary>
    private async Task<List<Roles>> GetUserActiveRolesAsync(Guid userId, CancellationToken ct)
    {
        var userRoleGuids = await GetUserRoleGuidsAsync(userId, ct);
        if (userRoleGuids is null || userRoleGuids.Count == 0)
            return [];

        return await _db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => userRoleGuids.Contains(r.RoleGuid)
                        && !r.IsDeleted
                        && r.RoleStatus == RoleStatus.Normal)
            .ToListAsync(ct);
    }
}
