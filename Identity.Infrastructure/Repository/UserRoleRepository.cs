namespace Identity.Infrastructure.Repository;

public class UserRoleRepository : IUserRoleRepository
{

    private readonly IdentityDbContext _userRoleDbContext;
    private readonly ILogger<IUserRoleRepository> _logger;
    public IUnitOfWork UnitOfWork => _userRoleDbContext;


    public UserRoleRepository(IdentityDbContext userRoleDbContext, ILogger<IUserRoleRepository> logger)
    {
        _userRoleDbContext = userRoleDbContext;
        _logger = logger;
    }



    public async ValueTask AddByUserRoleAsync(Roles userRole)
    {
        await _userRoleDbContext.AddAsync(userRole);
    }

    public async ValueTask<Roles?> FindByUserRoleAsync(Guid roleId)
    {
        try
        {
            return await _userRoleDbContext.Roles.Include(en => en.RoleClaims)
               .FirstOrDefaultAsync(en => en.Id == roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ >_< {DateTimeOffset.UtcNow}]在查找时出现问题,无法找到为{roleId}的数据!");
            throw;
        }

    }

    public async ValueTask<Roles?> FindByUserRoleAsync(string roleName)
    {
        try
        {
            return await _userRoleDbContext.Roles.Include(en => en.RoleClaims)
               .FirstOrDefaultAsync(en => en.RoleName == roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ >_< {DateTimeOffset.UtcNow}]在查找时出现问题,无法找到为{roleName}的数据!");
            throw;
        }
    }

    public async ValueTask<bool> IsUserRoleAsync(Guid guid)
    {
        if (await FindByUserRoleAsync(guid) is null) return false;
        return true;
    }

    public async ValueTask<bool> IsUserRoleAsync(string roleName)
    {
        if (await FindByUserRoleAsync(roleName) is null) return false;
        return true;
    }

    public async ValueTask UpByUserRoleAsync(Roles userRole)
    {
        try
        {
            var roleData = await FindByUserRoleAsync(userRole.Id);
            if (roleData is null) return;
            var updateCount=await _userRoleDbContext.Roles.ExecuteUpdateAsync(en => en
                .SetProperty(en => en.Attribute, userRole.Attribute)
                .SetProperty(en => en.RoleStatus, userRole.RoleStatus)
                .SetProperty(en => en.RoleAuthority, userRole.RoleAuthority));
            _logger.LogInformation($"[（*＾-＾*）{DateTimeOffset.UtcNow} 数据更新成功，一共更新了{updateCount}条目]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ >_< {DateTimeOffset.UtcNow}]在更新数据时出现问题,无法更新{userRole}的数据!");
            throw;
        }
    }
}