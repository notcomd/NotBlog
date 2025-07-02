using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Identity.Domain.Entities;
using Identity.Domain.IRepository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT;

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

    public async Task FailAsync(User user)
    {
        await user.UserAccessFail.FailAsync();
    }

    public async ValueTask<string> LogInByCheckPasswordAsync(PhoneNumber phoneNumber, string password, long code)
    {
        return await LogInByCheckPasswordCoreAsync(phoneNumber, password);
    }


    /// <summary>
    /// 登入验证
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public async ValueTask<string> LogInByCheckPasswordAsync([EmailAddress(ErrorMessage = "无效邮件地址")]string email, string password, string code)
    {
        var userData = await _userRepository.FindOneByUserAsync(email);
        if (userData is null)
        {
            _loggerUser.LogError($" {DateTime.UtcNow}  find {email} is null return null ");
            return $" {userData?.UserEmail}用户不存在！";
        }
        return await LogInByCheckPasswordCoreAsync(userData, password);

    }

    public async ValueTask<bool> SigInByCreateUserAsync(string email, string password, long code)
    {
        var usdata = await _userRepository.FindOneByUserAsync(email);
        if (usdata != null)
        {
            _loggerUser.LogInformation($"[{DateTime.UtcNow}]存在该用户", nameof(usdata));
            return false;
        }
        var salt = await HashH256Tool.GenerateSValueTask();
        var role = new UserRole(email);
        await _userRoleRepository.AddByUserRoleAsync(role);
        var passwordhash = await HashH256Tool.CreateHash256Async(password, salt);
        usdata = new User(role.UserRoleGuid, email, passwordhash, Convert.ToBase64String(salt), new Uri("https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png"));
        await _userRepository.AddOneByUserAsync(usdata);
        return true;
    }

    /// <summary>
    ///  登入验证核心方法
    /// </summary>
    /// <param name="userIdentifier">
    ///  用户标识符，可以是手机号或电子邮件地址。
    /// </param>
    /// <param name="password">
    ///  用户密码。
    /// </param>
    /// <returns>
    /// 返回一个字符串，表示登录结果。如果登录成功，将返回 JWT 令牌；如果失败，将返回错误信息。
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    private async ValueTask<string> LogInByCheckPasswordCoreAsync(object userIdentifier, string password)
    {
        User userData;
        if (userIdentifier is PhoneNumber phoneNumber)
        {
            userData = await _userRepository.FindOneByUserAsync(phoneNumber);
        }
        else if (userIdentifier is string email)
        {
            userData = await _userRepository.FindOneByUserAsync(email);
        }
        else
        {
            throw new ArgumentException("Invalid user identifier type");
        }

        if (userData is null)
        {
            _loggerUser.LogError($"User {userIdentifier} not found");
            return "111000";
        }

        var role = await _userRoleRepository.FindByUserRoleAsync(userData.UserRoleGuid);

        try
        {
            if (await userData.CheckByPasswordAsync(await HashH256Tool.CreateHash256Async(password, Encoding.UTF8.GetBytes(userData.Salt)), password, Encoding.UTF8.GetBytes(userData.Salt)))
            {
                if (await userData.UserAccessFail.CloseLockAsync())
                {
                    var listClaims = new List<Claim>
                    {
                        new(ClaimTypes.Name, userData!.UserName),
                        new(ClaimTypes.Email, userData!.UserEmail),
                        new(ClaimTypes.Role, role!.RoleName),
                        new(ClaimTypes.MobilePhone, userData.UserPhone!.PhoneCode),
                        new(ClaimTypes.Authentication, role!.LimitsOfAuthority.ToString())
                    };
                    _loggerUser.LogInformation($"date:[{userData.UserEmail}] 通验证，Token");
                    return _jwtTokenServer.BuilderTokenAsync(listClaims, _optionsSnapshot.Value);
                }
            }
            await FailAsync(userData);
            return "密码错误";
        }
        catch (Exception ex)
        {
            _loggerUser.LogError(ex, $"Error during login for user {userIdentifier}");
            return "登录失败";
        }
    }
}