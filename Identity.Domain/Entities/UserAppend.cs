using System.ComponentModel.DataAnnotations;

namespace Identity.Domain.Entities;


/// <summary>
/// 扩展用户信息
/// </summary>
public class UserAppend
{
    
    public Uri? ThereCover { get; set; }
    
    [StringLength(maximumLength:100,ErrorMessage = "string length is long！")]
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

    public ValueTask<UserAppend> ChangeByCoverValueTask(Uri cover)
    {
        this.ThereCover = cover;
        return new ValueTask<UserAppend>(this);
    }

    public ValueTask<UserAppend> ChangeByTitleValueTask(string title)
    {
        this.Title = title;
        return new ValueTask<UserAppend>(this);
    }

    public ValueTask<UserAppend> ChangeByBriefingNoteValueTask(string briefingNote)
    {
        this.BriefingNote = briefingNote;
        return new ValueTask<UserAppend>(this);
    }
    
}