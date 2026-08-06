namespace Identity.Domain.Entities.UserAggregate;

/// <summary>
/// 用户登录失败追踪与锁定策略
/// 
/// 策略: 连续失败达到 MaxFailedAttempts(5) 次后锁定 15 分钟；登录成功后自动清零。
/// 失败计数由 UserService 经仓储 ExecuteUpdate 原子递增（S-13），达到阈值时原子写入 LockOutEnd。
/// 锁定过期后下次尝试自动解除。
/// </summary>
public class UserAccessFail : Entity<int>
{
    /// <summary>最大连续失败次数（超过则锁定）</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>锁定持续时间</summary>
    public static readonly TimeSpan LockOutDuration = TimeSpan.FromMinutes(15);

    protected UserAccessFail()
    {
        UserAccessFailGuid = Guid.CreateVersion7();
    }

    /// <summary>
    /// 用户访问失败记录 ID
    /// </summary>
    public Guid UserAccessFailGuid { get; init; }

    /// <summary>
    /// 关联的用户 ID（外键）
    /// </summary>
    public Guid UserGuid { get; init; }

    /// <summary>
    /// 锁定结束时间（null = 未锁定；过期 = 自动解除）
    /// </summary>
    public DateTimeOffset? LockOutEnd { get; private set; }

    /// <summary>
    /// 连续失败次数
    /// </summary>
    public int AccessFaildCount { get; private set; }

    /// <summary>
    /// 当前是否处于有效锁定状态
    /// </summary>
    public bool IsLockedOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;

    /// <summary>
    /// 锁定是否已过期（可自动解除）
    /// </summary>
    private bool IsLockExpired => LockOutEnd.HasValue && LockOutEnd.Value <= DateTimeOffset.UtcNow;

    // ────────────── 工厂 ──────────────

    public static UserAccessFail CreateUserAccessFail(Guid userGuid)
    {
        if (userGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userGuid), "User cannot be null");

        return new UserAccessFail
        {
            UserAccessFailGuid = Guid.CreateVersion7(),
            UserGuid = userGuid,
            LockOutEnd = null,
            AccessFaildCount = 0
        };
    }

    // ────────────── 核心方法 ──────────────

    /// <summary>
    /// 检查是否允许登录尝试（锁定且未过期则拒绝）
    /// </summary>
    public bool CanLogin()
    {
        if (IsLockExpired)
            AutoUnlock();
        return !IsLockedOut;
    }

    /// <summary>
    /// 记录登录成功，清零失败计数与锁定状态
    /// </summary>
    public void RecordSuccess()
    {
        if (IsLockExpired)
            AutoUnlock();

        if (!IsLockedOut)
        {
            AccessFaildCount = 0;
            LockOutEnd = null;
        }
    }

    // ────────────── 内部 ──────────────

    private void AutoUnlock()
    {
        AccessFaildCount = 0;
        LockOutEnd = null;
    }
}
