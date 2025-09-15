using Identity.Domain.IdentiyResult;

namespace Identity.Domain.DomainServer;

public class IdentityDomainRegisterServer
{

    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<IdentityDomainRegisterServer> _logger;
    private readonly INotDateTime _notDateTime;


    public IdentityDomainRegisterServer(IUserRepository userRepository, IUserRoleRepository userRoleRepository,
        ILogger<IdentityDomainRegisterServer> logger, INotDateTime notDateTime)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _logger = logger;
        _notDateTime = notDateTime;
    }

    /// <summary>
    ///  注册邮箱用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="passwordHash">传输的加密的密码哈希</param>
    /// <returns></returns>
    public async ValueTask<UserAccessResult> RegisterWhitEmailAsync(string email, string passwordHash, string roleName)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 邮箱地址不能为空。");
            return UserAccessResult.NotVerify;
        }
        var emailSignUp = await _userRepository.FindOneByUserAsync(email);
        var roleDefult = await _userRoleRepository.FindByUserRoleAsync(roleName);
        if (emailSignUp is not null)
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {email} 已存在。");
            return UserAccessResult.AlreadyExists;
        }
        if (roleDefult is null)
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roleName} 不存在。");
            return UserAccessResult.NotFund;
        }
        var emailSignUpTrc = new User(roleDefult.Id, email, passwordHash, _notDateTime.UtcNow);
        await _userRepository.AddOneByUserAsync(emailSignUpTrc);
        _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {email} 创建成功。");
        return UserAccessResult.Success;
    }

    public async ValueTask<UserAccessResult> RegisterWhitPhoneAsync(PhoneNumber phoneNumber, string passwordHash, string roleName)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber, nameof(phoneNumber));
        var signUpDate = await _userRepository.FindOneByUserAsync(phoneNumber);
        var roleDefult = await _userRoleRepository.FindByUserRoleAsync(roleName);
        if (signUpDate is not null)
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {phoneNumber} 已存在。");
            return UserAccessResult.AlreadyExists;
        }
        if (roleDefult is null)
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 角色 {roleName} 不存在。");
            return UserAccessResult.NotFund;
        }
        var phoneSignUpTrc = new User(roleDefult.Id, phoneNumber, passwordHash, _notDateTime.UtcNow);
        await _userRepository.AddOneByUserAsync(phoneSignUpTrc);
        _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] 用户 {phoneNumber.PhoneCode} 创建成功。");
        return UserAccessResult.Success;
    }

    public bool IsPhoneNumber(User user) => user.IsVerifyByPhoneNumber(user.PhoneNumber);

    public bool IsEmail(User user) => user.IsVerifyByEmail(user.UserEmail);

    public bool IsCheckWhitEmail(string Email)
    {
        if (string.IsNullOrEmpty(Email))
        {
            _logger.LogWarning($"[{_notDateTime.UtcNow}] 邮箱地址不能为空。");
            return false;
        }
        if (new EmailAddressAttribute().IsValid(Email) is false)
        {
            _logger.LogWarning($"[{_notDateTime.UtcNow}] 邮箱地址格式不正确。");
            return false;
        }
        return true;
    }

    public bool IsCheckWhitPhoneNumber(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
        {
            _logger.LogWarning($"[{_notDateTime.UtcNow}] 手机号码不能为空。");
            return false;
        }
        if (phoneNumber.PhoneCode.Length <= 11 && phoneNumber.PhoneCode.Length >= 8)
        {
            _logger.LogWarning($"[{_notDateTime.UtcNow}] 手机号码格式不正确。");
            return false;
        }
        return true;
    }
}