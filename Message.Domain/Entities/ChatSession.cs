using Message.Domain.Enums;
using Message.Domain.Events;
using Message.Domain.SeedWork;

namespace Message.Domain.Entities;

/// <summary>
/// 会话
/// </summary>
public class ChatSession : Entity, IAggregateRoot
{
    private readonly Dictionary<Guid, DateTime> _lastReadTime = new();

    private readonly Dictionary<Guid, int> _unreadCount = new();

    public ChatSession(SessionType sessionType, Guid creatorId, HashSet<Guid>? participants = null,
        string? sessionName = null)
    {
        SessionId = Guid.NewGuid();
        SessionType = sessionType;
        CreatorId = creatorId;
        Participants = participants ?? new HashSet<Guid> { creatorId };
        SessionName = sessionName;
        CreatedTime = DateTime.UtcNow;
        IsDismissed = false;
        IsPinned = false;
        IsMuted = false;
    }

    private ChatSession()
    {
        SessionId = Guid.NewGuid();
        Participants = new HashSet<Guid>();
        CreatedTime = DateTime.UtcNow;
        IsDismissed = false;
    }

    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// 会话类型
    /// </summary>  
    public SessionType SessionType { get; init; }

    /// <summary>
    /// 会话名称
    /// </summary>
    public string? SessionName { get; private set; }

    /// <summary>
    /// 群聊ID
    /// </summary>
    public Guid? GroupId { get; init; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public Guid CreatorId { get; init; }

    /// <summary>
    /// 参与者ID列表
    /// </summary>
    public HashSet<Guid> Participants { get; private set; }

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
    /// 是否已置顶
    /// </summary>
    public bool IsPinned { get; private set; }

    /// <summary>
    /// 是否已禁言
    /// </summary>
    public bool IsMuted { get; private set; }

    /// <summary>
    /// 未读消息数量
    /// </summary>
    public IReadOnlyDictionary<Guid, int> UnreadCount => _unreadCount;

    /// <summary>
    /// 最后读取时间
    /// </summary>
    public IReadOnlyDictionary<Guid, DateTime> LastReadTime => _lastReadTime;

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
    /// <param name="groupName">群聊名称</param>
    /// <param name="initialMembers">初始成员ID列表</param>
    /// <returns>群聊会话</returns>
    public static ChatSession CreateGroupSession(Guid groupId, Guid creatorId, string groupName,
        HashSet<Guid> initialMembers)
    {
        var participants = new HashSet<Guid>(initialMembers) { creatorId };
        var session = new ChatSession(SessionType.Group, creatorId, participants, groupName)
        {
            GroupId = groupId
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
        _unreadCount[userId] = 0;
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
        _unreadCount.Remove(userId);
        _lastReadTime.Remove(userId);
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
            if (!_unreadCount.ContainsKey(participantId))
                _unreadCount[participantId] = 0;
            _unreadCount[participantId]++;
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

        if (_unreadCount.ContainsKey(userId) && _unreadCount[userId] > 0)
            _unreadCount[userId] = 0;

        _lastReadTime[userId] = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取未读消息数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>未读消息数量</returns>
    public int GetUnreadCount(Guid userId)
    {
        return _unreadCount.TryGetValue(userId, out var count) ? count : 0;
    }

    /// <summary>
    /// 置顶会话
    /// </summary>
    public void Pin()
    {
        IsPinned = true;
    }

    /// <summary>
    /// 取消置顶会话
    /// </summary>
    public void Unpin()
    {
        IsPinned = false;
    }

    /// <summary>
    /// 禁言会话
    /// </summary>
    /// <param name="duration">禁言持续时间</param>
    public void Mute()
    {
        IsMuted = true;
    }

    /// <summary>
    /// 取消禁言会话
    /// </summary>
    public void Unmute()
    {
        IsMuted = false;
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
    /// 更新会话名称
    /// </summary>
    /// <param name="name">会话名称</param>
    public void UpdateSessionName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("会话名称不能为空", nameof(name));

        SessionName = name;
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
}