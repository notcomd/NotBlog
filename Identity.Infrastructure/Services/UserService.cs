using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Identity.Domain.IService;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;

namespace Identity.Infrastructure.Services;

public class UserService(
    IOptionsSnapshot<JwtOptions> optionsSnapshot,
    ILogger<IUserRepository> loggerUser,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IJwtTokenService jwtTokenServer,
    ILogger<IUserRoleRepository> loggerUserRole)
    : IUserService
{
    public async Task<bool> SignInByCreateUserAsync(string email, string password, string code)
    {
        var userData = await userRepository.FindOneByUserAsync(email);
        if (userData is not null)
        {
            loggerUser.LogError($"[{DateTime.UtcNow}] 用户 {email} 已存在");
            return false;
        }

        Roles? userRole = null;
        if (!await userRoleRepository.IsUserRoleAsync("User"))
            userRole = Roles.RoleFactory.CreateUserRole();
        if (userRole is null)
        {
            loggerUserRole.LogError($"[{DateTime.UtcNow}] 创建用户角色失败");
            return false;
        }

        var newUser = await User.CreateByEmailUser(
            userRole.RoleGuid, email,
            password,
            null, null);
        await userRepository.AddOneByUserAsync(newUser);

        return true;
    }

    public async Task ResetPasswordAsync(string email, string password, string code)
    {
        var userData = await userRepository.FindOneByUserAsync(email);
        if (userData is null)
        {
            loggerUser.LogError($"[{DateTime.UtcNow}] 用户 {email} 不存在");
            return;
        }
    }

    public Task SendResetPasswordEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    public Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    ///  登入验证
    /// </summary>
    /// <param name="email">电子邮件地址</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码（当前未使用）</param>
    /// <returns>成功返回 JWT 令牌，失败返回错误信息</returns>
    public async Task<string> LogInByCheckPasswordAsync(
        [EmailAddress(ErrorMessage = "无效邮件地址")]
        string email,
        string password,
        string code)
    {
        var userData = await userRepository.FindOneByUserAsync(email);
        if (userData is null)
        {
            loggerUser.LogError($"[{DateTime.UtcNow}] 用户 {email} 不存在");
            return $"用户 {email} 不存在！";
        }

        return await LogInByCheckPasswordCoreAsync(userData, password);
    }
    // private readonly ILogger<IUserRoleRepository> _loggerUserRole = loggerUserRole;

    public async Task<string> LogInByCheckPasswordAsync(PhoneNumber phoneNumber, string password, long code)
    {
        var userData = await userRepository.FindOneByUserAsync(phoneNumber);
        if (userData is not null) return await LogInByCheckPasswordCoreAsync(phoneNumber, password);
        loggerUser.LogError($"[{DateTime.UtcNow}] 用户 {phoneNumber} 不存在");
        return $"用户 {phoneNumber} 不存在！";
    }


    /// <summary>
    ///  登入验证核心方法
    /// </summary>
    /// <param name="userIdentifier">用户标识符，可以是手机号或电子邮件地址</param>
    /// <param name="password">用户密码</param>
    /// <returns>返回一个字符串，表示登录结果。成功返回 JWT 令牌，失败返回错误信息</returns>
    /// <exception cref="ArgumentException">当用户标识符类型无效时抛出</exception>
    /// <exception cref="ArgumentNullException">当用户不存在时抛出</exception>
    private async ValueTask<string> LogInByCheckPasswordCoreAsync(object userIdentifier, string password)
    {
        User? userData;
        Roles? roleData;
        if (userIdentifier is PhoneNumber phoneNumber)
        {
            userData = await userRepository.FindOneByUserAsync(phoneNumber);
            if (userData is null)
                throw new ArgumentNullException(nameof(userIdentifier), "用户不存在");
        }
        else if (userIdentifier is string email)
        {
            userData = await userRepository.FindOneByUserAsync(email);
            if (userData is null)
                throw new ArgumentNullException(nameof(userIdentifier), "用户不存在");
        }
        else
        {
            throw new ArgumentException($"无效的用户标识符类型：{userIdentifier.GetType().Name}", nameof(userIdentifier));
        }

        var roleName = SwitchRole(await GetRoleName(userData));

        try
        {
            if (await userData.VerifyByPasswordAsync(password))
                if (userData.UserAccessFail.CloseLockAsync())
                {
                    var listClaims = new List<Claim>
                    {
                        new(ClaimTypes.Name, userData.UserName ??
                                             throw new ArgumentNullException(nameof(userData.UserName), "用户名不能为空")),
                        new(ClaimTypes.Email, userData.UserEmail ??
                                              throw new ArgumentNullException(nameof(userData.UserEmail), "用户邮箱不能为空")),
                        new(ClaimTypes.Role, roleName ?? "User"),
                        new(ClaimTypes.MobilePhone, userData.PhoneNumber?.PhoneCode ?? string.Empty),
                    };
                    loggerUser.LogInformation($"[{DateTime.UtcNow}] 用户 {userData.UserEmail} 验证通过，生成 Token");
                    return jwtTokenServer.BuilderTokenAsync(listClaims, optionsSnapshot.Value);
                }

            loggerUser.LogWarning($"[{DateTime.UtcNow}] 用户 {userData.UserEmail} 密码错误");
            return "密码错误";
        }
        catch (Exception ex)
        {
            // SwitchRole(await GetRoleName(userData));
            loggerUser.LogError(ex, $"[{DateTime.UtcNow}] 用户 {userIdentifier} 登录过程中发生错误");
            return "登录失败";
        }
    }

    private string SwitchRole(IEnumerable<RoleAuthority> roles)
    {
        var roleAuthorities = roles as RoleAuthority[] ?? roles.ToArray();
        if (!roleAuthorities.Any())
            return "User";
        if (roleAuthorities.Contains(RoleAuthority.Root))
            return "Root";
        if (roleAuthorities.Contains(RoleAuthority.Admin))
            return "Admin";
        if (roleAuthorities.Contains(RoleAuthority.User))
            return "User";
        return "Guest";
    }

    private async Task<IEnumerable<RoleAuthority>> GetRoleName(User? user)
    {
        if (user is null) return Enumerable.Empty<RoleAuthority>();
        List<RoleAuthority> roleNames = new();
        foreach (var roleGuid in user.UserRoleGuid)
        {
            var role = await userRoleRepository.FindByUserRoleAsync(roleGuid);
            if (role is null) continue;
            roleNames.Add(role.RoleAuthority);
        }

        return roleNames;
    }
}