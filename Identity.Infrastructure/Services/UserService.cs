using CacheMemory.Core;

namespace Identity.Infrastructure.Services;

public class UserService(
    IOptionsSnapshot<JwtOptions> optionsSnapshot,
    IDistributedCache distributedCache,
    ILogger<IUserRepository> loggerUser,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IJwtTokenService jwtTokenServer,
    ICacheMemory<TokenCacheEntry> cacheMemory,
    ILogger<IUserRoleRepository> loggerUserRole)
    : IUserService
{
    // ── 缓存键定义 ──
    private const string AccessTokenKeyPrefix = "auth:token";
    private const string RefreshTokenKeyPrefix = "auth:refresh";


    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="email">电子邮件地址</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码（当前未使用）</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    public async Task<bool> RegisterByCreateUserAsync(string email, string password, string code)
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

    /// <summary>
    /// 重置密码
    /// </summary>
    /// <param name="email">电子邮件地址</param>
    /// <param name="password">新密码</param>
    /// <param name="code">验证码（当前未使用）</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    public async Task ChangeByPasswordAsync(string email, string password, string code)
    {
        var userData = await userRepository.FindOneByUserAsync(email);
        if (userData is null)
        {
            loggerUser.LogError($"[{DateTime.UtcNow}] 用户 {email} 不存在");
            return;
        }

        await userData.ChangeByPasswordAsync(password);
        //await userRepository.
        loggerUser.LogInformation($"[{DateTime.UtcNow}] 用户 {email} 密码重置成功");
    }

    /// <summary>
    /// 发送重置密码邮件
    /// </summary>
    /// <param name="email">电子邮件地址</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    public Task SendResetPasswordEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<User>> GetAllUsersAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 手机号登入验证
    /// </summary>
    public async Task<TokenResult?> LogInByCheckPasswordAsync(PhoneNumber phoneNumber, string password, string code)
    {
        var userData = await userRepository.FindOneByUserAsync(phoneNumber);
        if (string.IsNullOrWhiteSpace(code) && string.Equals(code, "213123"))
            if (userData is null)
            {
                loggerUser.LogError("[{DateTime}] 用户 {PhoneNumber} 不存在", DateTime.UtcNow, phoneNumber);
                return null;
            }

        if (userData is not null) return await LogInByCheckPasswordCoreAsync(userData, password);
        return null;
    }

    Task<TokenResult?> IUserService.LogInByCheckPasswordAsync(string email, string password, string code)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 获取 OAuth 授权链接
    /// </summary>
    /// <param name="provider">OAuth提供程序</param>
    /// <param name="redirectUri">重定向 URI</param>
    /// <returns>OAuth 授权链接</returns>
    /// <exception cref="ArgumentException">提供程序无效</exception>
    /// <exception cref="ArgumentNullException">重定向 URI 为空</exception>
    public Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    ///  邮箱登入验证
    /// </summary>
    /// <param name="email">电子邮件地址</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码（当前未使用）</param>
    /// <returns>成功返回 TokenResult，失败返回 null</returns>
    public async Task<TokenResult?> LogInByCheckPasswordAsync(
        [EmailAddress(ErrorMessage = "无效邮件地址")]
        string email,
        string password,
        string code)
    {
        var userData = await userRepository.FindOneByUserAsync(email);
        if (userData is null)
        {
            loggerUser.LogError($"[{DateTime.UtcNow}] 用户 {email} 不存在");
            return null;
        }

        return await LogInByCheckPasswordCoreAsync(userData, password);
    }


    /// <summary>
    /// 登入验证核心方法
    /// 
    /// 职责:
    ///   1. 验证用户密码
    ///   2. 解除账号锁定
    ///   3. 构建 Claims 并生成 AccessToken + RefreshToken
    ///   4. 将两个 Token 分别存入分布式缓存
    /// </summary>
    /// <param name="userData">已验证存在的用户实体</param>
    /// <param name="password">用户密码</param>
    /// <returns>成功返回 TokenResult，密码错误或账号锁定返回 null</returns>
    private async ValueTask<TokenResult?> LogInByCheckPasswordCoreAsync(User userData, string password)
    {
        //var tokens = (AccessToken: string.Empty, RefreshToken: string.Empty);
        // ── 第一步: 验证密码 ──
        if (!await userData.VerifyByPasswordAsync(password))
        {
            loggerUser.LogWarning($"[{DateTime.UtcNow}] 用户 {userData.UserEmail} 密码错误");
            return null;
        }

        // ── 第二步: 解除账号锁定 ──
        if (!userData.UserAccessFail.CloseLockAsync())
        {
            loggerUser.LogWarning($"[{DateTime.UtcNow}] 用户 {userData.UserEmail} 账号已锁定，无法关闭锁定状态");
            return null;
        }

        try
        {
            // ── 第三步: 构建 Claims ──
            var roleName = SwitchRole(await GetRoleName(userData));
            var claims = BuildClaims(userData, roleName);

            // ── 第四步: 生成双 Token ──
            var config = optionsSnapshot.Value;
            var tokenData = await jwtTokenServer.BuildTokenAsync(claims, config);


            // ── 第五步: 写入缓存 ──
            //await cacheMemory.SetObjectAsync(userData.UserGuid.ToString(), tokenData, tokenData.ExpiresAt - DateTimeOffset.Now);
            // var tokenEntity = new TokenCacheEntry
            // {
            //     UserGuid = userData.UserGuid,
            //     Token = tokenData.AccessToken,
            //     CreatedAt = DateTimeOffset.Now,
            //     ExpiresAt = tokenData.ExpiresAt,
            //     TokenType = tokenData.TokenType,
            //     LinkedAccessToken = tokenData.RefreshToken
            // };
            //
            // await cacheMemory.SetAsync(userData.UserEmail, tokenEntity);

            await CacheTokensAsync(userData.UserGuid, tokenData, config);

            loggerUser.LogInformation(
                $"[{DateTime.UtcNow}] 用户 {userData.UserEmail} 验证通过，" +
                $"Token 已生成 (AccessToken 过期: {config.ExpireSeconds} 秒，RefreshToken 过期: {config.RefreshTokenExpireSeconds} 秒)");

            return tokenData;
        }
        catch (Exception ex)
        {
            loggerUser.LogError(ex, $"[{DateTime.UtcNow}] 用户 {userData.UserEmail} Token 生成失败");
            return null;
        }
    }


    /// <summary>
    /// 将 AccessToken 和 RefreshToken 分别存入分布式缓存
    /// 
    /// 缓存键格式:
    ///   AccessToken:  "auth:token:{userGuid}"
    ///   RefreshToken: "auth:refresh:{userGuid}"
    /// </summary>
    private async Task CacheTokensAsync(Guid userGuid, TokenResult tokenResult, JwtOptions config)
    {
        // AccessToken 缓存（生命周期与 Token 本身一致）
        var accessTokenEntry = new TokenCacheEntry
        {
            Token = tokenResult.AccessToken,
            UserGuid = userGuid,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = tokenResult.ExpiresAt,
            TokenType = tokenResult.TokenType,
            LinkedAccessToken = tokenResult.RefreshToken
        };

        var accessKey = $"{AccessTokenKeyPrefix}:{userGuid}";
        var accessTtl = config.ExpireSeconds > 0
            ? TimeSpan.FromSeconds(config.ExpireSeconds)
            : TimeSpan.FromHours(1);

        await cacheMemory.SetAsync(accessKey, accessTokenEntry, accessTtl);
        /*await distributedCache.SetStringAsync(
            accessKey,
            JsonSerializer.Serialize(accessTokenEntry),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = accessTtl });*/

        // RefreshToken 缓存（生命周期更长，默认为 7 天）
        if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
        {
            var refreshTokenEntry = new TokenCacheEntry
            {
                Token = tokenResult.RefreshToken,
                UserGuid = userGuid,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(config.RefreshTokenExpireSeconds),
                TokenType = "refresh",
                LinkedAccessToken = tokenResult.AccessToken
            };

            var refreshKey = $"{RefreshTokenKeyPrefix}:{userGuid}";
            var refreshTtl = config.RefreshTokenExpireSeconds > 0
                ? TimeSpan.FromSeconds(config.RefreshTokenExpireSeconds)
                : TimeSpan.FromDays(7);

            // await distributedCache.SetStringAsync(
            //     refreshKey,
            //     JsonSerializer.Serialize(refreshTokenEntry),
            //     new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = refreshTtl });

            await cacheMemory.SetAsync(refreshKey, refreshTokenEntry, refreshTtl);
        }
    }


    /// <summary>
    /// 从用户实体构建 JWT Claims
    /// </summary>
    /// <param name="userData">用户实体</param>
    /// <param name="roleName">角色名称</param>
    /// <returns>JWT Claims 列表</returns>
    private static List<Claim> BuildClaims(User userData, string roleName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userData.UserGuid.ToString()),
            new(ClaimTypes.Name, userData.UserName ??
                                 throw new InvalidOperationException("用户名不能为空")),
            new(ClaimTypes.Email, userData.UserEmail ??
                                  throw new InvalidOperationException("用户邮箱不能为空")),
            new(ClaimTypes.Role, roleName),
            new("user_guid", userData.UserGuid.ToString())
        };

        // 可选: 手机号
        if (!string.IsNullOrEmpty(userData.PhoneNumber?.PhoneCode))
            claims.Add(new Claim(ClaimTypes.MobilePhone, userData.PhoneNumber.PhoneCode));

        return claims;
    }


    /// <summary>
    /// 解析用户角色权限为字符串表示
    /// </summary>
    /// <param name="roles">角色权限列表</param>
    /// <returns>角色名称</returns>
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

    /// <summary>
    /// 获取用户角色
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>角色权限列表</returns>
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

/// <summary>
/// 缓存中的 Token 实体
/// </summary>
public sealed class TokenCacheEntry : IMemory
{
    /// <summary>Token 值</summary>
    public string Token { get; init; } = null!;

    /// <summary>所属用户</summary>
    public Guid UserGuid { get; init; }

    /// <summary>创建时间</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>过期时间</summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Token 类型 (access / refresh)</summary>
    public string TokenType { get; init; } = null!;

    /// <summary>关联的 AccessToken（仅 RefreshToken 缓存条目使用）</summary>
    public string? LinkedAccessToken { get; init; }
}