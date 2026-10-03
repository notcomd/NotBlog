
namespace Message.Infrastructure.Repository;

/// <summary>聊天会话仓储实现，负责 ChatSessions 表的查询与持久化。</summary>
public class ChatSessionRepository(MessageDbContext context) : IChatSessionRepository
{

    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;

    /// <summary>按会话 ID 获取聊天会话，不存在时返回 null。</summary>
       public async Task<ChatSession?> GetByIdAsync(Guid sessionId)
    {
        return await context.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    /// <summary>获取两名用户之间有效的私聊会话（排除已解散会话）。</summary>
    public async Task<ChatSession?> GetPrivateSessionAsync(Guid userId1, Guid userId2)
    {
        // 过滤已解散会话（2026-08-15）：删除好友解散会话后，重新加好友应创建全新会话，
        // 而非复用已解散的旧会话。
        return await context.ChatSessions
            .FirstOrDefaultAsync(s => s.SessionType == SessionType.Private &&
                                      !s.IsDismissed &&
                                      s.ParticipantsInternal.Contains(userId1) &&
                                      s.ParticipantsInternal.Contains(userId2) &&
                                      s.ParticipantsInternal.Count == 2);
    }

    /// <summary>获取指定用户参与的所有未解散会话。</summary>
    public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId)
    {
        return await context.ChatSessions
            .Where(s => !s.IsDismissed && s.ParticipantsInternal.Contains(userId))
            .ToListAsync();
    }

    /// <summary>获取指定用户置顶的会话，按最后消息时间倒序。</summary>
    public async Task<IEnumerable<ChatSession>> GetPinnedSessionsAsync(Guid userId)
    {
        // 置顶为用户维度：MemberStates 未落 PG（Ignore），加载后在内存按成员状态过滤。
        return (await context.ChatSessions
                .Where(s => !s.IsDismissed && s.ParticipantsInternal.Contains(userId))
                .ToListAsync())
            .Where(s => s.MemberStates.TryGetValue(userId, out var st) && st.IsPinned)
            .OrderByDescending(s => s.LastMessageTime);
    }

    /// <summary>按会话类型获取未解散的会话。</summary>
    public async Task<IEnumerable<ChatSession>> GetByTypeAsync(SessionType sessionType)
    {
        return await context.ChatSessions
            .Where(s => s.SessionType == sessionType && !s.IsDismissed)
            .ToListAsync();
    }

    /// <summary>获取指定用户的活跃会话，按最后消息时间倒序。</summary>
    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(Guid userId)
    {
        return await context.ChatSessions
            .Where(s => !s.IsDismissed && s.ParticipantsInternal.Contains(userId))
            .OrderByDescending(s => s.LastMessageTime)
            .ToListAsync();
    }

    /// <summary>按群组 ID 获取未解散的会话。</summary>
    public async Task<ChatSession?> GetByGroupIdAsync(Guid groupId)
    {
        return await context.ChatSessions
            .FirstOrDefaultAsync(s => s.GroupId == groupId && !s.IsDismissed);
    }

    /// <summary>按圈子 ID 获取未解散的会话。</summary>
    public async Task<ChatSession?> GetByCircleIdAsync(Guid circleId)
    {
        return await context.ChatSessions
            .FirstOrDefaultAsync(s => s.CircleId == circleId && !s.IsDismissed);
    }

    /// <summary>新增聊天会话并返回已跟踪的实体。</summary>
    public async Task<ChatSession> AddAsync(ChatSession session)
    {
        var entry = await context.ChatSessions.AddAsync(session);
        return entry.Entity;
    }

    /// <summary>更新聊天会话并返回已跟踪的实体。</summary>
    public async Task<ChatSession> UpdateAsync(ChatSession session)
    {
        var entry = context.ChatSessions.Update(session);
        return entry.Entity;
    }

    /// <summary>解散（软删除）指定会话。</summary>
    public async Task DeleteAsync(Guid sessionId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session is not null)
        {
            session.Dismiss();
            context.ChatSessions.Update(session);
        }
    }

    /// <summary>判断指定会话是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid sessionId)
    {
        return await context.ChatSessions.AnyAsync(s => s.SessionId == sessionId);
    }

    /// <summary>判断两名用户之间是否存在有效私聊会话。</summary>
    public async Task<bool> PrivateSessionExistsAsync(Guid userId1, Guid userId2)
    {
        // 与 GetPrivateSessionAsync 一致：已解散会话不视为有效私聊会话
        return await context.ChatSessions
            .AnyAsync(s => s.SessionType == SessionType.Private &&
                           !s.IsDismissed &&
                           s.ParticipantsInternal.Contains(userId1) &&
                           s.ParticipantsInternal.Contains(userId2));
    }

    /// <summary>统计未解散的会话数量。</summary>
    public async Task<int> GetSessionCountByUserAsync(Guid userId)
    {
        return await context.ChatSessions
            .CountAsync(s => !s.IsDismissed);
    }

    /// <summary>统计用户在所有会话中的未读消息总数。</summary>
    public async Task<int> GetTotalUnreadCountAsync(Guid userId)
    {
        var sessions = await context.ChatSessions
            .Where(s => !s.IsDismissed)
            .ToListAsync();

        return sessions.Sum(s => s.GetUnreadCount(userId));
    }

    /// <summary>获取用户存在未读消息的会话集合。</summary>
    public async Task<IEnumerable<ChatSession>> GetSessionsWithUnreadMessagesAsync(Guid userId)
    {
        var sessions = await context.ChatSessions
            .Where(s => !s.IsDismissed)
            .ToListAsync();

        return sessions.Where(s => s.GetUnreadCount(userId) > 0);
    }

    /// <summary>向会话添加参与者。</summary>
    public async Task AddParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session is not null)
        {
            session.AddParticipant(userId);
            context.ChatSessions.Update(session);
        }
    }

    /// <summary>从会话移除参与者。</summary>
    public async Task RemoveParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session is not null)
        {
            session.RemoveParticipant(userId);
            context.ChatSessions.Update(session);
        }
    }

    /// <summary>更新会话的最后一条消息信息（消息 ID 与内容摘要）。</summary>
    public async Task UpdateLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        var session = await GetByIdAsync(sessionId);
        if (session is not null)
        {
            session.UpdateLastMessage(messageId, content);
            context.ChatSessions.Update(session);
        }
    }
}