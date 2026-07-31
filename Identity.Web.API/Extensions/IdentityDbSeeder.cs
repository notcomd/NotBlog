using Identity.Domain.Entities.RoleAggregate;
using Identity.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Identity.Web.API.Extensions;

/// <summary>
/// Identity 数据库种子数据初始化器。
/// 在应用启动时自动插入系统默认角色，使用 Idempotent upsert 模式确保重复运行不会报错。
/// </summary>
public class IdentityDbSeeder : IDbSeeder<IdentityDbContext>
{
    private readonly ILogger<IdentityDbSeeder> _logger;

    public IdentityDbSeeder(ILogger<IdentityDbSeeder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync(IdentityDbContext context)
    {
        var existingCount = await context.Roles.CountAsync();
        if (existingCount > 0)
        {
            _logger.LogInformation("数据库中已存在 {Count} 个角色，跳过种子数据初始化", existingCount);
            return;
        }

        var defaultRoles = Roles.RoleFactory.GetDefaultRoles();
        await context.Roles.AddRangeAsync(defaultRoles);
        await context.SaveChangesAsync();

        _logger.LogInformation("已创建 {Count} 个系统默认角色: {Roles}",
            defaultRoles.Count,
            string.Join(", ", defaultRoles.Select(r => $"{r.RoleName}({r.RoleCode})")));
    }
}
