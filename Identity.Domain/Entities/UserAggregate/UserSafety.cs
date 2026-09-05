namespace Identity.Domain.Entities.UserAggregate;

public class UserSafety : Entity<int>
{
    private UserSafety(Guid userGuid)
    {
        UserSafetyGuid = Guid.CreateVersion7();
        UserGuid = userGuid;
        BlackOrWhite = BlackOrWhite.AuthorityWhite;
        UserStatus = UserStatus.Normal;
        IsTwoFactorEnabled = true;
    }

    public UserSafety(Guid userGuid, string securityStamp, string passwordSalt) : this(userGuid)
    {
        if (securityStamp == null)
            throw new ArgumentNullException(nameof(securityStamp));
        SecurityStamp = securityStamp;
        PasswordSalt = passwordSalt;
    }

    /// <summary>
    /// 用户安全ID
    /// </summary>
    public Guid UserSafetyGuid { get; init; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserGuid { get; init; }

    /// <summary>
    /// 安全戳
    /// </summary>
    public string? SecurityStamp { get; private set; }

    /// <summary>
    /// 密码盐
    /// </summary>
    public string? PasswordSalt { get; private set; }

    /// <summary>
    /// 黑名单
    /// </summary>
    public BlackOrWhite BlackOrWhite { get; private set; }

    /// <summary>
    /// 用户状态
    /// </summary>
    public UserStatus UserStatus { get; private set; }

    /// <summary>
    /// 锁定结束时间
    /// </summary>
    public DateTimeOffset? LockOutEnd { get; private set; }

    /// <summary>
    /// 二次验证（两步验证）开关：默认开启。
    /// 开启时使用密码登入需额外输入邮箱验证码；关闭后密码登入免验证码。
    /// </summary>
    public bool IsTwoFactorEnabled { get; private set; }

    public bool IsLockedOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;

    public static UserSafety CreateByUserSafety(Guid UserGuid, string? securityStamp, string? passwordSalt,
        BlackOrWhite blackOrWhite = BlackOrWhite.AuthorityWhite, UserStatus userStatus = UserStatus.Normal,
        bool isTwoFactorEnabled = true)
    {
        if (string.IsNullOrEmpty(passwordSalt) && string.IsNullOrEmpty(securityStamp))
            throw new ArgumentException("At least one of passwordSalt or securityStamp must be provided",
                nameof(passwordSalt));

        var userSafety = new UserSafety(UserGuid)
        {
            UserSafetyGuid = Guid.CreateVersion7(),
            SecurityStamp = securityStamp,
            PasswordSalt = passwordSalt,
            BlackOrWhite = blackOrWhite,
            UserStatus = userStatus,
            IsTwoFactorEnabled = isTwoFactorEnabled
        };

        return userSafety;
    }

    /// <summary>
    /// 修改用户锁定状态
    /// </summary>
    /// <param name="lockOutEnd"></param>
    /// <exception cref="ArgumentException"></exception>
    public void ChangeByLockOutEnd(DateTimeOffset? lockOutEnd)
    {
        if (IsLockedOut)
            throw new ArgumentException("LockOutEnd cannot be in the past", nameof(lockOutEnd));
        LockOutEnd = lockOutEnd;
    }

    /// <summary>
    /// 重置用户密码盐
    /// </summary>
    /// <param name="newPasswordSalt"></param>
    /// <exception cref="ArgumentException"></exception>
    public void ResetByPasswordSalt(string newPasswordSalt)
    {
        if (string.IsNullOrWhiteSpace(newPasswordSalt))
            throw new ArgumentException("Password salt cannot be null or empty", nameof(newPasswordSalt));
        PasswordSalt = newPasswordSalt;
    }

    /// <summary>
    /// 重置用户安全戳
    /// </summary>
    /// <param name="newSecurityStamp"></param>
    /// <exception cref="ArgumentException"></exception>
    public void ResetBySecurityStamp(string newSecurityStamp)
    {
        if (string.IsNullOrWhiteSpace(newSecurityStamp))
            throw new ArgumentException("Security stamp cannot be null or empty", nameof(newSecurityStamp));
        SecurityStamp = newSecurityStamp;
    }

    /// <summary>
    /// 修改二次验证（两步验证）开启状态。
    /// </summary>
    /// <param name="enabled">是否开启：开启后使用密码登入需额外输入邮箱验证码；关闭后免验证码</param>
    public void ChangeByTwoFactorEnabled(bool enabled)
    {
        IsTwoFactorEnabled = enabled;
    }
}