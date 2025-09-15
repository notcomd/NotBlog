using Identity.Domain.IdentiyResult;

namespace Identity.Domain.DomainServer;

public class IdentityDomainCheckLogInServer
{

    private readonly IUserRepository _userRepository;

    private readonly ILogger<IUserRepository> _loggerUser;

    private readonly INotDateTime _notDateTime;

    private readonly INotMemoryCache _notMemoryCache;

    public IdentityDomainCheckLogInServer(IUserRepository userRepository, ILogger<IUserRepository> loggerUser,
        INotDateTime notDateTime, INotMemoryCache notMemoryCache)
    {
        _userRepository = userRepository;
        _loggerUser = loggerUser;
        _notDateTime = notDateTime;
        _notMemoryCache = notMemoryCache;
    }

    /// <summary>
    ///  验证邮箱登录
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public async ValueTask<UserAccessResult> CheckLogInWhitEmailAsync(string email, string password)
    {
        var user = await _userRepository.FindOneByUserAsync(email);

        if (user is null)
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {email} 不存在。");
            return UserAccessResult.NotFund;
        }
        else if (IsUserLockedOut(user))
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {email} 被锁定。");
            return UserAccessResult.Locked;
        }
        else if (!user.IsVerifyByPassword(password))
        {
            AccessFailed(user);
            _loggerUser.LogError($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {email} 密码错误。");
            return UserAccessResult.Error;
        }
        else if (!IsUserActive(user))
        {
            _loggerUser.LogError($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {email} 未激活。");
            return UserAccessResult.NotActive;
        }
        ResetAccessFailCount(user);
        _loggerUser.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {email} 登录成功。");
        return UserAccessResult.Success;
    }

    /// <summary>
    ///  验证手机号登录
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public async ValueTask<UserAccessResult> CheckLogInWhitPhoneAsync(PhoneNumber phoneNumber, string password)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber, nameof(phoneNumber));

        var loginPhone = await _userRepository.FindOneByUserAsync(phoneNumber);

        if (loginPhone is null)
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {phoneNumber.PhoneCode} 不存在。");
            return UserAccessResult.NotFund;
        }
        if (IsUserLocked(loginPhone))
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {phoneNumber.PhoneCode} 被锁定。");
            return UserAccessResult.Locked;
        }
        if (!loginPhone.IsVerifyByPassword(password))
        {
            AccessFailed(loginPhone);
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {phoneNumber.PhoneCode} 密码错误。");
            return UserAccessResult.Error;
        }
        if (!IsUserActive(loginPhone))
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {phoneNumber.PhoneCode} 未激活。");
            return UserAccessResult.NotActive;
        }
        ResetAccessFailCount(loginPhone);
        _loggerUser.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {phoneNumber.PhoneCode} 验证成功。");
        return UserAccessResult.Success;
    }
<<<<<<< HEAD:Identity.Domain/DomainServer/IdentityDomainCheckLogInServer.cs

    public async ValueTask<UserAccessResult> CheckLogInWithGenalAsync(string LoginWithEmail, string code)
    {
        try
        {
            if (string.IsNullOrEmpty(LoginWithEmail) || string.IsNullOrEmpty(code))
                return UserAccessResult.NotVerify;
            var userCode = await _userRepository.FindOneByUserAsync(LoginWithEmail);

            if (userCode is null)
            {
                _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {LoginWithEmail} 不存在。");
                return UserAccessResult.NotFund;
            }
            if (IsUserLockedOut(userCode))
            {
                _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {LoginWithEmail} 被锁定。");
                return UserAccessResult.Locked;
            }
            else if (!await _notMemoryCache.IsValidateCodeAsync(LoginWithEmail, code))
            {
                AccessFailed(userCode);
                _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {LoginWithEmail} 验证码错误。");
                return UserAccessResult.Error;
            }
            else if (!IsUserActive(userCode))
            {
                _loggerUser.LogError($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {LoginWithEmail} 未激活。");
                return UserAccessResult.NotActive;
            }
            _loggerUser.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {LoginWithEmail} 登录成功。");
            return UserAccessResult.Success;
        }
        catch (Exception ex)
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {LoginWithEmail} 登录异常。{ex.Message}");
            return UserAccessResult.Error;
        }
    }

    public bool IsUserLocked(User user) => user.IsUserLockedOut();
=======
    
   
    public bool IsUserLocked(User user)=>user.IsUserLockedOut();
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9:Identity.Domain/Server/IdentityDomainCheckLogInServer.cs

    public bool IsUserLockedOut(User user) => user.UserAccessFail.IsLockOutByAccessFaild();

    public void ResetAccessFailCount(User user) => user.UserAccessFail.ResetFail();

    public void AccessFailed(User user) => user.UserAccessFail.VerifyByAccessFailed();

    public bool IsUserActive(User user) => user.UserSafety.GetIsActive();

    public bool IsUserSafetyLocked(User user) => user.UserSafety.GetIsLockedOut();

}
