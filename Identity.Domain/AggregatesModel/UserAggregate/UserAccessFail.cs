namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserAccessFail : Entity
{


    protected UserAccessFail()
    {
       
    }
      

    public Guid UserGuid { get; init; }

    public DateTimeOffset? LockOutEnd { get; private set; }

    public int AccessFaildCount { get; private set; }

    /// <summary>
    ///   锁定用户,如果LockOutEnd不为null且大于当前时间，则表示用户被锁定
    /// </summary>
    public bool IsLockOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;



    public static ValueTask<UserAccessFail> CreateByUserAccessFailAsync(Guid userGuid)
    {

        if (userGuid == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(userGuid), "UserGuid cannot be empty");
        }
        var userAccessFail = new UserAccessFail
        {             
            Id = Guid.CreateVersion7(),
            UserGuid=userGuid,
            LockOutEnd = null,
            AccessFaildCount = 0,

        };
        return new ValueTask<UserAccessFail>(userAccessFail);

    }


    public bool VerifyByAccessFaild()
    {

        AccessFaildCount++;
        if (AccessFaildCount > 5)
        {
            LockOutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            return false; // 锁定用户
        }
        return true; // 继续允许访问
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