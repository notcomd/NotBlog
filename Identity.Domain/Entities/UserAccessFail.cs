using System.ComponentModel.DataAnnotations;

namespace Identity.Domain.Entities;

public class UserAccessFail
{
    private bool LockOut;

    private UserAccessFail()
    {
    }


    public UserAccessFail(User user)
    {
        UserAccessFailGuid = new Guid();
        User = user;
        UserGuid = user.UserGuid;
    }

    [Key] public Guid UserAccessFailGuid { get; init; }

    public User User { get; init; }

    
    public Guid UserGuid { get; init; }
    public DateTime? LockOutEnd { get; private set; }
    public int AccessFaildCount { get; private set; }

    /// <summary>
    ///     密码验证失败添加次数
    /// </summary>
    /// <returns></returns>
    public ValueTask FailAsync()
    {
        AccessFaildCount++;
        if (AccessFaildCount > 5)
        {
            LockOut = true;
            LockOutEnd = DateTime.UtcNow.AddMinutes(5);
        }

        return new ValueTask();
    }

    /// <summary>
    ///     重置
    /// </summary>
    /// <returns></returns>
    private ValueTask ResetFailAsync()
    {
        LockOut = false;
        LockOutEnd = null;
        AccessFaildCount = 0;
        return new ValueTask();
    }


    /// <summary>
    ///     重置
    /// </summary>
    /// <returns></returns>
    public ValueTask<bool> CloseLockAsync()
    {
        if (!LockOut) return new ValueTask<bool>(false);
        if (LockOutEnd >= DateTime.UtcNow) return new ValueTask<bool>(true);
        ResetFailAsync();
        return new ValueTask<bool>(false);
    }
}