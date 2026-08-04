using CacheMemory.Core;
using Identity.Domain.Events;
using Identity.Domain.ICache;

namespace Identity.Infrastructure.Services;

public class UserService(
    IOptionsSnapshot<JwtOptions> optionsSnapshot,
    ILogger<IUserRepository> loggerUser,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IJwtTokenService jwtTokenServer,
    ICacheMemory<TokenCacheEntry> cacheMemory,
    IIdentityCacheService identityCacheService)
    : IUserService
{
    private const string AccessTokenKeyPrefix = "auth:token";
    private const string RefreshTokenKeyPrefix = "auth:refresh";
    private const string EmailCodeKeyPrefix = "Login_";

    // ── 邮箱密码登录 ──

    public async Task<TokenResult?> LogInByCheckPasswordAsync(
        [EmailAddress(ErrorMessage = "邮件地址不符合要求喵！")] string email,
        string password, string? code)
    {
        var userData = await userRepository.FindOneByUserAsync(email);
        // S-13：用户不存在与密码错误返回同一结果，避免账号枚举
        if (userData is null)
            return null;

        // 邮箱登录验证码：传了就校验（一次性消费），未传则放行纯密码登录
        if (!string.IsNullOrWhiteSpace(code))
        {
            var cached = await identityCacheService.GetStringAsync($"{EmailCodeKeyPrefix}{email}", default);
            if (string.IsNullOrEmpty(cached) || !string.Equals(cached, code, StringComparison.Ordinal))
                return null;
            await identityCacheService.RemoveAsync($"{EmailCodeKeyPrefix}{email}", default);
        }

        return await LogInByCheckPasswordCoreAsync(userData, password);
    }

    // ── 手机号密码登录 ──

    public async Task<TokenResult?> LogInByCheckPasswordAsync(PhoneNumber phoneNumber, string password, string code)
    {
        var userData = await userRepository.FindOneByUserAsync(phoneNumber);
        if (userData is null)
        {
            loggerUser.LogError("[{DateTime}] 用户 {PhoneNumber} 不存在", DateTime.UtcNow, phoneNumber);
            return null;
        }

        return await LogInByCheckPasswordCoreAsync(userData, password);
    }

    // ── 注册 ──
    // 注：邮箱注册走 RegisterByUserCommandHandler（命令链路，含验证码校验+事件发布），
    // 此前残留的 RegisterByCreateUserAsync 已删除（死代码且未 SaveChanges）。

    // ── 用户信息查询 ──

    public async Task<User?> GetUserInfoAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await userRepository.FindOneByUserAsync(email);
    }

    public async Task<ICollection<User>> FindUserByVagueAsync()
    {
        // 当前无筛选条件，返回全部用户（含 UserSafety / UserAccessFail 导航）
        return await userRepository.FindAllByUserAsync();
    }

    // ── 登录核心 ──

    /// <summary>
    /// 登入验证核心方法
    /// 
    /// 职责:
    ///   1. 验证用户密码
    ///   2. 解除账号锁定
    ///   3. 构建 Claims 并生成 AccessToken + RefreshToken
    ///   4. 将两个 Token 分别存入缓存
    /// </summary>
    private async ValueTask<TokenResult?> LogInByCheckPasswordCoreAsync(User userData, string password)
    {
        // ── 第一步: 检查是否被锁定 ──
        if (!userData.UserAccessFail.CanLogin())
        {
            loggerUser.LogWarning("[{DateTime}] 用户 {UserEmail} 账号已锁定，拒绝登录", DateTime.UtcNow, userData.UserEmail);
            return null;
        }

        // ── 第二步: 验证密码（成功时自动清零失败计数 / 旧哈希重哈希升级）──
        if (!await userData.VerifyByPasswordAsync(password))
        {
            // S-13：失败计数原子递增（ExecuteUpdate，并发安全），达到阈值时锁定账号
            var failCount = await userRepository.IncrementAccessFaildCountAsync(userData.UserGuid);
            if (failCount > UserAccessFail.MaxFailedAttempts)
            {
                await userRepository.LockUserAsync(
                    userData.UserGuid, DateTimeOffset.UtcNow.Add(UserAccessFail.LockOutDuration));
                userData.AddDomainEvent(new AccountLockedEvent(userData.UserGuid));
            }

            await userRepository.UnitOfWork.SaveChangesAsync();
            loggerUser.LogWarning("[{DateTime}] 用户 {UserEmail} 密码错误", DateTime.UtcNow, userData.UserEmail);
            return null;
        }

        await userRepository.UnitOfWork.SaveChangesAsync();

        try
        {
            var roleName = await GetRoleNameAsync(userData.UserRoleGuid);
            var claims = BuildClaims(userData, roleName.ToHashSet());

            var config = optionsSnapshot.Value;
            var tokenData = await jwtTokenServer.BuildTokenAsync(claims, config);

            await CacheTokensAsync(userData.UserGuid, tokenData, config);

            loggerUser.LogInformation(
                "[{DateTime}] 用户 {UserEmail} 验证通过，" +
                "Token 已生成 (AccessToken 过期: {ExpireSeconds} 秒，RefreshToken 过期: {RefreshSeconds} 秒)",
                DateTime.UtcNow, userData.UserEmail, config.ExpireSeconds, config.RefreshTokenExpireSeconds);

            return tokenData;
        }
        catch (Exception ex)
        {
            loggerUser.LogError(ex, "[{DateTime}] 用户 {UserEmail} Token 生成失败", DateTime.UtcNow, userData.UserEmail);
            return null;
        }
    }

    // ── Token 缓存 ──

    /// <summary>
    /// 将 AccessToken 和 RefreshToken 分别存入缓存
    /// </summary>
    private async Task CacheTokensAsync(Guid userGuid, TokenResult tokenResult, JwtOptions config)
    {
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

            await cacheMemory.SetAsync(refreshKey, refreshTokenEntry, refreshTtl);
        }
    }

    // ── Claims 构建 ──

    /// <summary>
    /// 从用户实体构建 JWT Claims
    /// </summary>
    private static List<Claim> BuildClaims(User userData, HashSet<string> roleName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userData.UserGuid.ToString()),
            new(ClaimTypes.Name, userData.UserName ??
                                 throw new InvalidOperationException("用户名不能为空")),
            new(ClaimTypes.Email, userData.UserEmail ??
                                  throw new InvalidOperationException("用户邮箱不能为空")),
            new(ClaimTypes.Role, string.Join(",", roleName))
        };

        if (!string.IsNullOrEmpty(userData.PhoneNumber?.PhoneCode))
            claims.Add(new Claim(ClaimTypes.MobilePhone, userData.PhoneNumber.PhoneCode));

        return claims;
    }

    // ── 角色查询 ──

    /// <summary>
    /// 获取角色名称
    /// </summary>
    private async Task<IEnumerable<string>> GetRoleNameAsync(IEnumerable<Guid> roleGuids)
    {
        var roleNames = new List<string>();
        foreach (var roleGuid in roleGuids)
        {
            var role = await userRoleRepository.FindByUserRoleAsync(roleGuid);
            if (role is null) continue;
            roleNames.Add(role.RoleName);
        }

        return roleNames;
    }
}

/// <summary>
/// 缓存中的 Token 实体
/// </summary>
public sealed class TokenCacheEntry : IMemory
{
    public string Token { get; init; } = null!;
    public Guid UserGuid { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string TokenType { get; init; } = null!;
    public string? LinkedAccessToken { get; init; }
}
