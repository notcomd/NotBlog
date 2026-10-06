using Identity.Domain.Entities.MenuAggregate;
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
        await SeedMenusAsync(context);
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

    /// <summary>
    /// 管理端默认菜单（幂等：以 Url 为键，已存在则跳过，不覆盖管理端改动）。
    /// 与前端管理端现有页面一一对应，作为菜单管理功能的初始数据。
    /// </summary>
    private async Task SeedMenusAsync(IdentityDbContext context)
    {
        var defaults = new (string Url, string Name, string Icon, int Sort)[]
        {
            ("/admin", "工作台", "dashboard", 10),
            ("/admin/users", "用户管理", "users", 20),
            ("/admin/content", "内容管理", "content", 30),
            ("/admin/reports", "举报管理", "reports", 40),
            ("/admin/circles", "社区管理", "circles", 50),
            ("/admin/files", "文件管理", "files", 60),
            ("/admin/announcements", "公报", "announcements", 70),
            ("/admin/menus", "菜单管理", "menus", 80),
            ("/admin/permissions", "权限管理", "permissions", 90),
            ("/admin/security", "账号安全", "security", 100)
        };

        var urls = defaults.Select(d => d.Url).ToList();
        var existing = await context.Menus
            .Where(m => urls.Contains(m.Url!))
            .Select(m => m.Url!)
            .ToListAsync();
        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var created = 0;
        foreach (var (url, name, icon, sort) in defaults)
        {
            if (existingSet.Contains(url))
                continue;

            context.Menus.Add(new Menu(name, MenuType.Item, null, url, icon, sort));
            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync();
            _logger.LogInformation("已创建 {Count} 个管理端默认菜单", created);
        }
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

    /// <summary>
    /// 默认角色-权限分配（幂等自愈）：ROOT/ADMIN 恒持有全部叶子 Api 权限、
    /// USER 持有除审计与身份管理外的叶子 Api 权限、GUEST/UNKNOWN 不分配。
    ///
    /// 为什么是「每次启动做差集补齐」而不是「只补本次新增的码」：
    /// 旧实现仅在 Roles 表尚无任何授权时做全量分配，此后永远只补「本次启动新出现的码」。
    /// 若某个权限码在更早的启动中就已入库（不属于本次新增），它将永远补不上——
    /// 典型症状是 ADMIN 角色缺少 api:audit:*，导致管理端内容/举报/社区接口全部 403。
    /// 故此处每次都按目标集合做差集补齐，新老库都能自愈。
    ///
    /// ⚠️ 只授叶子（PermissionType.Api）：目录/菜单节点不直接入库授权，
    /// 否则 "api:audit" 目录码会前缀段匹配放行全部 api:audit:* （对 USER 即权限提升）。
    /// 代价：管理员若手工从 ROOT/ADMIN 撤销某权限，下次启动会被补回（这是自愈的取舍）。
    /// </summary>
    private async Task SeedRolePermissionsAsync(IdentityDbContext context)
    {
        var roles = await context.Roles
            .Include(r => r.Permissions)
            .ToListAsync();
        if (roles.Count == 0)
            return;

        // 仅可授权的叶子码；已软删的码不参与补授
        var permissions = await context.Permissions
            .Where(p => p.PermissionType == PermissionType.Api && !p.IsDeleted)
            .ToListAsync();
        if (permissions.Count == 0)
        {
            _logger.LogWarning("Permissions 表无可授权的叶子权限，跳过角色-权限分配");
            return;
        }

        var changed = false;
        foreach (var role in roles)
        {
            switch (role.RoleCode.ToUpperInvariant())
            {
                case "ROOT":
                case "ADMIN":
                case "USER":
                    // 差集补齐：已有授权跳过，避免重复插入 RolePermissions 关联行
                    var granted = role.Permissions
                        .Select(p => p.PermissionCode)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var p in permissions)
                    {
                        if (role.RoleCode.ToUpperInvariant() == "USER"
                            && (p.PermissionCode.StartsWith("api:audit:", StringComparison.OrdinalIgnoreCase)
                                || p.PermissionCode.Equals("api:identity:manage", StringComparison.OrdinalIgnoreCase)))
                            continue;
                        if (!granted.Add(p.PermissionCode))
                            continue;
                        role.Permissions.Add(p);
                        changed = true;
                    }
                    break;

                default: // GUEST / UNKNOWN：不分配
                    break;
            }
        }

        if (!changed)
        {
            _logger.LogInformation("角色-权限分配已是最新，无需补齐");
            return;
        }

        await context.SaveChangesAsync();
        var stats = roles.Select(r => $"{r.RoleName}:{r.Permissions.Count}").ToList();
        _logger.LogInformation("角色-权限分配已补齐: {Stats}", string.Join(", ", stats));
    }

    /// <summary>
    /// 初始化系统管理账号（幂等：邮箱已存在则跳过，不重置已有账号密码）：
    ///   root  → ROOT（系统根角色，全量权限）
    ///   admin → ADMIN（管理员角色）
    /// 邮箱可由 InitialAdmin:RootEmail / InitialAdmin:AdminEmail 覆盖（未配置时用上述默认值）；
    /// 开通通知的投递地址由 InitialAdmin:NotifyEmail 指定（未配置时发到账号邮箱本身），
    /// 便于账号使用系统自有地址（普通用户注册不到）而通知投递到真实收件箱。
    /// 密码由 CSPRNG 随机生成，仅在真正新建账号时经邮件后台队列明文下发
    /// （P6：不在数据库事务内做 SMTP 外部 IO）；同时按运维需要**明文写入日志**
    /// （原实现刻意不落日志，2026-10-05 改为记录，便于本地/部署环境直接取初始密码）。
    /// ⚠️ 生产环境应移除此日志行或降级处理，改由安全渠道分发；首次登录后请立即修改密码。
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

            // ⚠️ 初始密码明文落日志（2026-10-05 按运维需要新增）：原实现刻意不落日志、仅邮件下发。
            //    仅在该账号本次真正新建并落库成功后输出，幂等跳过时不打印。
            _logger.LogWarning(
                "系统管理账号已生成，初始密码（明文，仅本次生成时输出，请登录后立即修改）：{Email} / {Password}",
                email, password);

            // P6：初始密码经邮件后台队列明文下发；主题带角色便于同收件箱区分
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
