using Identity.Domain.ICache;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;

namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 统一登录命令处理器（登录/注册二合一）：
/// 1. 密码登入：锁定检查 → 密码校验（失败原子递增失败计数，达阈值锁定）→ 二次验证（开启用户校验并一次性消费邮箱验证码）；
/// 2. 验证码登入：校验并一次性消费验证码 → 查无用户则派发 <see cref="RegisterByEmailCommand"/> 自动注册 → 锁定检查；
/// 3. 公共尾部：构建 Claims → 签发 AccessToken/RefreshToken → 登记多设备会话。
/// 增（自动注册）改（失败计数/锁定）事务统一经本命令以 UnitOfWork 提交，不再散落在基础设施服务中；
/// 邮件仅入后台队列（P6），不占用数据库事务做 SMTP 外部 IO。
/// </summary>
public class LogInCommandHandler(
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IIdentityCacheService identityCacheService,
    IJwtTokenService jwtTokenService,
    ITokenSessionService tokenSessionService,
    IOptionsSnapshot<JwtOptions> optionsSnapshot,
    INotMediator mediator,
    ILogger<LogInCommandHandler> logger)
    : IRequestHandler<LogInCommand, LogInCommandResult?>
{
    private const string EmailCodeKeyPrefix = "Login_";

    /// <summary>身份校验通过的中间产物（User 为 null 表示认证失败）</summary>
    private sealed record AuthenticatedUser(User? User, bool IsNewUser);

    public async Task<LogInCommandResult?> Handler(LogInCommand request, CancellationToken cancellationToken)
    {
        var auth = request.IsPasswordLogin
            ? await LogInByPasswordAsync(request, cancellationToken)
            : await LogInByEmailCodeAsync(request, cancellationToken);

        if (auth.User is null)
            return null;

        var token = await BuildTokenForUserAsync(auth.User);
        return token is null
            ? null
            : new LogInCommandResult(token, auth.IsNewUser, auth.User.UserGuid,
                auth.User.UserEmail, auth.User.UserName, auth.User.AvatarUrl);
    }

    /// <summary>
    /// 验证码登入（含自动注册）：校验并一次性消费验证码后，邮箱不存在则自动注册并下发初始密码。
    /// </summary>
    private async Task<AuthenticatedUser> LogInByEmailCodeAsync(LogInCommand request, CancellationToken cancellationToken)
    {
        // 校验并一次性消费验证码（放入缓存即视为已下发）
        if (!await ConsumeEmailCodeAsync(request.Email, request.Code, cancellationToken))
            return new(null, false);

        var userData = await FindUserAsync(request.Email);
        var isNewUser = false;
        if (userData is null)
        {
            // 陌生邮箱 → 自动注册：注册命令创建账号并生成初始密码邮件下发
            var registered = await mediator.SendAsync(new RegisterByEmailCommand(request.Email), cancellationToken);
            if (registered is null)
                return new(null, false);
            userData = registered.User;
            isNewUser = registered.IsNewUser;
        }

        // 锁定检查（自动注册后一并检查）
        if (!userData.UserAccessFail.CanLogin())
        {
            logger.LogWarning("[{DateTime}] 用户 {UserEmail} 账号已锁定，拒绝登录", DateTime.UtcNow, userData.UserEmail);
            return new(null, false);
        }

        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return new(userData, isNewUser);
    }

    /// <summary>
    /// 密码登入（邮箱 + 密码 + 二次验证）：邮箱不存在或锁定时拒绝（密码登入不做自动注册，杜绝邮箱枚举）；
    /// 密码错误原子递增失败计数并达到阈值时锁定；开启二次验证的用户再校验邮箱验证码。
    /// </summary>
    private async Task<AuthenticatedUser> LogInByPasswordAsync(LogInCommand request, CancellationToken cancellationToken)
    {
        var userData = await FindUserAsync(request.Email);
        if (userData is null)
            return new(null, false); // 密码登入不自动注册，杜绝邮箱枚举

        // 锁定检查
        if (!userData.UserAccessFail.CanLogin())
        {
            logger.LogWarning("[{DateTime}] 用户 {UserEmail} 账号已锁定，拒绝密码登入", DateTime.UtcNow, userData.UserEmail);
            return new(null, false);
        }

        // 密码校验
        if (!await userData.VerifyByPasswordAsync(request.Password!))
        {
            await RecordAccessFailAsync(userData.UserGuid);
            return new(null, false);
        }

        // 二次验证（仅开启二次验证的用户需校验邮箱验证码）
        if (userData.UserSafety.IsTwoFactorEnabled &&
            !await ConsumeEmailCodeAsync(request.Email, request.Code, cancellationToken))
            return new(null, false);

        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return new(userData, false);
    }

    /// <summary>
    /// 校验并一次性消费邮箱验证码。
    /// </summary>
    private async Task<bool> ConsumeEmailCodeAsync(string email, string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var cached = await identityCacheService.GetStringAsync($"{EmailCodeKeyPrefix}{email}", cancellationToken);
        if (string.IsNullOrEmpty(cached) || !string.Equals(cached, code, StringComparison.Ordinal))
            return false;
        await identityCacheService.RemoveAsync($"{EmailCodeKeyPrefix}{email}", cancellationToken);
        return true;
    }

    /// <summary>
    /// 原子记录密码登入失败：经仓储 ExecuteUpdate 递增失败计数（并发安全），达到阈值后锁定账号。
    /// </summary>
    private async Task RecordAccessFailAsync(Guid userGuid)
    {
        var count = await userRepository.IncrementAccessFaildCountAsync(userGuid);
        if (count >= UserAccessFail.MaxFailedAttempts)
        {
            var lockOutEnd = DateTimeOffset.UtcNow.Add(UserAccessFail.LockOutDuration);
            await userRepository.LockUserAsync(userGuid, lockOutEnd);
            logger.LogWarning("[{DateTime}] 用户 {UserGuid} 密码登入失败达阈值，已锁定至 {LockOutEnd}",
                DateTime.UtcNow, userGuid, lockOutEnd);
        }
    }

    /// <summary>
    /// 为已通过身份校验的用户构建 Claims 并生成 AccessToken + RefreshToken，登记多设备会话。
    /// </summary>
    private async ValueTask<TokenResult?> BuildTokenForUserAsync(User userData)
    {
        try
        {
            var roleName = await GetRoleNameAsync(userData.UserRoleGuid);
            var claims = BuildClaims(userData, roleName.ToHashSet());

            var config = optionsSnapshot.Value;
            var tokenData = await jwtTokenService.BuildTokenAsync(claims, config);

            // P3：多设备会话登记（每设备独立槽位，不再互相覆盖）
            await tokenSessionService.RegisterAsync(userData.UserGuid, tokenData,
                TimeSpan.FromSeconds(config.ExpireSeconds),
                TimeSpan.FromSeconds(config.RefreshTokenExpireSeconds));

            logger.LogInformation(
                "[{DateTime}] 用户 {UserEmail} 验证通过，" +
                "Token 已生成 (AccessToken 过期: {ExpireSeconds} 秒，RefreshToken 过期: {RefreshSeconds} 秒)",
                DateTime.UtcNow, userData.UserEmail, config.ExpireSeconds, config.RefreshTokenExpireSeconds);

            return tokenData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{DateTime}] 用户 {UserEmail} Token 生成失败", DateTime.UtcNow, userData.UserEmail);
            return null;
        }
    }

    /// <summary>
    /// 从用户实体构建 JWT Claims。
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

    /// <summary>
    /// 获取角色名称。
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

    /// <summary>按邮箱查询用户（仓储已 Include UserSafety / UserAccessFail 导航）。</summary>
    private ValueTask<User?> FindUserAsync(string email) => userRepository.FindOneByUserAsync(email);
}