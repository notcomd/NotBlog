using Identity.Domain.ICache;

namespace Identity.Infrastructure.Services;

/// <summary>
/// 用户读侧/安全信息服务（纯查询 + 二次验证开关更新）。
/// 登录/注册（含自动注册创建用户、密码失败计数、锁定）等增删改事务已迁移至
/// Web.API Application 层 CQRS 命令（LogInCommand / RegisterByEmailCommand），
/// 本服务不再持有任何创建/登录事务，遵循框架命令编排设计。
/// </summary>
public class UserService(
    ILogger<IUserRepository> loggerUser,
    IUserRepository userRepository,
    IIdentityCacheService identityCacheService)
    : IUserService
{
    private const string EmailCodeKeyPrefix = "Login_";

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
}