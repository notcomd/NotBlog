using Identity.Domain.Entities.RoleAggregate;
using Identity.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.Web.API.Extensions;

/// <summary>
/// Identity 数据库种子数据初始化器。
/// 启动时自动完成（均幂等，重复运行安全）：
/// ① 权限映射初始数据：appsettings PermissionMappings → Permissions 表（按 Code 去重补齐）；
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
        await SeedRolePermissionsAsync(context);
        await SeedInitialAdminAsync(context);
    }

    /// <summary>权限映射初始数据：appsettings PermissionMappings → Permissions 表（按 Code 幂等补齐）</summary>
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
            .ToList();

        var existing = await context.Permissions
            .Where(p => codes.Contains(p.PermissionCode))
            .Select(p => p.PermissionCode)
            .ToListAsync();

        var toAdd = codes
            .Except(existing, StringComparer.OrdinalIgnoreCase)
            .Select(code => new Permission(code, ResolvePermissionName(code), "api"))
            .ToList();

        if (toAdd.Count == 0)
        {
            _logger.LogInformation("权限初始数据已就绪（{Count} 个权限码）", existing.Count);
            return;
        }

        await context.Permissions.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
        _logger.LogInformation("已写入 {Count} 个权限初始数据（共 {Total} 个权限码）", toAdd.Count, codes.Count);
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
                    foreach (var p in permissions)
                        role.Permissions.Add(p);
                    break;

                case "USER":
                    foreach (var p in permissions)
                    {
                        if (p.PermissionCode.StartsWith("api:audit:", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (p.PermissionCode.Equals("api:identity:manage", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>权限码 → 中文名（如 api:tweet:read → 推文-读取）</summary>
    private static string ResolvePermissionName(string code)
    {
        var parts = code.Split(':');
        if (parts.Length < 3)
            return code;

        var resourceName = ResourceNames.TryGetValue(parts[1], out var rn) ? rn : parts[1];
        var actionName = ActionNames.TryGetValue(parts[2], out var an) ? an : parts[2];
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
