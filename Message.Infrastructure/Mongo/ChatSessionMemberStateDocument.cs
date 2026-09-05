namespace Message.Infrastructure.Mongo;

/// <summary>
/// 会话内单个成员的会话状态的 MongoDB 文档子片段（嵌套于 <see cref="ChatSessionDocument.MemberStates"/>）。
/// <para>纯 POCO，对应领域值对象 <c>ChatSessionMemberState</c>，读写由 <see cref="ChatSessionMapper"/> 互转。</para>
/// </summary>
public sealed class ChatSessionMemberStateDocument
{
    /// <summary>成员（用户）ID。</summary>
    public Guid MemberId { get; set; }

    /// <summary>未读消息数。</summary>
    public int UnreadCount { get; set; }

    /// <summary>最后读取时间。</summary>
    public DateTime? LastReadTime { get; set; }

    /// <summary>是否置顶（成员维度）。</summary>
    public bool IsPinned { get; set; }

    /// <summary>是否免打扰（成员维度）。</summary>
    public bool IsMuted { get; set; }
}