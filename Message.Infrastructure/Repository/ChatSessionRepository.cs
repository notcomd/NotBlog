using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Message.Infrastructure.Repository;

public class ChatSessionRepository : Repository<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(MessageDbContext context) : base(context)
    {
    }

    public async Task<ChatSession?> GetByIdAsync(Guid sessionId)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    public async Task<ChatSession?> GetPrivateSessionAsync(Guid userId1, Guid userId2)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.SessionType == SessionType.Private &&
                                      s.Participants.Contains(userId1) &&
                                      s.Participants.Contains(userId2) &&
                                      s.Participants.Count == 2);
    }

    public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(s => !s.IsDismissed)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatSession>> GetPinnedSessionsAsync(Guid userId)
    {
        return await _dbSet
            .Where(s => s.IsPinned && !s.IsDismissed)
            .OrderByDescending(s => s.LastMessageTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatSession>> GetByTypeAsync(SessionType sessionType)
    {
        return await _dbSet
            .Where(s => s.SessionType == sessionType && !s.IsDismissed)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(Guid userId)
    {
        return await _dbSet
            .Where(s => !s.IsDismissed)
            .OrderByDescending(s => s.LastMessageTime)
            .ToListAsync();
    }

    public async Task<ChatSession?> GetByGroupIdAsync(Guid groupId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.GroupId == groupId && !s.IsDismissed);
    }

    public new async Task<ChatSession> AddAsync(ChatSession session)
    {
        var entry = await _dbSet.AddAsync(session);
        return entry.Entity;
    }

    public new async Task<ChatSession> UpdateAsync(ChatSession session)
    {
        var entry = _dbSet.Update(session);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid sessionId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session != null)
        {
            session.Dismiss();
            _dbSet.Update(session);
        }
    }

    public async Task<bool> ExistsAsync(Guid sessionId)
    {
        return await _dbSet.AnyAsync(s => s.SessionId == sessionId);
    }

    public async Task<bool> PrivateSessionExistsAsync(Guid userId1, Guid userId2)
    {
        return await _dbSet
            .AnyAsync(s => s.SessionType == SessionType.Private &&
                           s.Participants.Contains(userId1) &&
                           s.Participants.Contains(userId2));
    }

    public async Task<int> GetSessionCountByUserAsync(Guid userId)
    {
        return await _dbSet
            .CountAsync(s => !s.IsDismissed);
    }

    public async Task<int> GetTotalUnreadCountAsync(Guid userId)
    {
        var sessions = await _dbSet
            .Where(s => !s.IsDismissed)
            .ToListAsync();

        return sessions.Sum(s => s.GetUnreadCount(userId));
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsWithUnreadMessagesAsync(Guid userId)
    {
        var sessions = await _dbSet
            .Where(s => !s.IsDismissed)
            .ToListAsync();

        return sessions.Where(s => s.GetUnreadCount(userId) > 0);
    }

    public async Task AddParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session != null)
        {
            session.AddParticipant(userId);
            _dbSet.Update(session);
        }
    }

    public async Task RemoveParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session != null)
        {
            session.RemoveParticipant(userId);
            _dbSet.Update(session);
        }
    }

    public async Task UpdateLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        var session = await GetByIdAsync(sessionId);
        if (session != null)
        {
            session.UpdateLastMessage(messageId, content);
            _dbSet.Update(session);
        }
    }
}