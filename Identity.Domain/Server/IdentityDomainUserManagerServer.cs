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

            var role = await _userRoleRepository.FindByUserRoleAsync(changeRoleName);
            if (role is null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {changeRoleName} 不存在。");
                return UserAccessResult.NotFund;
            }

            await _userRepository.UpdateByUserAsync(changeEmail, en =>
            {
                en.ChangeByUserRole(role.Id);
                return Task.CompletedTask;
            });

            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {changeEmail} 的角色已更改为 {changeRoleName}。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在变更用户角色时出现问题!");
            return UserAccessResult.Error;
        }
    }


    public async ValueTask<UserAccessResult> ChangeWithUserSafetyAsync(string changeUser, ChangByUserSafetyDto changeUserSafety)
    {
        try
        {
            await _userRepository.UpdateByUserSafetyAsync(changeUser, en =>
            {
                if (!string.IsNullOrEmpty(changeUserSafety.SecurityStamp))
                    en.UserSafety.ChangeBySecurityStamp(changeUserSafety.SecurityStamp);
                if (changeUserSafety.BlackOrWhite is not null)
                    en.UserSafety.ChangeByBlackOrWhiteStatus((EnBlackOrWhite)changeUserSafety.BlackOrWhite);
                if (!string.IsNullOrEmpty(changeUserSafety.PasswordSalt))
                    en.UserSafety.ChangeByPasswordSalt(changeUserSafety.PasswordSalt);
                if (changeUserSafety.UserStatus is not null)
                    en.UserSafety.ChangeByUserStatus((EnUserStatus)changeUserSafety.UserStatus);
                en.UserSafety.ChangeByLockOutEndTime(changeUserSafety.LockOutEnd);
                return Task.CompletedTask;
            });

            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {changeUser} 的安全信息已更改。");
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
                 return Task.CompletedTask;
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
            foreach (var item in userDate.UserClaims ?? throw new ArgumentNullException(nameof(userDate)))
            {
                foreach (var trc in changeUserClaims)
                {
                    if (UserClaimEquals(item, trc) && item is not null && trc is not null)
                    {
                        _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}]用户{changeEmail} 无需更新");
                        break;
                    }
                    userDate.UpdateClaim(trc!.ClaimType, trc.ClaimValue);
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


    public async ValueTask<UserAccessResult> ChangeWithEmailUserAsync(string changeEmail, ChangeByUserDto newUserInformetion)
    {
        try
        {
            ArgumentNullException.ThrowIfNullOrEmpty(changeEmail);
            ArgumentNullException.ThrowIfNull(newUserInformetion);

            await _userRepository.UpdateByUserAsync(changeEmail, en =>
             {
                 en.ChangeByUserName(newUserInformetion!.UserName);
                 en.ChangeByAddress(newUserInformetion!.UserAddress);
                 en.ChangeByImageCover(newUserInformetion!.UserImageCover);
                 return Task.CompletedTask;
             });
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {changeEmail} 的信息已更改。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在变更用户信息时出现问题!");
            return UserAccessResult.Error;
        }
    }



    public async ValueTask<bool> ChangeByUserPasswordAsync(string changeEmail, ChangeByUserPasswordDto changeByUserPasswordDto)
    {
        try
        {
            await _userRepository.UpdateByUserSafetyAsync(changeEmail, async en =>
            {
                var oldHashPasswod = await HashHelper.CreateHash256Async(changeByUserPasswordDto.OldPassword,
                    Encoding.UTF8.GetBytes(en.UserSafety.PasswordSalt));
                if (await en.IsVerifyByPasswordAsync(oldHashPasswod))
                {
                    await en.ChangeByPasswordAsync(changeByUserPasswordDto.NewPassword,
                        Encoding.UTF8.GetBytes(en.UserSafety.PasswordSalt), en.UserSafety.SecurityStamp);
                    var stemp = await HashHelper.GenerateSecurityStamp();
                    en.UserSafety.ChangeBySecurityStamp(stemp);
                }
                return;
            });
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {changeEmail} 的密码已更改。");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在变更用户密码时出现问题!");
            return false;
        }
    }



    public async ValueTask<ResultWIthUserDto> GetWithUserAsync(string userEmail, CancellationToken cancellationToken)
    {

        try
        {
            var userData = await _userRepository.FindOneByUserAsync(userEmail);
            

            var result = userData is null ? null : new ResultWIthUserDto(userData.UserName, userData.UserEmail,
                userData.ImageCover, userData.UserRoleGuid,
                userData.PhoneNumber is not null ? new WithResultPhoneDto((int)userData.PhoneNumber.AddressRegion, userData.PhoneNumber.PhoneCode) : null,
                new WIthResultSafetyDto(userData.UserSafety.UserStatus, userData.UserSafety.BlackOrWhite),
                 userData.UserClaims?
                .Select(claim => new WithResultClaimDto(claim.ClaimType, claim.ClaimValue)) ?? Enumerable.Empty<WithResultClaimDto>()
                , userData.CreateDatetime);
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在获取用户信息时出现问题!");
            throw;
        }
        finally { await Task.CompletedTask; }

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
