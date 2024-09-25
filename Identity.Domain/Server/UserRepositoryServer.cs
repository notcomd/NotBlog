using System.Security.Claims;
using Identity.Domain.Entities;
using Identity.Domain.IRepository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT;

namespace Identity.Domain.Server;

public class UserRepositoryServer
{

    private readonly IUserRepository _userRepository ;
    private readonly ILogger _logger;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IOptionsSnapshot<Notcomd_JwtOptions> _optionsSnapshot;

    private readonly INotcomd_JwtTokenServer _jwtTokenServer;
    public UserRepositoryServer(IOptionsSnapshot<Notcomd_JwtOptions> optionsSnapshot,ILogger logger,IUserRepository userRepository, IUserRoleRepository userRoleRepository,INotcomd_JwtTokenServer jwtTokenServer)
    {
        _jwtTokenServer = jwtTokenServer;
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
        _logger = logger;
        _optionsSnapshot = optionsSnapshot;
    }

    public async ValueTask<string> LogInByCheckPasswordAsync(PhoneNumber phoneNumber,string password,long codeing)
    {
        
        var us = await _userRepository.FindOneByUserAsync(phoneNumber);
        var role = await _userRoleRepository.FindByUserRoleAsync(us.UserRoleGuid);
        if (us is null && role is null)
        {
            _logger.LogError($" {DateTime.UtcNow}  find {phoneNumber} is null return null ");
            throw new ArgumentNullException($"find phone is null!{nameof(phoneNumber)}");
        }
        if (await us.CheckByPasswordAsync(HashH256Tool.CreateHash256Async(password).GetAwaiter().GetResult())&& await us.UserAccessFail.CloseLockAsync())
        {
            var listClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, us.UserName),
                new Claim(ClaimTypes.Email, us!.UserEmail),
                new Claim(ClaimTypes.Role, role!.RoleName),
                new Claim(ClaimTypes.MobilePhone,us.UserPhone.PhoneCode)
            };
            return _jwtTokenServer.BuilderTokenAsync(listClaims,_optionsSnapshot.Value);
        }
        await us.UserAccessFail.FailAsync();
        return "密码错误";
    }

    public async ValueTask<string> LogInByCheckPasswordAsync(string email, string password, long codeing)
    {
        var us = await _userRepository.FindOneByUserAsync(email);
        var role = await _userRoleRepository.FindByUserRoleAsync(us.UserRoleGuid);
        
        if (us is null && role is null)
        {
            _logger.LogError($" {DateTime.UtcNow}  find {email} is null return null ");
            throw new ArgumentNullException("find phone is null!");
        }
        if (await us.CheckByPasswordAsync(HashH256Tool.CreateHash256Async(password).GetAwaiter().GetResult()) && await us.UserAccessFail.CloseLockAsync())
        {
            var listClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, us.UserName),
                new Claim(ClaimTypes.Email, us!.UserEmail),
                new Claim(ClaimTypes.Role, role!.RoleName)
            };
            return _jwtTokenServer.BuilderTokenAsync(listClaims,_optionsSnapshot.Value);
        }
        await  us.UserAccessFail.FailAsync();
        return "凭证错误";
    }
    
    public ValueTask SigInByCreateUserAsync(string email, string password, long codeing)
    {

        return ValueTask.CompletedTask;
    }
}