using System.Net.WebSockets;

namespace Identity.Infrastructure.Repository;

public class UserRoleRepository : IUserRoleRepository
{

    private readonly IdentityDbContext _userRoleDbContext;
    private readonly ILogger<IUserRoleRepository> _logger;
    public IUnitOfWork UnitOfWork => _userRoleDbContext;
    private readonly INotDateTime _notDateTime;

    public UserRoleRepository(IdentityDbContext userRoleDbContext, ILogger<IUserRoleRepository> logger, INotDateTime notDateTime)
    {
        _userRoleDbContext = userRoleDbContext;
        _logger = logger;
        _notDateTime = notDateTime;
    }

    public async ValueTask AddByUserRoleAsync(Roles userRole)
    {
        ArgumentNullException.ThrowIfNull(userRole, nameof(userRole));
        await _userRoleDbContext.AddAsync(userRole);
        _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}]UserRole Add! {userRole.Id}");
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
            _logger.LogError(ex, $"[ >_< {_notDateTime.UtcNow}]在查找时出现问题,无法找到为{roleId}的数据!");
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
            _logger.LogError(ex, $"[ >_< {_notDateTime.UtcNow}]在查找时出现问题,无法找到为{roleName}的数据!");
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
            var updateCount = await _userRoleDbContext.Roles.ExecuteUpdateAsync(en => en
                .SetProperty(en => en.Attribute, userRole.Attribute)
                .SetProperty(en => en.RoleStatus, userRole.RoleStatus)
                .SetProperty(en => en.RoleAuthority, userRole.RoleAuthority));
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow} 数据更新成功，一共更新了{updateCount}条目]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ >_< {_notDateTime.UtcNow}]在更新数据时出现问题,无法更新{userRole}的数据!");
            throw;
        }
    }

    public async ValueTask<IEnumerable<RoleClaim>> FindRoleClaimByRolesAsync(Guid roleGuid)
    {
        try
        {
            var role = await FindByUserRoleAsync(roleGuid);
            if (role is null) return Enumerable.Empty<RoleClaim>();
            return role.RoleClaims;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ >_< {_notDateTime.UtcNow}]在查找角色声明时出现问题,无法找到为{roleGuid}的数据!");
            throw;
        }
    }


    public async ValueTask UpdateWithRoleAsync(Guid roleGuid, Func<Roles, Task> func)
    {
        try
        {
            var roleData = await FindByUserRoleAsync(roleGuid);
            if(roleData is null) throw new ArgumentNullException(nameof(roleGuid), "Role GUID cannot be empty");
            await func(roleData);
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow} 数据更新成功，更新了{roleData.Id}条目]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ >_< {_notDateTime.UtcNow}]在更新数据时出现问题,无法更新{roleGuid}的数据!");
            return;
        }
    }

    public async ValueTask UpdateWithRoleAsync(string roleName, Func<Roles, Task> func)
    {
        try
        {
            var roleData =await FindByUserRoleAsync(roleName);
            if (roleData is null) throw new ArgumentNullException(nameof(roleName), "Role Name cannot be empty");
            await func(roleData);
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow} 数据更新成功，更新了{roleData.Id}条目]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ >_< {_notDateTime.UtcNow}]在更新数据时出现问题,无法更新{roleName}的数据!");
            return;
        }
    }

    public async ValueTask DeleteByUserRoleAsync(Guid roleGuid)
    {
        try {
            var roleData = await FindByUserRoleAsync(roleGuid);
            if (roleData is null) throw new ArgumentNullException(nameof(roleGuid), "Role GUID cannot be empty");
            _userRoleDbContext.Roles.Remove(roleData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ >_< {_notDateTime.UtcNow}]在删除数据时出现问题,无法删除{roleGuid}的数据!");
            throw;
        }
    }


}