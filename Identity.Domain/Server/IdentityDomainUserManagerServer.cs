using Identity.Domain.IdentiyResult;

namespace Identity.Domain.Server;

public class IdentityDomainUserManagerServer
{

    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<IdentityDomainUserManagerServer> _logger;
    private readonly INotDateTime.INotDateTime _notDateTime;

    public IdentityDomainUserManagerServer(IUserRepository userRepository, IUserRoleRepository userRoleRepository,
        ILogger<IdentityDomainUserManagerServer> logger, INotDateTime.INotDateTime notDateTime)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _logger = logger;
        _notDateTime = notDateTime;
    }


    public async ValueTask<UserAccessResult> ChangeWithUserRoleAsync(string changeEmail, string changeRoleName)
    {
        try
        {
            var user = await _userRepository.FindOneByUserAsync(changeEmail);
            if (user is null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {changeEmail} 不存在。");
                return UserAccessResult.NotFund;
            }
            var role = await _userRoleRepository.FindByUserRoleAsync(changeRoleName);
            if (role is null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {changeRoleName} 不存在。");
                return UserAccessResult.NotFund;
            }
            user.ChangeByUserRole(role.Id);
            await _userRepository.UpdateByUserAsync(user);
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {changeEmail} 的角色已更改为 {changeRoleName}。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在变更用户角色时出现问题!");
            return UserAccessResult.Error;
        }
    }


    public async ValueTask<UserAccessResult> ChangeWithEmailSafetyAsync(string changeEmail, UserSafety changeUserSafety)
    {
        try
        {
            var userEmailData = await _userRepository.FindOneByUserAsync(changeEmail);
            if (userEmailData is null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {changeEmail} 不存在。");
                return UserAccessResult.NotFund;
            }

            if (UserSafetyEquals(userEmailData.UserSafety, changeUserSafety))
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {changeEmail} 的安全信息没有变化。");
                return UserAccessResult.NotChange;
            }

            await _userRepository.UpdateByUserSafetyAsync(userEmailData.Id, en =>
            {
                en.ChangeBySecurityStamp(changeUserSafety.SecurityStamp);
                en.ChangeByBlackOrWhiteStatus(changeUserSafety.BlackOrWhite);
                en.ChangeByPasswordSalt(changeUserSafety.PasswordSalt);
                en.ChangeByLockOutEndTime(changeUserSafety.LockOutEnd);
            });
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {changeEmail} 的安全信息已更改。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在变更用户安全信息时出现问题!");
            return UserAccessResult.Error;
        }

    }


    public async ValueTask<UserAccessResult> ChangeWithPhoneSafetyAsync(PhoneNumber changePhoneNumber, UserSafety changeUserSafety)
    {
        try
        {
            var userEmailData = await _userRepository.FindOneByUserAsync(changePhoneNumber);
            if (userEmailData is null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {changePhoneNumber} 不存在。");
                return UserAccessResult.NotFund;
            }

            if (UserSafetyEquals(userEmailData.UserSafety, changeUserSafety))
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {changePhoneNumber} 的安全信息没有变化。");
                return UserAccessResult.NotChange;
            }

            await _userRepository.UpdateByUserSafetyAsync(userEmailData.Id, en =>
            {
                en.ChangeBySecurityStamp(changeUserSafety.SecurityStamp);
                en.ChangeByBlackOrWhiteStatus(changeUserSafety.BlackOrWhite);
                en.ChangeByPasswordSalt(changeUserSafety.PasswordSalt);
                en.ChangeByLockOutEndTime(changeUserSafety.LockOutEnd);
            });
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {changePhoneNumber} 的安全信息已更改。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在变更用户安全信息时出现问题!");
            return UserAccessResult.Error;
        }

    }


    public async ValueTask<UserAccessResult> ChangeWithEmailUserClaimAsync(string changeEmail, UserClaim changeUserClaim)
    {
        try
        {
            var userData = await _userRepository.FindOneByUserAsync(changeEmail);
            if (userData is null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {changeEmail} 不存在。");
                return UserAccessResult.NotFund;
            }

            await _userRepository.UpdateByUserClaimAsync(userData.Id, en =>
             {
                 foreach (var claimItm in userData.UserClaims)
                 {
                     if (UserClaimEquals(claimItm, changeUserClaim) && claimItm is not null)
                     {
                         _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}]用户{changeEmail} 无需更新");
                         break;
                     }
                     userData.UpdateClaim(changeUserClaim.ClaimType, changeUserClaim.ClaimValue);
                 }
             });

            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {changeEmail} 的信息已更改。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在变更用户安全信息时出现问题!");
            return UserAccessResult.Error;
        }


    }


    public async ValueTask<UserAccessResult> ChangeWithEmailUserClaimListAsync(string changeEmail, IEnumerable<UserClaim> changeUserClaims)
    {
        try
        {
            var userDate = await _userRepository.FindOneByUserAsync(changeEmail);
            ArgumentNullException.ThrowIfNullOrEmpty(nameof(userDate));
            foreach(var item in userDate.UserClaims ?? throw new ArgumentNullException(nameof(userDate)))
            {
                foreach(var trc in changeUserClaims)
                {
                    if (UserClaimEquals(item, trc)&&item is not null && trc is not null)
                    {
                        _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}]用户{changeEmail} 无需更新");
                        break;
                    }
                    userDate.UpdateClaim(trc.ClaimType,trc.ClaimValue);
                    _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 更新了Key:{trc.ClaimType}->Value:{trc.ClaimValue}");
                }
            }
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在变更用户安全信息时出现问题!");
            return UserAccessResult.Error;
        }
    }


    protected bool UserSafetyEquals(UserSafety userSafety, UserSafety userSafetyTrc)
    {
        if (userSafety is null || userSafetyTrc is null)
        {
            return false;
        }
        return
               userSafety.BlackOrWhite == userSafetyTrc.BlackOrWhite &&
               userSafety.LockOutEnd == userSafetyTrc.LockOutEnd &&
               userSafety.SecurityStamp == userSafetyTrc.SecurityStamp &&
               userSafety.PasswordSalt == userSafetyTrc.PasswordSalt;
    }

    protected bool UserClaimEquals(UserClaim userClaim, UserClaim claim)
    {
        if (userClaim is null || claim is null)
            return false;
        return userClaim.ClaimValue == userClaim.ClaimValue && userClaim.ClaimType == claim.ClaimType;

    }

}
