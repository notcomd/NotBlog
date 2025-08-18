using Identity.Domain.IdentiyResult;

namespace Identity.Domain.Server;

public class IdentityDomainCheckLogServer
{

    private readonly IUserRepository _userRepository;

    private readonly ILogger<IUserRepository> _loggerUser;

    private readonly INotDateTime.INotDateTime _notDateTime;

    public IdentityDomainCheckLogServer(IUserRepository userRepository, ILogger<IUserRepository> loggerUser, INotDateTime.INotDateTime notDateTime)
    {
        _userRepository = userRepository;
        _loggerUser = loggerUser;
        _notDateTime = notDateTime;
    }


    public async ValueTask<UserAccessResult> CheckLogInAsync(string email, string password)
    {
        var user = await _userRepository.FindOneByUserAsync(email);
        if (user is null)
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {email} 不存在。");
            return UserAccessResult.UserNotFound;
        }
        if (IsUserLockedOut(user))
        {
            _loggerUser.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {email} 被锁定。");
            return UserAccessResult.UserLocked;
        }
        if (!await user.IsVerifyByPassword(password))
        {
            AccessFailed(user);
            _loggerUser.LogError($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {email} 密码错误。");
            return UserAccessResult.PasswordError;
        }
        if (!this.IsUserActive(user))
        {
            _loggerUser.LogError($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {email} 未激活。");
            return UserAccessResult.UserNotActive;
        }
        ResetAccessFailCount(user);
        _loggerUser.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {email} 登录成功。");
        return UserAccessResult.Success;
    }




    public bool IsUserLockedOut(User user)
    {
        return user.UserAccessFail.IsLockOutByAccessFaild();
    }

    public void ResetAccessFailCount(User user)
    {
        _loggerUser.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 重置用户 {user.UserEmail} 的访问失败计数。");
        user.UserAccessFail.ResetFail();
    }

    public void AccessFailed(User user)
    {
        _loggerUser.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 记录用户 {user.UserEmail} 的访问失败。");
        user.UserAccessFail.VerifyByAccessFailed();
    }

    public bool IsUserActive(User user)
    {
        return user.UserSafety.GetIsActive();
    }

}
