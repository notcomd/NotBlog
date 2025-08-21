using Identity.Domain.IdentiyResult;

namespace Identity.Domain.Server;

public class IdentityDomainRoleManagerServer
{


    private readonly INotDateTime.INotDateTime _notDateTime;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<IdentityDomainRoleManagerServer> _logger;


    public IdentityDomainRoleManagerServer(INotDateTime.INotDateTime notDateTime, IUserRoleRepository userRoleRepository, ILogger<IdentityDomainRoleManagerServer> logger)
    {
        _notDateTime = notDateTime ?? throw new ArgumentNullException(nameof(notDateTime));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async ValueTask<UserAccessResult> CreateRoleAsync(string roleName)
    {
        try
        {
            if (string.IsNullOrEmpty(roleName))
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色名称不能为空。");
                return UserAccessResult.NotFund;
            }
            var roleExists = await _userRoleRepository.FindByUserRoleAsync(roleName);
            if (roleExists is not null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roleName} 已存在。");
                return UserAccessResult.AlreadyExists;
            }
            var newRole = Roles.CreateByRoleAsync(roleName, _notDateTime.NowOffset);
            await _userRoleRepository.AddByUserRoleAsync(newRole);
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 角色 {roleName} 创建成功。");
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
    public Claim ResultWhitRoleClaim(Roles roles) => roles.RoleClaimToClaim(roles.RoleClaims);

}
