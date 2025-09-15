using Identity.Domain.IdentiyResult;

namespace Identity.Domain.DomainServer;

public class IdentityDomainRoleManagerServer
{


    private readonly INotDateTime _notDateTime;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<IdentityDomainRoleManagerServer> _logger;


    public IdentityDomainRoleManagerServer(INotDateTime notDateTime, IUserRoleRepository userRoleRepository, ILogger<IdentityDomainRoleManagerServer> logger)
    {
        _notDateTime = notDateTime ?? throw new ArgumentNullException(nameof(notDateTime));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async ValueTask<UserAccessResult> RegisterWithRoleAsync(RegisterWithRoleDto registerWithRoleDto)
    {
        try
        {
            if (registerWithRoleDto is null)
                throw new ArgumentNullException(nameof(registerWithRoleDto));
            var roleExists = await _userRoleRepository.FindByUserRoleAsync(registerWithRoleDto.RoleName);
            if (roleExists is not null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roleExists.RoleName} 已存在。");
                return UserAccessResult.AlreadyExists;
            }
            var newRole = new Roles(registerWithRoleDto.RoleName, registerWithRoleDto.Attribute,
                _notDateTime.UtcNow, registerWithRoleDto.RoleAuthority, registerWithRoleDto.RoleStatus);
            await _userRoleRepository.AddByUserRoleAsync(newRole);
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 角色 {newRole.RoleName} 创建成功。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 创建角色失败。");
            return UserAccessResult.Error;
        }
    }


    public async ValueTask<UserAccessResult> ChangeWhitRoleAsync(Roles roles)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(roles, nameof(roles));
            var roleExists = await _userRoleRepository.FindByUserRoleAsync(roles.RoleName);
            if (roleExists is not null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roles.RoleName} 以存在。");
                return UserAccessResult.AlreadyExists;
            }
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色更改失败。");
            return UserAccessResult.Error;
        }
    }


    public void ChangeWhitRoleClaim(Roles roles, RoleClaim roleClaim)
    {
        roles.AddRoleClaim(roleClaim);
    }


    public List<Claim> ResultWhitRoleClaim(Roles roles)
    {
        var claim = new List<Claim>();
        foreach (var item in roles.RoleClaims)
        {
            claim.Add(item.ToClaim());
        }
        return claim;
    }


    public async ValueTask<UserAccessResult> ChangeWithRoleAsync(string roleName, ChangeWithRoleDto changeWithRoleDto)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(roleName, nameof(roleName));
            ArgumentNullException.ThrowIfNull(changeWithRoleDto, nameof(changeWithRoleDto));

            await _userRoleRepository.UpdateWithRoleAsync(roleName, async en =>
            {
                if (changeWithRoleDto.ChangeRoleAuthority != en.RoleAuthority)
                    en.ChangeByRoleAuthority(changeWithRoleDto.ChangeRoleAuthority);
                if (changeWithRoleDto.ChangeRoleStatus != en.RoleStatus)
                    en.ChangeByRoleStatus(changeWithRoleDto.ChangeRoleStatus);
                if (!string.IsNullOrEmpty(changeWithRoleDto.ChangeRoleName) && changeWithRoleDto.ChangeRoleName != en.RoleName)
                    en.ChangeByRoleName(changeWithRoleDto.ChangeRoleName);
                if (changeWithRoleDto.ChangeRoleClaims is not null && changeWithRoleDto.ChangeRoleClaims.Any())
                {
                    var list = new List<RoleClaim>();
                    foreach (var claim in changeWithRoleDto.ChangeRoleClaims)
                    {
                        var claimTrc = RoleClaim.CreateByRoleClaim(en.Id, claim.ClaimType, claim.ClaimValue);
                        list.Add(claimTrc);
                    }
                    en.ChangeByRoleClaim(list);
                }
            });
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 角色 {roleName} 的信息已更改。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色更改失败。");
            return UserAccessResult.Error;
        }
    }


    public async ValueTask<ResultWithRoleDto?> GetWithRoleAsync(object roleWithObject,CancellationToken cancellationToken)
    {
        try
        {
            if (roleWithObject is Guid roleGuid)
            {
                var roleDate = await _userRoleRepository.FindByUserRoleAsync(roleGuid);
                if (roleDate is null)
                {
                    _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roleGuid} 未找到。");
                    return null;
                }
                var roleResult = new ResultWithRoleDto(roleDate.RoleName, roleDate.Attribute is null ? string.Empty : roleDate.Attribute,
                    roleDate.RoleClaims?.Select(en => new WithResultRoleClaimDto(en.ClaimType, en.ClaimValue))??[]);
                return roleResult;
            }
            else if (roleWithObject is string roleName)
            {
                var roleDate = await _userRoleRepository.FindByUserRoleAsync(roleName);
                if (roleDate is null)
                {
                    _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roleName} 未找到。");
                    return null;
                }
                var roleResult = new ResultWithRoleDto(roleDate.RoleName, roleDate.Attribute is null ? string.Empty : roleDate.Attribute,
                    roleDate.RoleClaims?.Select(en => new WithResultRoleClaimDto(en.ClaimType, en.ClaimValue))??[]);
                return roleResult;
            }
            else {                
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 提供的参数类型不支持。");
                throw new Exception("Provided parameter type is not supported.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 获取角色信息失败。");
            return null;
        }

    }


    public async ValueTask<ResultWithRoleManagerDto?> GetWithRoleManagerInformetionAsync(object roleWithObject,CancellationToken cancellationToken)
    {
        try
        {
            if (roleWithObject is Guid roleGuid)
            {
                var roleDate = await _userRoleRepository.FindByUserRoleAsync(roleGuid);
                if (roleDate is null)
                {
                    _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roleGuid} 未找到。");
                    return null;
                }
                var roleResult = new ResultWithRoleManagerDto(roleDate.RoleName, 
                    roleDate.Attribute is null?string.Empty:roleDate.Attribute,
                    roleDate.RoleAuthority,
                    roleDate.RoleStatus,
                    roleDate.RoleClaims?.Select(en => new WithResultRoleClaimDto(en.ClaimType, en.ClaimValue))??[]);
                return roleResult;
            }
            else if (roleWithObject is string roleName)
            {
                var roleDate = await _userRoleRepository.FindByUserRoleAsync(roleName);
                if (roleDate is null)
                {
                    _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roleName} 未找到。");
                    return null;
                }
                var roleResult = new ResultWithRoleManagerDto(roleDate.RoleName,
                    roleDate.Attribute is null?string.Empty:roleDate.Attribute,
                    roleDate.RoleAuthority,
                    roleDate.RoleStatus,
                    roleDate.RoleClaims?.Select(en => new WithResultRoleClaimDto(en.ClaimType, en.ClaimValue))?? []);
                return roleResult;
            }
            else
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 提供的参数类型不支持。");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 获取角色信息失败。");
            return null;
        }
    }


    public async ValueTask<IEnumerable<ResultWithRoleManagerDto>> GetWithRoleManagerInformetionsAsync(CancellationToken cancellationToken)
    {
        try{
            // 接口中的FindByUserRolesAsync方法不接受CancellationToken参数
            var roleDate = await _userRoleRepository.FindByUserRolesAsync();
            if (roleDate is null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 未找到。");
                return Enumerable.Empty<ResultWithRoleManagerDto>();
            }
            var roleResult = roleDate.Select(role => new ResultWithRoleManagerDto(role.RoleName,
                    role.Attribute is null ? string.Empty : role.Attribute,
                    role.RoleAuthority,
                    role.RoleStatus,
                    role.RoleClaims?.Select(claim => new WithResultRoleClaimDto(claim.ClaimType, claim.ClaimValue)) ?? []));
            return roleResult;

        }catch(Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 获取角色信息失败。");
            return Enumerable.Empty<ResultWithRoleManagerDto>();
        }
    }


}
