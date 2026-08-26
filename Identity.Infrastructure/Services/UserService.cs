using Identity.Domain.Events;
using Identity.Domain.ICache;
using Identity.Domain.Dto;
using Notcomd.Token.JWT.Security;

namespace Identity.Infrastructure.Services;

public class UserService(
    IOptionsSnapshot<JwtOptions> optionsSnapshot,
    ILogger<IUserRepository> loggerUser,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IJwtTokenService jwtTokenServer,
    ITokenSessionService tokenSessionService,
    IIdentityCacheService identityCacheService,
    IMailQueue mailQueue)
    : IUserService
{
    private const string EmailCodeKeyPrefix = "Login_";

    // ── 统一登录/注册（邮箱验证码） ──

    /// <summary>
    /// 邮箱验证码登录（统一登录/注册）：
    /// 1. 校验并一次性消费邮箱验证码；
    /// 2. 邮箱已存在 → 直接登录；
    /// 3. 邮箱不存在 → 自动创建账号并生成初始密码发送到该邮箱，然后立即登录。
    /// </summary>
    public async Task<EmailLoginResult?> LogInByEmailCodeAsync(
        [EmailAddress(ErrorMessage = "无效邮件地址")] string email, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        // 校验验证码（放入缓存即视为已下发），校验通过后一次性消费
        var cached = await identityCacheService.GetStringAsync($"{EmailCodeKeyPrefix}{email}", default);
        if (string.IsNullOrEmpty(cached) || !string.Equals(cached, code, StringComparison.Ordinal))
            return null;
        await identityCacheService.RemoveAsync($"{EmailCodeKeyPrefix}{email}", default);

        var userData = await userRepository.FindOneByUserAsync(email);
        var isNewUser = false;
        if (userData is null)
        {
            // 陌生邮箱 → 自动注册并下发初始密码
            userData = await CreateUserAndSendInitialPasswordAsync(email);
            if (userData is null)
                return null;
            isNewUser = true;
        }

        // ── 第一步: 检查是否被锁定 ──
        if (!userData.UserAccessFail.CanLogin())
        {
            loggerUser.LogWarning("[{DateTime}] 用户 {UserEmail} 账号已锁定，拒绝登录", DateTime.UtcNow, userData.UserEmail);
            return null;
        }

        await userRepository.UnitOfWork.SaveChangesAsync();

        var token = await BuildTokenForUserAsync(userData);
        return token is null ? null : new EmailLoginResult(token, isNewUser, userData.UserGuid);
    }

    /// <summary>
    /// 密码登入（邮箱 + 密码 + 二次验证）：
    /// 1. 邮箱不存在或锁定时拒绝（密码登入不做自动注册）；
    /// 2. 校验密码，失败则原子递增失败计数并达到阈值时锁定；
    /// 3. 开启二次验证的用户再校验邮箱验证码（校验通过后一次性消费），关闭二次验证的用户免验证码。
    /// </summary>
    public async Task<EmailLoginResult?> LogInByPasswordAsync(
        [EmailAddress(ErrorMessage = "无效邮件地址")] string email, string password, string? code)
    {
        if (string.IsNullOrWhiteSpace(password))
            return null;

        var userData = await userRepository.FindOneByUserAsync(email);
        if (userData is null)
            return null; // 密码登入不自动注册，杜绝邮箱枚举

        // ── 第一步: 检查是否被锁定 ──
        if (!userData.UserAccessFail.CanLogin())
        {
            loggerUser.LogWarning("[{DateTime}] 用户 {UserEmail} 账号已锁定，拒绝密码登入", DateTime.UtcNow, userData.UserEmail);
            return null;
        }

        // ── 第二步: 校验密码 ──
        if (!await userData.VerifyByPasswordAsync(password))
        {
            await RecordAccessFailAsync(userData.UserGuid);
            return null;
        }

        // ── 第三步: 二次验证（仅开启二次验证的用户需校验邮箱验证码）──
        if (userData.UserSafety.IsTwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var cached = await identityCacheService.GetStringAsync($"{EmailCodeKeyPrefix}{email}", default);
            if (string.IsNullOrEmpty(cached) || !string.Equals(cached, code, StringComparison.Ordinal))
                return null;
            await identityCacheService.RemoveAsync($"{EmailCodeKeyPrefix}{email}", default);
        }

        await userRepository.UnitOfWork.SaveChangesAsync();

        var token = await BuildTokenForUserAsync(userData);
        return token is null ? null : new EmailLoginResult(token, false, userData.UserGuid);
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
            loggerUser.LogWarning("[{DateTime}] 用户 {UserGuid} 密码登入失败达阈值，已锁定至 {LockOutEnd}",
                DateTime.UtcNow, userGuid, lockOutEnd);
        }
    }

    /// <summary>
    /// 自动注册邮箱账号并下发初始密码。
    /// 密码由 CSPRNG 生成（含大写、小写、数字、特殊字符），初始密码经邮件后台队列发送到该邮箱。
    /// 并发同邮箱自动注册由 UserEmail 唯一索引兜底（TOCTOU 竞态由 DB 约束终结）。
    /// </summary>
    private async Task<User?> CreateUserAndSendInitialPasswordAsync(string email)
    {
        var userRole = await userRoleRepository.FindByUserRoleAsync("User");
        if (userRole is null)
        {
            loggerUser.LogError("[{DateTime}] 默认角色 'User' 未配置，无法自动注册: {Email}", DateTime.UtcNow, email);
            throw new InvalidOperationException("默认角色 'User' 未在数据库中配置。");
        }

        var initialPassword = JwtRandom.GenerateComplexPassword();
        var user = await User.CreateByEmailUser(
            userRole.RoleGuid, email, initialPassword, null, null);
        await userRepository.AddOneByUserAsync(user);

        try
        {
            await userRepository.UnitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 并发登录同一陌生邮箱同时自动注册：唯一索引兜底
            loggerUser.LogWarning("[{DateTime}] 并发自动注册冲突，邮箱已存在: {Email}", DateTime.UtcNow, email);
            return null;
        }

        // P6：初始密码经邮件后台队列发送，不占用数据库事务做 SMTP 外部 IO
        mailQueue.Enqueue(email, "账号开通通知",
            $"您已通过邮箱验证码成功创建账号。您的初始密码为：{initialPassword}，请登录后尽快修改密码。");

        loggerUser.LogInformation("[{DateTime}] 邮箱自动注册成功并已下发初始密码: {Email}", DateTime.UtcNow, email);
        return user;
    }

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

    // ── 用户安全信息更新 ──

    /// <summary>
    /// 获取当前登录用户的二次验证开关状态。
    /// </summary>
    public async Task<bool?> GetUserSafetyAsync(Guid userGuid)
    {
        var user = await userRepository.FindOneByUserAsync(userGuid);
        return user is null ? null : user.UserSafety.IsTwoFactorEnabled;
    }

    /// <summary>
    /// 更新当前登录用户的安全信息（当前仅支持二次验证开关）。
    /// 关闭二次验证（置 false）为降级操作，必须通过密码或邮箱验证码二次确认，防止账户被接管。
    /// </summary>
    public async Task<bool> UpdateUserSafetyAsync(Guid userGuid, bool? isTwoFactorEnabled,
        string? password, string? code)
    {
        if (isTwoFactorEnabled is null)
            return false; // 无可更新内容

        var user = await userRepository.FindOneByUserAsync(userGuid);
        if (user is null)
        {
            loggerUser.LogWarning("[{DateTime}] 未找到用户 {UserGuid}，无法更新安全信息", DateTime.UtcNow, userGuid);
            return false;
        }

        var target = isTwoFactorEnabled.Value;

        // 关闭为降级操作：需密码或邮箱验证码之二选一进行二次确认
        if (!target && !await ConfirmIdentityAsync(user, password, code))
            return false;

        user.UserSafety.ChangeByTwoFactorEnabled(target);
        await userRepository.UnitOfWork.SaveChangesAsync();

        loggerUser.LogInformation(
            "[{DateTime}] 用户 {UserGuid} 已更新安全信息，IsTwoFactorEnabled = {Enabled}",
            DateTime.UtcNow, userGuid, target);
        return true;
    }

    /// <summary>
    /// 安全信息降级操作的身份二次确认：密码或邮箱验证码二选一通过即成功。
    /// </summary>
    private async Task<bool> ConfirmIdentityAsync(User user, string? password, string? code)
    {
        var hasPassword = !string.IsNullOrWhiteSpace(password);
        var hasCode = !string.IsNullOrWhiteSpace(code);
        if (!hasPassword && !hasCode)
            return false;

        if (hasPassword)
            return await user.VerifyByPasswordAsync(password!);

        return await ConsumeEmailCodeAsync(user.UserEmail, code!);
    }

    /// <summary>
    /// 校验并一次性消费邮箱验证码。
    /// </summary>
    private async Task<bool> ConsumeEmailCodeAsync(string email, string code)
    {
        var cached = await identityCacheService.GetStringAsync($"{EmailCodeKeyPrefix}{email}", default);
        if (string.IsNullOrEmpty(cached) || !string.Equals(cached, code, StringComparison.Ordinal))
            return false;
        await identityCacheService.RemoveAsync($"{EmailCodeKeyPrefix}{email}", default);
        return true;
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
            var tokenData = await jwtTokenServer.BuildTokenAsync(claims, config);

            // P3：多设备会话登记（每设备独立槽位，不再互相覆盖）
            await tokenSessionService.RegisterAsync(userData.UserGuid, tokenData,
                TimeSpan.FromSeconds(config.ExpireSeconds),
                TimeSpan.FromSeconds(config.RefreshTokenExpireSeconds));

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

