namespace Message.Domain.Options;

/// <summary>
/// 圈子邀请码生成配置项（字符集、长度、尝试次数与配额规则）。
/// </summary>
public class GenerateCircleInvitationOption
{
    /// <summary>
    /// 邀请码字符集（不含易混淆字符：0、O、1、I、L）
    /// </summary>
    public  string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    
    /// <summary>
    /// 邀请码长度（默认 6 位，支持 4~12 位）
    /// </summary>
    public  int CodeLength = 6;

    /// <summary>
    /// 最大尝试次数（默认 10 次）
    /// </summary>
    public  int MaxCodeAttempts = 10;

    /// <summary>
    /// 配额窗口（默认 7 天）
    /// </summary>
    public   TimeSpan QuotaWindow = TimeSpan.FromDays(7);
    
    /// <summary>
    /// 管理员每周邀请码配额（默认 2 个）
    /// </summary>
    public  int AdminWeeklyCodeQuota = 2;

    /// <summary>
    /// 普通成员每周邀请码配额（默认 1 个）
    /// </summary>
    public  int MemberWeeklyCodeQuota = 1;
}