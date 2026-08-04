using Message.Domain.Entities;
using Message.Domain.Enums;
using Commons.SeedWork;

namespace Message.Domain.IRepository;

public interface IChatSessionRepository : IRepository<ChatSession, IUnitOfWork>
{
    Task<ChatSession?> GetByIdAsync(Guid sessionId);
    Task<ChatSession?> GetPrivateSessionAsync(Guid userId1, Guid userId2);
    Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<ChatSession>> GetPinnedSessionsAsync(Guid userId);
    Task<IEnumerable<ChatSession>> GetByTypeAsync(SessionType sessionType);
    Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(Guid userId);
    Task<ChatSession?> GetByGroupIdAsync(Guid groupId);
    Task<ChatSession> AddAsync(ChatSession session);
    Task<ChatSession> UpdateAsync(ChatSession session);
    Task DeleteAsync(Guid sessionId);
    Task<bool> ExistsAsync(Guid sessionId);
    Task<bool> PrivateSessionExistsAsync(Guid userId1, Guid userId2);
    Task<int> GetSessionCountByUserAsync(Guid userId);
    Task<int> GetTotalUnreadCountAsync(Guid userId);
    Task<IEnumerable<ChatSession>> GetSessionsWithUnreadMessagesAsync(Guid userId);
    Task AddParticipantAsync(Guid sessionId, Guid userId);
    Task RemoveParticipantAsync(Guid sessionId, Guid userId);
    Task UpdateLastMessageAsync(Guid sessionId, Guid messageId, string? content);
}