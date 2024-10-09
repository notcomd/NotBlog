using System.Security.Claims;
using Identity.Domain.Entities;
using Identity.Domain.IRepository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT;

namespace Identity.Domain.Server;

public class UserRepositoryServer
{
    private readonly INotcomd_JwtTokenServer _jwtTokenServer;
    private readonly ILogger<IUserRepository> _loggerUser;
    private readonly ILogger<IUserRoleRepository> _loggerUserRole;
    private readonly IOptionsSnapshot<JwtOptions> _optionsSnapshot;

    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public UserRepositoryServer(IOptionsSnapshot<JwtOptions> optionsSnapshot, ILogger<IUserRepository> loggerUser,
        IUserRepository userRepository, IUserRoleRepository userRoleRepository,
        INotcomd_JwtTokenServer jwtTokenServer, ILogger<IUserRoleRepository> loggerUserRole)
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
        var us = await _userRepository.FindOneByUserAsync(phoneNumber);
        var role = await _userRoleRepository.FindByUserRoleAsync(us.UserRoleGuid);
        if (us is null && role is null)
        {
            _loggerUser.LogError($" {DateTime.UtcNow}  find {phoneNumber} is null return null ");
            throw new ArgumentNullException($"find phone is null!{nameof(phoneNumber)}");
        }

        if (await us.CheckByPasswordAsync(HashH256Tool.CreateHash256Async(password).GetAwaiter().GetResult()) &&
            await us.UserAccessFail.CloseLockAsync())
        {
            var listClaims = new List<Claim>
            {
                new(ClaimTypes.Name, us.UserName),
                new(ClaimTypes.Email, us.UserEmail),
                new(ClaimTypes.Role, role!.RoleName),
                new(ClaimTypes.MobilePhone, us.UserPhone!.PhoneCode)
            };
            return _jwtTokenServer.BuilderTokenAsync(listClaims, _optionsSnapshot.Value);
        }

        await us.UserAccessFail.FailAsync();
        return "密码错误";
    }

    public async ValueTask<string> LogInByCheckPasswordAsync(string email, string password, long code)
    {
        var us = await _userRepository.FindOneByUserAsync(email);
        var role = await _userRoleRepository.FindByUserRoleAsync(us.UserRoleGuid);

        if (us is null && role is null)
        {
            _loggerUser.LogError($" {DateTime.UtcNow}  find {email} is null return null ");
            _loggerUserRole.LogInformation($"[{DateTime.UtcNow}]");
            throw new ArgumentNullException("find phone is null!");
        }

        if (await us.CheckByPasswordAsync(HashH256Tool.CreateHash256Async(password).GetAwaiter().GetResult()) &&
            await us.UserAccessFail.CloseLockAsync())
        {
            var listClaims = new List<Claim>
            {
                new(ClaimTypes.Name, us.UserName),
                new(ClaimTypes.Email, us!.UserEmail),
                new(ClaimTypes.Role, role!.RoleName),
                new(ClaimTypes.MobilePhone, us.UserPhone!.PhoneCode)
            };
            return _jwtTokenServer.BuilderTokenAsync(listClaims, _optionsSnapshot.Value);
        }

        await us.UserAccessFail.FailAsync();
        return "凭证错误";
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