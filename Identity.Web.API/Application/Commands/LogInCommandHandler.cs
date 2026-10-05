using Identity.Domain.ICache;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;

namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 统一登录命令处理器（登录/注册二合一）：
/// 1. 密码登入：锁定检查 → 密码校验（失败原子递增失败计数，达阈值锁定）→ 二次验证（开启用户校验并一次性消费邮箱验证码）；
/// 2. 验证码登入：校验并一次性消费验证码 → 查无用户则派发 <see cref="RegisterByEmailCommand"/> 自动注册 → 锁定检查；
/// 3. 公共尾部：构建 Claims（含权限集合 + 数据范围，供网关本地判定）→ 签发 AccessToken/RefreshToken → 登记多设备会话。
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
    IPermissionChecker permissionChecker,
    ILogger<LogInCommandHandler> logger)
    : IRequestHandler<LogInCommand, LogInCommandResult?>
{
    private const string EmailCodeKeyPrefix = "Login_";

    /// <summary>身份校验通过的中间产物（User 为 null 表示认证失败；FailureReason 仅在需前端引导时回传）</summary>
    private sealed record AuthenticatedUser(User? User, bool IsNewUser, LoginFailureReason? FailureReason = null);

    public async Task<LogInCommandResult?> Handler(LogInCommand request, CancellationToken cancellationToken)
    {
        var auth = request.IsPasswordLogin
            ? await LogInByPasswordAsync(request, cancellationToken)
            : await LogInByEmailCodeAsync(request, cancellationToken);

        // 失败时：仅「密码已通过、仅缺二次验证码」这一种可区分原因回传给 API 层（前端据此引导补码）；
        // 其余失败（邮箱不存在/密码错误/账号锁定/验证码错误）统一返回 null，对外同一文案，防止账号枚举。
        if (auth.User is null)
        {
            return auth.FailureReason is null
                ? null
                : new LogInCommandResult(null, false, Guid.Empty, FailureReason: auth.FailureReason);
        }

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
        if (!await ConsumeEmailCodeAsync(request.Email, request.Code, cancellationToken))
            return new(null, false);

        var userData = await FindUserAsync(request.Email);
        var isNewUser = false;
        if (userData is null)
        {
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

        // 二次验证（仅开启二次验证的用户需校验邮箱验证码；该开关默认开启）
        if (userData.UserSafety.IsTwoFactorEnabled &&
            !await ConsumeEmailCodeAsync(request.Email, request.Code, cancellationToken))
        {
            // 密码校验已通过、仅缺有效验证码。此前静默返回 401 难以定位，故补日志并回传可区分原因。
            logger.LogWarning(
                "[{DateTime}] 用户 {UserEmail} 已开启二次验证，密码登入未提供有效邮箱验证码（ProvidedCode={ProvidedCode}），拒绝登入",
                DateTime.UtcNow, userData.UserEmail, !string.IsNullOrWhiteSpace(request.Code));
            return new(null, false, LoginFailureReason.EmailCodeRequired);
        }

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
            var claims = await BuildClaimsAsync(userData, roleName.ToHashSet());

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
    /// 从用户实体构建 JWT Claims（含权限集合 + 数据范围 claim，网关据此本地判定，不再每请求回调 Identity）
    /// </summary>
    private async Task<List<Claim>> BuildClaimsAsync(User userData, HashSet<string> roleName)
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

        // 权限集合（角色直连 + 组继承，去重，逗号分隔；授权目录码自动覆盖子孙的判定在网关按前缀段匹配）
        // 空权限也写入空 claim：网关据此本地拒绝，避免无 claim 时回退远程校验
        var permissions = await permissionChecker.GetUserPermissionsAsync(userData.UserGuid);
        claims.Add(new Claim(PermissionClaimTypes.Permissions,
            string.Join(',', permissions.OrderBy(x => x, StringComparer.Ordinal))));

        // 数据范围（Root/Admin → All，其余 Own）
        var scope = await permissionChecker.GetUserDataScopeAsync(userData.UserGuid);
        claims.Add(new Claim(PermissionClaimTypes.DataScope, scope.ToClaimValue()));

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