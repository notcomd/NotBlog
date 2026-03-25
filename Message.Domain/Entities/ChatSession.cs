using Message.Domain.Enums;
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

    public Guid SessionId { get; init; }
    public SessionType SessionType { get; init; }
    public string? SessionName { get; private set; }
    public Guid? GroupId { get; init; }
    public Guid CreatorId { get; init; }
    public HashSet<Guid> Participants { get; private set; }
    public Guid? LastMessageId { get; private set; }
    public string? LastMessageContent { get; private set; }
    public DateTime? LastMessageTime { get; private set; }
    public DateTime CreatedTime { get; init; }
    public DateTime? DismissedTime { get; private set; }
    public bool IsDismissed { get; private set; }
    public bool IsPinned { get; private set; }
    public bool IsMuted { get; private set; }
    public IReadOnlyDictionary<Guid, int> UnreadCount => _unreadCount;
    public IReadOnlyDictionary<Guid, DateTime> LastReadTime => _lastReadTime;

    public static ChatSession CreatePrivateSession(Guid userId1, Guid userId2)
    {
        var participants = new HashSet<Guid> { userId1, userId2 };
        return new ChatSession(SessionType.Private, userId1, participants);
    }

    public static ChatSession CreateGroupSession(Guid groupId, Guid creatorId, string groupName,
        HashSet<Guid> initialMembers)
    {
        var participants = new HashSet<Guid>(initialMembers) { creatorId };
        return new ChatSession(SessionType.Group, creatorId, participants, groupName)
        {
            GroupId = groupId
        };
    }

    public void AddParticipant(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("会话已解散");
        if (Participants.Contains(userId))
            throw new InvalidOperationException("用户已在会话中");

        Participants.Add(userId);
        _unreadCount[userId] = 0;
    }

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

    public void MarkAsRead(Guid userId)
    {
        if (!Participants.Contains(userId))
            throw new InvalidOperationException("用户不在会话中");

        if (_unreadCount.ContainsKey(userId) && _unreadCount[userId] > 0)
            _unreadCount[userId] = 0;

        _lastReadTime[userId] = DateTime.UtcNow;
    }

    public int GetUnreadCount(Guid userId)
    {
        return _unreadCount.TryGetValue(userId, out var count) ? count : 0;
    }

    public void Pin()
    {
        IsPinned = true;
    }

    public void Unpin()
    {
        IsPinned = false;
    }

    public void Mute()
    {
        IsMuted = true;
    }

    public void Unmute()
    {
        IsMuted = false;
    }

    public void Dismiss()
    {
        if (IsDismissed)
            throw new InvalidOperationException("会话已解散");

        IsDismissed = true;
        DismissedTime = DateTime.UtcNow;
    }

    public void UpdateSessionName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("会话名称不能为空", nameof(name));

        SessionName = name;
    }

    public bool IsParticipant(Guid userId)
    {
        return Participants.Contains(userId);
    }
}