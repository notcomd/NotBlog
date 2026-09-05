namespace Message.Domain.Entities.Chat;

/// <summary>
/// 会话内单个成员的会话状态（未读 / 最后读取 / 置顶 / 免打扰）。
/// <para>
/// 是 <see cref="ChatSession.MemberStates"/> 的值对象。将原先散落在聚合根上的四个字段
/// （<c>_unreadCount / _lastReadTime / IsPinned / IsMuted</c>）收敛为按成员维度的一份状态，
/// 使私聊多端（SignalR 按"用户 + 会话"维度管理连接）下各成员的未读徽标、置顶、免打扰可独立表达。
/// </para>
/// </summary>
public sealed class ChatSessionMemberState
{
    /// <summary>成员（用户）ID。</summary>
    public Guid MemberId { get; init; }

    /// <summary>未读消息数。</summary>
    public int UnreadCount { get; private set; }

    /// <summary>最后读取时间（null 表示从未读取）。</summary>
    public DateTime? LastReadTime { get; private set; }

    /// <summary>是否置顶会话（仅作用于该成员）。</summary>
    public bool IsPinned { get; private set; }

    /// <summary>是否免打扰（仅作用于该成员）。</summary>
    public bool IsMuted { get; private set; }

    /// <summary>创建成员状态（初始未读为 0）。</summary>
    public ChatSessionMemberState(Guid memberId)
    {
        MemberId = memberId;
    }

    /// <summary>未读 +1（消息到达）。</summary>
    public void IncrementUnread()
    {
        UnreadCount++;
    }

    /// <summary>标记为已读：未读清零并记录读取时间。</summary>
    /// <param name="at">读取时刻。</param>
    public void MarkAsRead(DateTime at)
    {
        UnreadCount = 0;
        LastReadTime = at;
    }

    /// <summary>设置置顶状态。</summary>
    public void SetPinned(bool pinned)
    {
        IsPinned = pinned;
    }

    /// <summary>设置免打扰状态。</summary>
    public void SetMuted(bool muted)
    {
        IsMuted = muted;
    }

    /// <summary>
    /// 从持久化数据重建成员状态（Mongo 投影读取路径）。
    /// <para>重构已校验、已持久化的数据，不做额外默认赋值。</para>
    /// </summary>
    public static ChatSessionMemberState Rebuild(
        Guid memberId, int unreadCount, DateTime? lastReadTime, bool isPinned, bool isMuted)
    {
        var state = new ChatSessionMemberState(memberId)
        {
            UnreadCount = unreadCount,
            LastReadTime = lastReadTime,
            IsPinned = isPinned,
            IsMuted = isMuted
        };
        return state;
    }
}