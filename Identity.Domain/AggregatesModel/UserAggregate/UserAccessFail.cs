namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserAccessFail : Entity
{


    protected UserAccessFail()
    {
        UserAccessFailGuid = Guid.CreateVersion7();
    }

    public Guid UserAccessFailGuid { get; init; }

    public Guid UserGuid { get; init; }

    public DateTimeOffset? LockOutEnd { get; private set; }

    public int AccessFaildCount { get; private set; }

    /// <summary>
    ///   锁定用户,如果LockOutEnd不为null且大于当前时间，则表示用户被锁定
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
                AccessFaildCount = 0,

            };
            return userAccessFail;
        }

        throw new ArgumentNullException(nameof(UserGuid), "User cannot be null");
    }


    public bool VerifyByAccessFaild(bool checkByPassword)
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
        else
        {
            AccessFaildCount = 0;
            LockOutEnd = null;
            return true; // 重置失败计数，允许访问
        }

        // return false; // 锁定用户
    }




    public void ResetFailAsync()
    {
        if (!IsLockOut)
            throw new InvalidOperationException("Cannot reset fails when not locked out.");
        LockOutEnd = null;
        AccessFaildCount = 0;
    }


    /// <summary>
    ///     重置
    /// </summary>
    /// <returns></returns>
    public bool CloseLockAsync()
    {
        if (!IsLockOut) return false;
        if (LockOutEnd >= DateTime.UtcNow) return true;
        ResetFailAsync();
        return false;
    }
}