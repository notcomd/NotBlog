using Identity.Domain.IdentiyResult;

namespace Identity.Domain.Server;

public class IdentityDomainCheckLogInServer
{

    private readonly IUserRepository _userRepository;

    private readonly ILogger<IUserRepository> _loggerUser;

    private readonly INotDateTime.INotDateTime _notDateTime;

    public IdentityDomainCheckLogInServer(IUserRepository userRepository, ILogger<IUserRepository> loggerUser, INotDateTime.INotDateTime notDateTime)
    {
        _userRepository = userRepository;
        _loggerUser = loggerUser;
        _notDateTime = notDateTime;
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
        if (IsUserLockedOut(user))
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {email} 被锁定。");
            return UserAccessResult.Locked;
        }
        if (!await user.IsVerifyByPasswordAsync(password))
        {
            AccessFailed(user);
            _loggerUser.LogError($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {email} 密码错误。");
            return UserAccessResult.Error;
        }
        if (!this.IsUserActive(user))
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
    public async ValueTask<UserAccessResult> CheckLogInWhitPhoneAsync(PhoneNumber phoneNumber,string password)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber, nameof(phoneNumber));
        var loginPhone=await _userRepository.FindOneByUserAsync(phoneNumber);
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
        if (!await loginPhone.IsVerifyByPasswordAsync(password))
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
    
   
    public bool IsUserLocked(User user)=>user.IsUserLockedOut();

    public bool IsUserLockedOut(User user) => user.UserAccessFail.IsLockOutByAccessFaild();

    public void ResetAccessFailCount(User user) => user.UserAccessFail.ResetFail();

    public void AccessFailed(User user) => user.UserAccessFail.VerifyByAccessFailed();

    public bool IsUserActive(User user) => user.UserSafety.GetIsActive();

}
