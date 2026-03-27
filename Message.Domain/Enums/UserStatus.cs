namespace Message.Domain.Enums;

public enum UserStatus
{
    /// <summary>
    /// 在线状态
    /// </summary>
    Online,

    /// <summary>
    /// 离线状态
    /// </summary>
    Offline,

    /// <summary>
    /// 离开状态
    /// </summary>
    Away,

    /// <summary>
    /// 繁忙状态
    /// </summary>  
    Busy,

    /// <summary>
    /// 不可见状态
    /// </summary>
    Invisible
}