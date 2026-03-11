using System.Diagnostics;

namespace Identity.Domain.Server;

public class UserRepositoryServer
{
    private readonly IJwtTokenService _jwtTokenServer;
    private readonly ILogger<IUserRepository> _loggerUser;
    private readonly ILogger<IUserRoleRepository> _loggerUserRole;
    private readonly IOptionsSnapshot<JwtOptions> _optionsSnapshot;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public UserRepositoryServer(IOptionsSnapshot<JwtOptions> optionsSnapshot, ILogger<IUserRepository> loggerUser,
        IUserRepository userRepository, IUserRoleRepository userRoleRepository,
        IJwtTokenService jwtTokenServer, ILogger<IUserRoleRepository> loggerUserRole)
    {
        _jwtTokenServer = jwtTokenServer;
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
        _loggerUser = loggerUser;
        _loggerUserRole = loggerUserRole;
        _optionsSnapshot = optionsSnapshot;
    }

    public async ValueTask<string> LogInByCheckPasswordAsync(PhoneNumber phoneNumber, string password, long code)
    {
        return await LogInByCheckPasswordCoreAsync(phoneNumber, password);
    }

    /// <summary>
    ///     登入验证
    /// </summary>
    /// <param name="email">电子邮件地址</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码（当前未使用）</param>
    /// <returns>成功返回 JWT 令牌，失败返回错误信息</returns>
    public async ValueTask<string> LogInByCheckPasswordAsync(
        [EmailAddress(ErrorMessage = "无效邮件地址")]
        string email,
        string password,
        string code)
    {
        var userData = await _userRepository.FindOneByUserAsync(email);
        if (userData is null)
        {
            _loggerUser.LogError($"[{DateTime.UtcNow}] 用户 {email} 不存在");
            return $"用户 {email} 不存在！";
        }

        return await LogInByCheckPasswordCoreAsync(userData, password);
    }

    public async ValueTask<bool> SigInByCreateUserAsync(string email, string password, string code)
    {
        var existingUser = await _userRepository.FindOneByUserAsync(email);
        Debug.Assert(existingUser != null, nameof(existingUser) + " != null");
        if (existingUser != null)
        {
            _loggerUser.LogInformation($"[{DateTime.UtcNow}] 已存在用户：{email}");
            return false;
        }

        var userRoleGuid = new HashSet<Guid>();
        var role = new Roles(userRoleGuid, email);
        await _userRoleRepository.AddByUserRoleAsync(role);

        var newUser = await User.CreateByEmailUser(
            userRoleGuid,
            email,
            password,
            new Uri("https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png"));
        await _userRepository.AddOneByUserAsync(newUser);
        return true;
    }

    /// <summary>
    ///     登入验证核心方法
    /// </summary>
    /// <param name="userIdentifier">用户标识符，可以是手机号或电子邮件地址</param>
    /// <param name="password">用户密码</param>
    /// <returns>返回一个字符串，表示登录结果。成功返回 JWT 令牌，失败返回错误信息</returns>
    /// <exception cref="ArgumentException">当用户标识符类型无效时抛出</exception>
    /// <exception cref="ArgumentNullException">当用户不存在时抛出</exception>
    private async ValueTask<string> LogInByCheckPasswordCoreAsync(object userIdentifier, string password)
    {
        User? userData;

        if (userIdentifier is PhoneNumber phoneNumber)
        {
            userData = await _userRepository.FindOneByUserAsync(phoneNumber);
            if (userData is null)
                throw new ArgumentNullException(nameof(userIdentifier), "用户不存在");
        }
        else if (userIdentifier is string email)
        {
            userData = await _userRepository.FindOneByUserAsync(email);
            if (userData is null)
                throw new ArgumentNullException(nameof(userIdentifier), "用户不存在");
        }
        else
        {
            throw new ArgumentException($"无效的用户标识符类型：{userIdentifier.GetType().Name}", nameof(userIdentifier));
        }

        var role = await _userRoleRepository.FindUserIdByRoleAsync(userData.UserGuid);

        try
        {
            if (await userData.VerifyByPassword(password))
                if (userData.UserAccessFail.CloseLockAsync())
                {
                    var listClaims = new List<Claim>
                    {
                        new(ClaimTypes.Name, userData.UserName),
                        new(ClaimTypes.Email, userData.UserEmail),
                        new(ClaimTypes.Role, role?.RoleName ?? "User"),
                        new(ClaimTypes.MobilePhone, userData.PhoneNumber?.PhoneCode ?? string.Empty),
                        new(ClaimTypes.Authentication, role?.RoleAuthority.ToString() ?? "0")
                    };

                    _loggerUser.LogInformation($"[{DateTime.UtcNow}] 用户 {userData.UserEmail} 验证通过，生成 Token");
                    return _jwtTokenServer.BuilderTokenAsync(listClaims, _optionsSnapshot.Value);
                }

            _loggerUser.LogWarning($"[{DateTime.UtcNow}] 用户 {userData.UserEmail} 密码错误");
            return "密码错误";
        }
        catch (Exception ex)
        {
            _loggerUser.LogError(ex, $"[{DateTime.UtcNow}] 用户 {userIdentifier} 登录过程中发生错误");
            return "登录失败";
        }
    }

    private static string SwitchRole(Roles roles)
    {
        if (roles is null)
            throw new ArgumentNullException(nameof(roles));

        return roles.RoleAuthority switch
        {
            RoleAuthority.Root => "Root",
            RoleAuthority.Admin => "Admin",
            RoleAuthority.User => "User",
            RoleAuthority.Guest => "Guest",
            _ => "未知角色"
        };
    }
}