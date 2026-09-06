using Identity.Domain.Entities.RoleAggregate;
using Identity.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.Web.API.Extensions;

/// <summary>
/// Identity 数据库种子数据初始化器。
/// 启动时自动完成（均幂等，重复运行安全）：
/// ① 权限映射初始数据：appsettings PermissionMappings → Permissions 表（按 Code 去重补齐，
///    并按 code 段前缀自动构建权限树：目录节点 PermissionType=Menu，叶子 PermissionType=Api）；
/// ② 系统默认角色（已有角色则跳过）；
/// ③ 默认角色-权限分配：ROOT/ADMIN 全量、USER 排除审计与身份管理、GUEST/UNKNOWN 不分配（已有分配则跳过）。
/// </summary>
public class IdentityDbSeeder : IDbSeeder<IdentityDbContext>
{
    private readonly ILogger<IdentityDbSeeder> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>权限码 → 中文名（资源段）</summary>
    private static readonly IReadOnlyDictionary<string, string> ResourceNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tweet"] = "推文", ["session"] = "会话", ["identity"] = "身份", ["file"] = "文件",
            ["markdown"] = "文章", ["video"] = "视频", ["videocollection"] = "视频收藏",
            ["addvideo"] = "视频上传", ["videowatch"] = "观看统计", ["videoreview"] = "视频评论",
            ["videobarrage"] = "弹幕", ["videostream"] = "视频流", ["comment"] = "评论",
            ["report"] = "举报", ["audit"] = "审计", ["friend"] = "好友", ["group"] = "群组",
            ["message"] = "消息", ["circle"] = "圈子", ["topic"] = "话题", ["follow"] = "关注",
            ["userinfo"] = "用户资料", ["notification"] = "通知", ["favorite"] = "收藏", ["email"] = "邮箱"
        };

    /// <summary>权限码 → 中文名（动作段）</summary>
    private static readonly IReadOnlyDictionary<string, string> ActionNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["read"] = "读取", ["create"] = "创建", ["update"] = "更新", ["delete"] = "删除", ["manage"] = "管理"
        };

    public IdentityDbSeeder(ILogger<IdentityDbSeeder> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task SeedAsync(IdentityDbContext context)
    {
        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedDefaultRoleGroupsAsync(context);
        await SeedRolePermissionsAsync(context);
        await SeedInitialAdminAsync(context);
    }

    /// <summary>
    /// 权限映射初始数据：appsettings PermissionMappings → Permissions 表（按 Code 幂等补齐），
    /// 并按 code 段前缀自动构建权限树：
    ///   api:tweet:read → api(Menu 根) → api:tweet(Menu 目录) → api:tweet:read(Api 叶子)
    /// 幂等规则：只补不删；已存在节点不动其名称/类型；已存在但未挂父（ParentId 为 null）的
    /// 节点按 code 前缀补挂到目录下——目录已存在而叶子为空挂父属首次建树，此后不再变动。
    /// </summary>
    private async Task SeedPermissionsAsync(IdentityDbContext context)
    {
        var mappings = _configuration.GetSection("PermissionMappings").Get<List<PermissionMappingItem>>();
        if (mappings is null || mappings.Count == 0)
        {
            _logger.LogWarning("未配置 PermissionMappings 节，跳过权限初始数据");
            return;
        }

        var codes = mappings
            .Select(m => m.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (codes.Count == 0)
            return;

        // 收集全部需要存在的节点：叶子码 + 其每级段前缀（目录）
        // 例 api:tweet:read → [api, api:tweet] 为目录，api:tweet:read 为叶子
        var required = new List<(string Code, bool IsLeaf, int SortOrder)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in codes)
        {
            var segments = code.Split(':');
            for (var depth = 1; depth <= segments.Length; depth++)
            {
                var prefix = string.Join(':', segments.Take(depth));
                if (seen.Add(prefix))
                    required.Add((prefix, depth == segments.Length, required.Count));
            }
        }

        // ⚠️ 查询范围必须覆盖 required 中的全部 code（目录节点含段前缀，不在叶子 codes 集合内），
        // 否则目录节点每次启动都被当作「不存在」重复创建，破坏幂等
        var allCodes = required.Select(r => r.Code).ToList();
        var existing = await context.Permissions
            .Where(p => allCodes.Contains(p.PermissionCode))
            .ToListAsync();
        var byCode = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in existing)
            byCode[e.PermissionCode] = e;

        var toAdd = new List<Permission>();
        var addedCount = 0;
        var linkedCount = 0;

        // required 已按段深升序（父先于子），逐节点补齐并挂接
        foreach (var (code, isLeaf, sortOrder) in required)
        {
            if (!byCode.TryGetValue(code, out var node))
            {
                var type = isLeaf ? PermissionType.Api : PermissionType.Menu;
                var parent = ResolveParentCode(code);
                var permission = new Permission(
                    code,
                    ResolvePermissionName(code),
                    type,
                    parent is not null && byCode.TryGetValue(parent, out var parentNode) ? parentNode.PermissionId : null,
                    null, null, sortOrder);
                byCode[code] = permission;
                toAdd.Add(permission);
                addedCount++;
            }
            else if (!node.IsDeleted && node.ParentId is null)
            {
                // 已存在但未挂父：按 code 前缀补挂（父目录需已存在——required 段深升序保证其先被处理）
                var parent = ResolveParentCode(code);
                if (parent is not null && byCode.TryGetValue(parent, out var parentNode))
                {
                    node.ChangeParent(parentNode.PermissionId);
                    linkedCount++;
                }
            }
        }

        if (toAdd.Count > 0)
        {
            await context.Permissions.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
        }
        else if (linkedCount > 0)
        {
            await context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "权限初始数据就绪：共 {Total} 个权限码，本次新增 {Added} 个节点，补挂父节点 {Linked} 个",
            codes.Count, addedCount, linkedCount);
    }

    /// <summary>取 code 的父目录 code（去掉末段；无父返回 null）</summary>
    private static string? ResolveParentCode(string code)
    {
        var idx = code.LastIndexOf(':');
        return idx <= 0 ? null : code[..idx];
    }

    /// <summary>系统默认角色（已存在则跳过）</summary>
    private async Task SeedRolesAsync(IdentityDbContext context)
    {
        var existingCount = await context.Roles.CountAsync();
        if (existingCount > 0)
        {
            _logger.LogInformation("数据库中已存在 {Count} 个角色，跳过角色种子初始化", existingCount);
            return;
        }

        var defaultRoles = Roles.RoleFactory.GetDefaultRoles();
        await context.Roles.AddRangeAsync(defaultRoles);
        await context.SaveChangesAsync();

        _logger.LogInformation("已创建 {Count} 个系统默认角色: {Roles}",
            defaultRoles.Count,
            string.Join(", ", defaultRoles.Select(r => $"{r.RoleName}({r.RoleCode})")));
    }

    /// <summary>
    /// 默认角色组（幂等：组 code 已存在则跳过，不覆盖用户手改）：
    ///   管理员组（ROOT/ADMIN）、普通用户组（USER）、访客组（GUEST/UNKNOWN）
    /// 角色与组双向挂接（Roles.RoleGroupGuids 与 RoleGroup.RoleGuids 同步）。
    ///
    /// ⚠️ 组权限保持为空——权限单一来源是「角色直连」（SeedRolePermissionsAsync +
    /// 管理端 PUT /role/{roleId}/permissions 树形授权）。
    /// 若默认组也授与角色相同的权限，会产生双通道授权：管理端缩减角色权限时
    /// 组继承仍持有旧权限，撤权失效且难排查。组级授权留给未来专门的组授权端点。
    /// </summary>
    private async Task SeedDefaultRoleGroupsAsync(IdentityDbContext context)
    {
        var existingCodes = await context.RoleGroups
            .Where(g => !g.IsDeleted)
            .Select(g => g.RoleGroupCode)
            .ToListAsync();
        var existing = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        if (existing.Contains("admin_group") && existing.Contains("user_group") && existing.Contains("guest_group"))
        {
            _logger.LogInformation("默认角色组已就绪，跳过");
            return;
        }

        var roles = await context.Roles
            .Where(r => !r.IsDeleted && r.RoleStatus == RoleStatus.Normal)
            .ToListAsync();

        // 组定义：code → (名称, 默认角色 code)
        var groups = new (string Code, string Name, string[] RoleCodes)[]
        {
            ("admin_group", "管理员组", ["ROOT", "ADMIN"]),
            ("user_group", "普通用户组", ["USER"]),
            ("guest_group", "访客组", ["GUEST", "UNKNOWN"])
        };

        var created = 0;
        foreach (var (code, name, roleCodes) in groups)
        {
            if (existing.Contains(code))
                continue;

            var roleGroup = new RoleGroup(name, code);
            foreach (var role in roles.Where(r => roleCodes.Contains(r.RoleCode, StringComparer.OrdinalIgnoreCase)))
            {
                roleGroup.AddRole(role.RoleGuid);
                role.AddToRoleGroup(roleGroup.RoleGroupGuid);
            }

            context.RoleGroups.Add(roleGroup);
            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync();
            _logger.LogInformation("已创建 {Count} 个默认角色组", created);
        }
    }

    /// <summary>默认角色-权限分配（已有任何分配则跳过）：ROOT/ADMIN 全量、USER 排除审计与身份管理、GUEST/UNKNOWN 不分配</summary>
    private async Task SeedRolePermissionsAsync(IdentityDbContext context)
    {
        var roles = await context.Roles
            .Include(r => r.Permissions)
            .ToListAsync();
        if (roles.Count == 0)
            return;

        if (roles.Any(r => r.Permissions.Count > 0))
        {
            _logger.LogInformation("角色-权限分配已存在，跳过");
            return;
        }

        var permissions = await context.Permissions.ToListAsync();
        if (permissions.Count == 0)
        {
            _logger.LogWarning("Permissions 表为空，跳过角色-权限分配");
            return;
        }

        foreach (var role in roles)
        {
            switch (role.RoleCode.ToUpperInvariant())
            {
                case "ROOT":
                case "ADMIN":
                case "USER":
                    // ⚠️ 只授叶子（PermissionType.Api）：目录/菜单节点不直接入库授权——
                    // 否则 "api:audit" 目录码不匹配 StartsWith("api:audit:") 排除规则，
                    // 前缀段匹配会让 USER 放行全部审计权限（权限提升漏洞）。
                    // 目录级授权留给管理端按树勾选（RolePermissions 存目录码 + 前缀匹配自动覆盖子孙）。
                    foreach (var p in permissions)
                    {
                        if (p.PermissionType != PermissionType.Api)
                            continue;
                        if (role.RoleCode.ToUpperInvariant() == "USER"
                            && (p.PermissionCode.StartsWith("api:audit:", StringComparison.OrdinalIgnoreCase)
                                || p.PermissionCode.Equals("api:identity:manage", StringComparison.OrdinalIgnoreCase)))
                            continue;
                        role.Permissions.Add(p);
                    }
                    break;

                default: // GUEST / UNKNOWN：不分配
                    break;
            }
        }

        await context.SaveChangesAsync();
        var stats = roles.Select(r => $"{r.RoleName}:{r.Permissions.Count}").ToList();
        _logger.LogInformation("已初始化角色-权限分配: {Stats}", string.Join(", ", stats));
    }

    /// <summary>
    /// 初始化管理用户：配置 InitialAdmin:Email + 密码（InitialAdmin:Password 或环境变量
    /// INITIAL_ADMIN_PASSWORD，S-01 凭据外置）；邮箱已存在则跳过（幂等）。
    /// </summary>
    private async Task SeedInitialAdminAsync(IdentityDbContext context)
    {
        var email = _configuration["InitialAdmin:Email"];
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogInformation("未配置 InitialAdmin:Email，跳过初始化管理用户");
            return;
        }

        var password = _configuration["InitialAdmin:Password"]
                       ?? Environment.GetEnvironmentVariable("INITIAL_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "未配置初始化管理员密码（InitialAdmin:Password 或环境变量 INITIAL_ADMIN_PASSWORD），跳过管理用户初始化: {Email}",
                email);
            return;
        }

        var exists = await context.Users.AnyAsync(u => u.UserEmail == email);
        if (exists)
        {
            _logger.LogInformation("初始化管理用户已存在，跳过: {Email}", email);
            return;
        }

        var rootRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "ROOT" );
        if (rootRole is null)
        {
            _logger.LogWarning("未找到ROOT 角色，跳过初始化管理用户: {Email}", email);
            return;
        }

        var user = await User.CreateByEmailUser(rootRole.RoleGuid, email, password, null, null);
        await context.Users.AddAsync(user);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 并发初始化同邮箱：UserEmail 唯一索引兜底
            _logger.LogWarning("初始化管理用户并发冲突（邮箱已存在），跳过: {Email}", email);
            return;
        }

        _logger.LogInformation("已创建初始化管理用户: {Email}（角色 Root）", email);
    }

    /// <summary>
    /// 权限码 → 中文名：
    ///   api → 接口（根目录）；api:tweet → 推文（模块目录）；api:tweet:read → 推文-读取（叶子）
    /// </summary>
    private static string ResolvePermissionName(string code)
    {
        var parts = code.Split(':');
        if (parts.Length == 1)
            return parts[0].Equals("api", StringComparison.OrdinalIgnoreCase) ? "接口" : parts[0].ToUpperInvariant();
        if (parts.Length == 2)
            return ResourceNames.TryGetValue(parts[1], out var rn) ? rn : parts[1];

        var resourceName = ResourceNames.TryGetValue(parts[1], out var resource) ? resource : parts[1];
        var actionName = ActionNames.TryGetValue(parts[^1], out var action) ? action : parts[^1];
        return $"{resourceName}-{actionName}";
    }

    /// <summary>appsettings PermissionMappings 配置项</summary>
    private sealed record PermissionMappingItem
    {
        public string Method { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }
}
