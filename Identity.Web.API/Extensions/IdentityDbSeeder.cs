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
/// ③ 系统默认角色组：admin_group / user_group / guest_group（已有则跳过）；
/// ④ 默认角色-权限分配：ROOT/ADMIN 全量、USER 排除审计与身份管理、GUEST/UNKNOWN 不分配（已有分配则跳过）；
/// ⑤ 系统管理账号：root（ROOT）+ admin（ADMIN），密码由 CSPRNG 随机生成并经邮件后台队列下发
///    （邮箱已存在则跳过、不重置密码）。
/// </summary>
public class IdentityDbSeeder : IDbSeeder<IdentityDbContext>
{
    private readonly ILogger<IdentityDbSeeder> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMailQueue _mailQueue;

    /// <summary>本次 SeedPermissionsAsync 新增的叶子码（存量库增量补授权用）</summary>
    private readonly List<string> _newlyAddedLeafCodes = new();

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

    public IdentityDbSeeder(ILogger<IdentityDbSeeder> logger, IConfiguration configuration, IMailQueue mailQueue)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _mailQueue = mailQueue ?? throw new ArgumentNullException(nameof(mailQueue));
    }

    public async Task SeedAsync(IdentityDbContext context)
    {
        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedDefaultRoleGroupsAsync(context);
        await SeedRolePermissionsAsync(context);
        await SeedManagementAccountsAsync(context);
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
                if (isLeaf)
                    _newlyAddedLeafCodes.Add(code);
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
            await SeedIncrementalNewPermissionsAsync(context, roles);
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
    /// 增量补授本次新增的叶子码（如 api:turn:read）：
    /// 存量库已有角色分配时主授权逻辑整体跳过（幂等），新码永远不会被授予；
    /// 此处只补「Seeder 本次新建」的 Api 叶子，不触碰任何既有授权——管理员手动撤权不受影响。
    /// ROOT/ADMIN 全量补；USER 沿用排除规则（api:audit:*、api:identity:manage）；GUEST/UNKNOWN 不补。
    /// </summary>
    private async Task SeedIncrementalNewPermissionsAsync(
        IdentityDbContext context, List<Roles> roles)
    {
        if (_newlyAddedLeafCodes.Count == 0)
            return;

        var newPermissions = await context.Permissions
            .Where(p => p.PermissionType == PermissionType.Api && !p.IsDeleted
                        && _newlyAddedLeafCodes.Contains(p.PermissionCode))
            .ToListAsync();
        if (newPermissions.Count == 0)
            return;

        var changed = false;
        foreach (var role in roles)
        {
            var upper = role.RoleCode.ToUpperInvariant();
            if (upper is not ("ROOT" or "ADMIN" or "USER"))
                continue;

            var granted = role.Permissions
                .Select(p => p.PermissionCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var permission in newPermissions)
            {
                if (upper == "USER"
                    && (permission.PermissionCode.StartsWith("api:audit:", StringComparison.OrdinalIgnoreCase)
                        || permission.PermissionCode.Equals("api:identity:manage", StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (granted.Add(permission.PermissionCode))
                {
                    role.Permissions.Add(permission);
                    changed = true;
                }
            }
        }

        if (!changed)
            return;

        await context.SaveChangesAsync();
        _logger.LogInformation(
            "已为默认角色增量补授新增权限码 {Count} 个: {Codes}",
            _newlyAddedLeafCodes.Count, string.Join(", ", _newlyAddedLeafCodes));
    }

    /// <summary>
    /// 初始化系统管理账号（幂等：邮箱已存在则跳过，不重置已有账号密码）：
    ///   root  → ROOT（系统根角色，全量权限）
    ///   admin → ADMIN（管理员角色）
    /// 邮箱可由 InitialAdmin:RootEmail / InitialAdmin:AdminEmail 覆盖（未配置时用上述默认值）；
    /// 开通通知的投递地址由 InitialAdmin:NotifyEmail 指定（未配置时发到账号邮箱本身），
    /// 便于账号使用系统自有地址（普通用户注册不到）而通知投递到真实收件箱。
    /// 密码由 CSPRNG 随机生成，仅在真正新建账号时经邮件后台队列明文下发
    /// （P6：不在数据库事务内做 SMTP 外部 IO），不写入日志；首次登录后请立即修改密码。
    /// </summary>
    private async Task SeedManagementAccountsAsync(IdentityDbContext context)
    {
        var seeds = new (string ConfigKey, string DefaultEmail, string RoleCode)[]
        {
            ("RootEmail", "root@notblog.com", "ROOT"),
            ("AdminEmail", "admin@notblog.com", "ADMIN")
        };

        // 通知投递地址：账号邮箱为系统自有地址（可能收不到信），故支持单独指定收件箱
        var notifyEmail = _configuration["InitialAdmin:NotifyEmail"];

        foreach (var (configKey, defaultEmail, roleCode) in seeds)
        {
            var email = _configuration[$"InitialAdmin:{configKey}"];
            if (string.IsNullOrWhiteSpace(email))
                email = defaultEmail;

            if (await context.Users.AnyAsync(u => u.UserEmail == email))
            {
                _logger.LogInformation("系统管理账号已存在，跳过: {Email}", email);
                continue;
            }

            var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == roleCode);
            if (role is null)
            {
                _logger.LogWarning("未找到 {RoleCode} 角色，跳过系统管理账号初始化: {Email}", roleCode, email);
                continue;
            }

            var password = JwtRandom.GenerateComplexPassword();
            var user = await User.CreateByEmailUser(role.RoleGuid, email, password, null, null);
            await context.Users.AddAsync(user);
            // 与邮箱注册流程一致：角色侧登记该用户，保证角色↔用户双向可查
            role.AddUserGuid(user.UserGuid);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // 并发初始化同邮箱：UserEmail 唯一索引兜底
                _logger.LogWarning("系统管理账号初始化并发冲突（邮箱已存在），跳过: {Email}", email);
                context.ChangeTracker.Clear();
                continue;
            }

            // P6：初始密码经邮件后台队列明文下发，不落日志；主题带角色便于同收件箱区分
            var target = string.IsNullOrWhiteSpace(notifyEmail) ? email : notifyEmail;
            _mailQueue.Enqueue(target, $"[NotBlog] 系统管理账号开通通知（{role.RoleName}）",
                $"您的系统管理账号已创建：{email}（角色 {role.RoleName}）。初始密码：{password}，请登录后立即修改密码。");
            _logger.LogInformation("已创建系统管理账号: {Email}（角色 {RoleCode}），初始密码已入队邮件下发至 {Notify}",
                email, roleCode, target);
        }
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
