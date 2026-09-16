
namespace Message.Domain.Entities.Chat;

/// <summary>
/// 会话
/// </summary>
public class ChatSession : Entity<Guid>, IAggregateRoot
{
    private readonly Dictionary<Guid, ChatSessionMemberState> _memberStates = new();

    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// 会话类型
    /// </summary>  
    public SessionType SessionType { get; init; }

    /// <summary>
    /// 群聊ID
    /// </summary>
    public Guid? GroupId { get; init; }

    /// <summary>
    /// 关联社区ID（Channel 社区聊天，Discord 式服务器频道）
    /// </summary>
    public Guid? CircleId { get; init; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public Guid CreatorId { get; init; }

    /// <summary>
    /// 参与者ID列表
    /// </summary>
    public List<Guid> Participants { get; private set; }

    /// <summary>
    /// 最后一条消息ID
    /// </summary>
    public Guid? LastMessageId { get; private set; }

    /// <summary>
    /// 最后一条消息内容
    /// </summary>
    public string? LastMessageContent { get; private set; }

    /// <summary>
    /// 最后一条消息时间
    /// </summary>
    /// </summary>
    public DateTime? LastMessageTime { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; init; }

    /// <summary>
    /// 解散时间
    /// </summary>
    public DateTime? DismissedTime { get; private set; }

    /// <summary>
    /// 是否已解散
    /// </summary>
    public bool IsDismissed { get; private set; }

    /// <summary>
    /// 各成员的会话状态（未读 / 最后读取 / 置顶 / 免打扰），key = 用户ID。
    /// 置顶/免打扰为成员维度而非会话维度，便于 SignalR 多端会话管理。
    /// </summary>
    public IReadOnlyDictionary<Guid, ChatSessionMemberState> MemberStates => _memberStates;


    /// <summary>
    ///  
    /// </summary>
    /// <param name="sessionType"></param>
    /// <param name="creatorId"></param>
    /// <param name="participants"></param>
    public ChatSession(SessionType sessionType, Guid creatorId, IEnumerable<Guid>? participants = null)
    {
        SessionId = Guid.NewGuid();
        SessionType = sessionType;
        CreatorId = creatorId;
        Participants = participants?.ToList() ?? new List<Guid> { creatorId };
        CreatedTime = DateTime.UtcNow;
        IsDismissed = false;
        foreach (var participantId in Participants)
            _memberStates[participantId] = new ChatSessionMemberState(participantId);
    }

    private ChatSession()
    {
        SessionId = Guid.CreateVersion7();
        Participants = new List<Guid>();
        CreatedTime = DateTime.UtcNow;
        IsDismissed = false;
    }


    /// <summary>
    /// 创建私聊会话
    /// </summary>
    /// <param name="userId1">用户ID1</param>
    /// <param name="userId2">用户ID2</param>
    /// <returns>私聊会话</returns>
    public static ChatSession CreatePrivateSession(Guid userId1, Guid userId2)
    {
        var participants = new HashSet<Guid> { userId1, userId2 };
        var session = new ChatSession(SessionType.Private, userId1, participants);
        session.AddDomainEvent(new SessionCreatedEvent(session.SessionId, participants, SessionType.Private));
        return session;
    }

    /// <summary>
    /// 创建群聊会话
    /// </summary>
    /// <param name="groupId">群聊ID</param>
    /// <param name="creatorId">创建者ID</param>
    /// <param name="initialMembers">初始成员ID列表</param>
    /// <returns>群聊会话</returns>
    public static ChatSession CreateGroupSession(Guid groupId, Guid creatorId, HashSet<Guid> initialMembers)
    {
        var participants = new HashSet<Guid>(initialMembers) { creatorId };
        var session = new ChatSession(SessionType.Group, creatorId, participants)
        {
            GroupId = groupId
        };
        session.AddDomainEvent(new SessionCreatedEvent(session.SessionId, participants, SessionType.Group));
        return session;
    }

    /// <summary>
    /// 创建社区群组会话（SessionType.Group 承载社区聊天）。
    /// </summary>
    /// <param name="groupId">社区群组ID</param>
    /// <param name="circleId">社区ID</param>
    /// <param name="creatorId">创建者ID（圈主，同时是群主）</param>
    /// <param name="initialMembers">初始成员ID列表（创建时仅圈主，成员加入经事件同步）</param>
    /// <returns>社区群组会话</returns>
    public static ChatSession CreateCommunityGroupSession(Guid groupId, Guid circleId, Guid creatorId, HashSet<Guid> initialMembers)
    {
        var participants = new HashSet<Guid>(initialMembers) { creatorId };
        var session = new ChatSession(SessionType.Group, creatorId, participants)
        {
            GroupId = groupId,
            CircleId = circleId
        };
        session.AddDomainEvent(new SessionCreatedEvent(session.SessionId, participants, SessionType.Group));
        return session;
    }

    /// <summary>
    /// 添加参与者
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void AddParticipant(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("会话已解散");
        if (Participants.Contains(userId))
            throw new InvalidOperationException("用户已在会话中");

        Participants.Add(userId);
        _memberStates[userId] = new ChatSessionMemberState(userId);
    }

    /// <summary>
    /// 移除参与者
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void RemoveParticipant(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("会话已解散");
        if (!Participants.Contains(userId))
            throw new InvalidOperationException("用户不在会话中");
        if (Participants.Count <= 2 && SessionType == SessionType.Private)
            throw new InvalidOperationException("私聊会话至少需要两个参与者");

        Participants.Remove(userId);
        _memberStates.Remove(userId);
    }

    /// <summary>
    /// 更新最后一条消息
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="content">消息内容</param>
    public void UpdateLastMessage(Guid messageId, string? content)
    {
        if (IsDismissed)
            throw new InvalidOperationException("会话已解散");

        LastMessageId = messageId;
        LastMessageContent = content;
        LastMessageTime = DateTime.UtcNow;

        foreach (var participantId in Participants)
        {
            if (!_memberStates.TryGetValue(participantId, out var state))
            {
                state = new ChatSessionMemberState(participantId);
                _memberStates[participantId] = state;
            }
            state.IncrementUnread();
        }
    }

    /// <summary>
    /// 标记为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void MarkAsRead(Guid userId)
    {
        if (!Participants.Contains(userId))
            throw new InvalidOperationException("用户不在会话中");

        StateOf(userId).MarkAsRead(DateTime.UtcNow);
    }

    /// <summary>
    /// 获取未读消息数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>未读消息数量</returns>
    public int GetUnreadCount(Guid userId)
    {
        return MemberStates.TryGetValue(userId, out var state) ? state.UnreadCount : 0;
    }

    /// <summary>
    /// 设置某成员的会话置顶状态（成员维度）。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pinned">是否置顶</param>
    public void SetPinned(Guid userId, bool pinned)
    {
        if (!Participants.Contains(userId))
            throw new InvalidOperationException("用户不在会话中");
        StateOf(userId).SetPinned(pinned);
    }

    /// <summary>
    /// 设置某成员的会话免打扰状态（成员维度）。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="muted">是否免打扰</param>
    public void SetMuted(Guid userId, bool muted)
    {
        if (!Participants.Contains(userId))
            throw new InvalidOperationException("用户不在会话中");
        StateOf(userId).SetMuted(muted);
    }

    /// <summary>获取（必要时创建）成员的会话状态。</summary>
    private ChatSessionMemberState StateOf(Guid userId)
    {
        if (_memberStates.TryGetValue(userId, out var state))
            return state;
        state = new ChatSessionMemberState(userId);
        _memberStates[userId] = state;
        return state;
    }

    /// <summary>
    /// 解散会话
    /// </summary>
    public void Dismiss()
    {
        if (IsDismissed)
            throw new InvalidOperationException("会话已解散");

        IsDismissed = true;
        DismissedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查用户是否为会话参与者
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否为会话参与者</returns>
    public bool IsParticipant(Guid userId)
    {
        return Participants.Contains(userId);
    }

    /// <summary>
    /// 从持久化数据重建会话聚合根（Mongo 投影读取路径）。
    /// <para>重构已校验、已持久化的实体，不重复触发领域工厂校验、不重新产生领域事件。</para>
    /// </summary>
    public static ChatSession Rebuild(
        Guid sessionId, SessionType sessionType, Guid? groupId, Guid? circleId, Guid creatorId,
        List<Guid> participants, IReadOnlyDictionary<Guid, ChatSessionMemberState> memberStates,
        Guid? lastMessageId, string? lastMessageContent, DateTime? lastMessageTime,
        DateTime createdTime, DateTime? dismissedTime, bool isDismissed)
    {
        var session = new ChatSession
        {
            SessionId = sessionId,
            SessionType = sessionType,
            GroupId = groupId,
            CircleId = circleId,
            CreatorId = creatorId,
            CreatedTime = createdTime
        };
        session.Participants = participants;
        session._memberStates.Clear();
        foreach (var (userId, state) in memberStates)
            session._memberStates[userId] = state;
        session.LastMessageId = lastMessageId;
        session.LastMessageContent = lastMessageContent;
        session.LastMessageTime = lastMessageTime;
        session.DismissedTime = dismissedTime;
        session.IsDismissed = isDismissed;
        return session;
    }
}