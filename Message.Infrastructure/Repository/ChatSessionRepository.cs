using Message.Infrastructure.EntityFramework;

namespace Message.Infrastructure.Repository;

public class ChatSessionRepository(MessageDbContext context) : IChatSessionRepository
{

    public IUnitOfWork UnitOfWork => context;

       public async Task<ChatSession?> GetByIdAsync(Guid sessionId)
    {
        return await context.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    public async Task<ChatSession?> GetPrivateSessionAsync(Guid userId1, Guid userId2)
    {
        return await context.ChatSessions
            .FirstOrDefaultAsync(s => s.SessionType == SessionType.Private &&
                                      s.Participants.Contains(userId1) &&
                                      s.Participants.Contains(userId2) &&
                                      s.Participants.Count == 2);
    }

    public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId)
    {
        return await context.ChatSessions
            .Where(s => !s.IsDismissed && s.Participants.Contains(userId))
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatSession>> GetPinnedSessionsAsync(Guid userId)
    {
        return await context.ChatSessions
            .Where(s => s.IsPinned && !s.IsDismissed && s.Participants.Contains(userId))
            .OrderByDescending(s => s.LastMessageTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatSession>> GetByTypeAsync(SessionType sessionType)
    {
        return await context.ChatSessions
            .Where(s => s.SessionType == sessionType && !s.IsDismissed)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(Guid userId)
    {
        return await context.ChatSessions
            .Where(s => !s.IsDismissed && s.Participants.Contains(userId))
            .OrderByDescending(s => s.LastMessageTime)
            .ToListAsync();
    }

    public async Task<ChatSession?> GetByGroupIdAsync(Guid groupId)
    {
        return await context.ChatSessions
            .FirstOrDefaultAsync(s => s.GroupId == groupId && !s.IsDismissed);
    }

    public async Task<ChatSession> AddAsync(ChatSession session)
    {
        var entry = await context.ChatSessions.AddAsync(session);
        return entry.Entity;
    }

    public async Task<ChatSession> UpdateAsync(ChatSession session)
    {
        var entry = context.ChatSessions.Update(session);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid sessionId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session is not null)
        {
            session.Dismiss();
            context.ChatSessions.Update(session);
        }
    }

    public async Task<bool> ExistsAsync(Guid sessionId)
    {
        return await context.ChatSessions.AnyAsync(s => s.SessionId == sessionId);
    }

    public async Task<bool> PrivateSessionExistsAsync(Guid userId1, Guid userId2)
    {
        return await context.ChatSessions
            .AnyAsync(s => s.SessionType == SessionType.Private &&
                           s.Participants.Contains(userId1) &&
                           s.Participants.Contains(userId2));
    }

    public async Task<int> GetSessionCountByUserAsync(Guid userId)
    {
        return await context.ChatSessions
            .CountAsync(s => !s.IsDismissed);
    }

    public async Task<int> GetTotalUnreadCountAsync(Guid userId)
    {
        var sessions = await context.ChatSessions
            .Where(s => !s.IsDismissed)
            .ToListAsync();

        return sessions.Sum(s => s.GetUnreadCount(userId));
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsWithUnreadMessagesAsync(Guid userId)
    {
        var sessions = await context.ChatSessions
            .Where(s => !s.IsDismissed)
            .ToListAsync();

        return sessions.Where(s => s.GetUnreadCount(userId) > 0);
    }

    public async Task AddParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session is not null)
        {
            session.AddParticipant(userId);
            context.ChatSessions.Update(session);
        }
    }

    public async Task RemoveParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session is not null)
        {
            session.RemoveParticipant(userId);
            context.ChatSessions.Update(session);
        }
    }

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