using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Identity.Domain.Entities;
using Identity.Domain.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT;

namespace Identity.Domain.Server;

public class UserRepositoryServer
{
    private readonly IJwtTokenOptions _jwtTokenServer;
    private readonly ILogger<IUserRepository> _loggerUser;
    private readonly ILogger<IUserRoleRepository> _loggerUserRole;
    private readonly IOptionsSnapshot<JwtOptions> _optionsSnapshot;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public UserRepositoryServer(IOptionsSnapshot<JwtOptions> optionsSnapshot, ILogger<IUserRepository> loggerUser,
        IUserRepository userRepository, IUserRoleRepository userRoleRepository,
        IJwtTokenOptions jwtTokenServer, ILogger<IUserRoleRepository> loggerUserRole)
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

    public async ValueTask<ActionResult<string>> LogInByCheckPasswordAsync([EmailAddress(ErrorMessage = "无效邮件地址")]string email, string password, string code)
    {
        var userData = await _userRepository.FindOneByUserAsync(email);
        if (userData is null)
        {
            _loggerUser.LogError($" {DateTime.UtcNow}  find {email} is null return null ");
            return new ActionResult<string>("用户不存在");
        }
        return await LogInByCheckPasswordCoreAsync(userData, password);
    }

    public async ValueTask SigInByCreateUserAsync(string email, string password, long code)
    {
        var usdata = await _userRepository.FindOneByUserAsync(email);
        if (usdata != null)
        {
            _loggerUser.LogInformation($"[{DateTime.UtcNow}]存在该用户", nameof(usdata));
            return;
        }
        var salt = await HashH256Tool.GenerateSValueTask();
        var role = new UserRole(email);
        await _userRoleRepository.AddByUserRoleAsync(role);
        var passwordhash = await HashH256Tool.CreateHash256Async(password, salt);
        usdata = new User(role.UserRoleGuid, email, passwordhash, Convert.ToBase64String(salt));
        await _userRepository.AddOneByUserAsync(usdata);
    }

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
                        new(ClaimTypes.Name, userData.UserName),
                        new(ClaimTypes.Email, userData.UserEmail),
                        new(ClaimTypes.Role, role!.RoleName),
                        new(ClaimTypes.MobilePhone, userData.UserPhone!.PhoneCode),
                        new(type: ClaimTypes.Authentication, role!.LimitsOfAuthority.ToString())
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