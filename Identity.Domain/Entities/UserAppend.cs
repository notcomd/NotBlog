namespace Identity.Domain.Entities;


/// <summary>
/// 扩展用户信息
/// </summary>
public class UserAppend
{
    
    public Uri? ThereCover { get; set; }
    
    public string? BriefingNote { get; private set; }
    
    public string? Title { get; set; }
    
    private UserAppend(){}
    
    public UserAppend(Uri? thereCover, string? briefingNote, string? title)
    {
        ThereCover = thereCover;
        BriefingNote = briefingNote;
        Title = title;
    }
}