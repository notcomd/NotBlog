namespace Message.Domain.Enums;

/// <summary>邀请类型</summary>
public enum CircleInvitationType
{
    /// <summary>邀请码（输入 6 位码加入）</summary>
    Code,
    /// <summary>邀请链接（token 免输入加入）</summary>
    Link,
    /// <summary>按用户直邀（对方确认后加入）</summary>
    Direct
}
