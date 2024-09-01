namespace Identity.Domain.Entities;


/// <summary>
/// 扩展用户信息
/// </summary>
public class UserAppend
{
    
    public Uri? ThereCover { get; set; }
    
    public string? BriefingNote { get; private set; }
    
    public string? Title { get; private set; }
    
    public long Lever { get; private set; }
    
    public long Experience { get; private set; }
    
    private UserAppend(){}
    
    public UserAppend(Uri? thereCover, string? briefingNote, string? title)
    {
        ThereCover = thereCover;
        BriefingNote = briefingNote;
        Title = title;
        Lever = 0;
        Experience = 0;
    }

    private ValueTask ChangeByLeverValueTask()
    {
        Lever++;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 添加经验值
    /// </summary>
    /// <param name="experience"></param>
    /// <returns></returns>
    private ValueTask<UserAppend> AddByExperienceValueTask(long experience)
    {
        this.Experience += experience;
        return new ValueTask<UserAppend>(this);
    }

    /// <summary>
    /// 当经验到达时便升级等级
    /// </summary>
    /// <returns></returns>
    public ValueTask<UserAppend> ChangeByLeverUpValueTask()
    {
        var exp = Lever * 500;
        if (Experience >= exp)
        {
            ChangeByLeverValueTask();
        }
        return new ValueTask<UserAppend>(this);
    }
    
    
    
}