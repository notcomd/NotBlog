namespace Identity.Domain.Entities;

public class UserAccessFail
{
    public Guid UserAccessFailGuid { get; init; }
    
    public object? Message { get; set; }
    public User User { get; init; }
    
    private bool LockOut;
    public DateTime? LockOutEnd { get; private set; }
    public int AccessFaildCount { get; private set; }
    
    private UserAccessFail(){}

    
    public UserAccessFail(User user)
    {
        UserAccessFailGuid = new Guid();
        User = user;
    }

    /// <summary>
    /// 密码验证失败添加次数
    /// </summary>
    /// <returns></returns>
    public ValueTask FailAsync()
    {
        AccessFaildCount++;
        if (AccessFaildCount > 5)
        {
            this.LockOut = true;
            this.LockOutEnd = DateTime.UtcNow.AddMinutes(5);
        }
        return new ValueTask();
    }

    /// <summary>
    /// 重置
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
    /// 重置
    /// </summary>
    /// <returns></returns>
    public ValueTask<bool> CloseLockAsync()
    {
        if (!LockOut) return new ValueTask<bool>(false);
        if (LockOutEnd >= DateTime.UtcNow)
        {
            return new ValueTask<bool>(true);
        }
        ResetFailAsync();
        return new ValueTask<bool>(false);
    }
    
}