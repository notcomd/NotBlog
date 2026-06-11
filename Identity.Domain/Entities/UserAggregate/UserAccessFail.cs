namespace Identity.Domain.Entities.UserAggregate;

/// <summary>
/// 用户访问失败
/// </summary>
public class UserAccessFail : Entity
{
    protected UserAccessFail()
    {
        UserAccessFailGuid = Guid.CreateVersion7();
    }

    /// <summary>
    /// 用户访问失败ID
    /// </summary>
    public Guid UserAccessFailGuid { get; init; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserGuid { get; init; }

    /// <summary>
    /// 锁定结束时间
    /// </summary>
    public DateTimeOffset? LockOutEnd { get; private set; }

    /// <summary>
    /// 访问失败次数
    /// </summary>
    public int AccessFaildCount { get; private set; }

    /// <summary>
    /// 锁定用户,如果LockOutEnd不为null且大于当前时间，则表示用户被锁定
    /// </summary>
    public bool IsLockOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;


    public static UserAccessFail CreateUserAccessFail(Guid userGuid)
    {
        if (userGuid != null)
        {
            var userAccessFail = new UserAccessFail
            {
                UserAccessFailGuid = Guid.CreateVersion7(),
                UserGuid = userGuid,
                LockOutEnd = null,
                AccessFaildCount = 0
            };
            return userAccessFail;
        }

        throw new ArgumentNullException(nameof(UserGuid), "User cannot be null");
    }

    /// <summary>
    /// 验证用户访问失败
    /// </summary>
    /// <param name="checkByPassword">是否检查密码</param>
    /// <returns>是否允许访问</returns>
    public bool VerifyByAccessFaild(bool checkByPassword)
    {
        while (AccessFaildCount <= 5)
        {
            if (!checkByPassword)
            {
                AccessFaildCount++;
                if (AccessFaildCount > 5)
                {
                    LockOutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                    return false; // 锁定用户
                }

                return true; // 继续允许访问
            }

            AccessFaildCount = 0;
            LockOutEnd = null;
            return true; // 重置失败计数，允许访问
        }

        return false; // 锁定用户
    }

    /// <summary>
    /// 重置用户访问失败
    /// </summary>
    public void ResetFailAsync()
    {
        if (!IsLockOut)
            throw new InvalidOperationException("Cannot reset fails when not locked out.");
        LockOutEnd = null;
        AccessFaildCount = 0;
    }

    /// <summary>
    /// 关闭用户访问失败锁定
    /// </summary>
    /// <returns>是否成功关闭访问失败锁定</returns>
    public bool CloseLockAsync()
    {
        if (!IsLockOut) return false;
        if (LockOutEnd >= DateTime.UtcNow) return true;
        ResetFailAsync();
        return true;
    }
}