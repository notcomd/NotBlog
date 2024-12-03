using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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

    public async Task FilyAsync(User user)
    {
        await user.UserAccessFail.FailAsync();
    }

    public async ValueTask<string> LogInByCheckPasswordAsync(PhoneNumber phoneNumber, string password, long code)
    {
        var us = await _userRepository.FindOneByUserAsync(phoneNumber);
        if (us == null)
        {
            _loggerUser.LogError($"User {phoneNumber} not found");
            return "111000";
        }
        var role = await _userRoleRepository.FindByUserRoleAsync(us!.UserRoleGuid);

        if (await us!.CheckByPasswordAsync(HashH256Tool.CreateHash256Async(password).GetAwaiter().GetResult()) &&
            await us.UserAccessFail.CloseLockAsync())
        {
            var listClaims = new List<Claim>
            {
                new(ClaimTypes.Name, us.UserName),
                new(ClaimTypes.Email, us.UserEmail),
                new(ClaimTypes.Role, role!.RoleName),
                new(ClaimTypes.MobilePhone, us.UserPhone!.PhoneCode),
                new(type: ClaimTypes.Authentication, role!.LimitsOfAuthority.ToString())
            };
            _loggerUser.LogInformation($"date:[{us.UserEmail}通验证，Token]");
            return _jwtTokenServer.BuilderTokenAsync(listClaims, _optionsSnapshot.Value);
        }

        await us.UserAccessFail.FailAsync();
        return "密码错误";
    }

    public async ValueTask<ActionResult<string>> LogInByCheckPasswordAsync([EmailAddress(ErrorMessage = "无效邮件地址")]string email, string password, string code)
    {
        var userData = await _userRepository.FindOneByUserAsync(email);
        if (userData is null)
        {
            _loggerUser.LogError($" {DateTime.UtcNow}  find {email} is null return null ");
            return string.Format("没有该角色");
        }
        //var UserAccess = new UserAccessFail(userData);
        var userRole = await _userRoleRepository.FindByUserRoleAsync(userData.UserRoleGuid);
        if (await userData.CheckByPasswordAsync(await HashH256Tool.CreateHash256Async(password)))
        {
            if (await userData.UserAccessFail.CloseLockAsync())
            {

            }
            var listClaims = new List<Claim>
            {
                new(ClaimTypes.Name, userData.UserName),
                new(ClaimTypes.Email, userData.UserEmail),
                // new(ClaimTypes.Role, userRole!.RoleName),
                // new(type:ClaimTypes.Authentication,userRole!.LimitsOfAuthority.ToString()),
                new(ClaimTypes.MobilePhone, userData.UserPhone!.PhoneCode)
            };
            return _jwtTokenServer.BuilderTokenAsync(listClaims, _optionsSnapshot.Value);
        }
        await FilyAsync(userData);
        return new ActionResult<string>("无效凭证");
    }

    public async ValueTask SigInByCreateUserAsync(string email, string password, long code)
    {
        var usdata = await _userRepository.FindOneByUserAsync(email);
        if (usdata != null)
        {
            _loggerUser.LogInformation($"[{DateTime.UtcNow}]存在该用户", nameof(usdata));
            return;
        }
        var role = new UserRole(email);
        await _userRoleRepository.AddByUserRoleAsync(role);
        var passwordhash = await HashH256Tool.CreateHash256Async(password);
        usdata = new User(role.UserRoleGuid, email, passwordhash);
        await _userRepository.AddOneByUserAsync(usdata);
    }
}