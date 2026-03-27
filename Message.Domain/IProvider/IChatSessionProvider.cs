using Message.Domain.Entities;

namespace Message.Domain.IProvider;

public interface IChatSessionProvider
{
    Task<ChatSession> CreatePrivateSessionAsync(Guid userId1, Guid userId2);

    Task<ChatSession> CreateGroupSessionAsync(Guid groupId, Guid creatorId, string groupName,
        HashSet<Guid> initialMembers);

    Task<ChatSession?> GetSessionAsync(Guid sessionId);


    Task<ChatSession?> GetPrivateSessionAsync(Guid userId1, Guid userId2);

    Task<IEnumerable<ChatSession>> GetUserSessionsAsync(Guid userId);

    Task<IEnumerable<ChatSession>> GetPinnedSessionsAsync(Guid userId);

    Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(Guid userId);

    Task<ChatSession?> GetSessionByGroupIdAsync(Guid groupId);

    Task<int> GetSessionCountByUserAsync(Guid userId);

    Task<int> GetTotalUnreadCountAsync(Guid userId);

    Task<IEnumerable<ChatSession>> GetSessionsWithUnreadMessagesAsync(Guid userId);

    Task AddParticipantAsync(Guid sessionId, Guid userId);

    Task RemoveParticipantAsync(Guid sessionId, Guid userId);

    Task<bool> IsParticipantAsync(Guid sessionId, Guid userId);

    Task UpdateLastMessageAsync(Guid sessionId, Guid messageId, string? content);

    Task MarkSessionAsReadAsync(Guid sessionId, Guid userId);


    Task PinSessionAsync(Guid sessionId);

    Task UnpinSessionAsync(Guid sessionId);

    Task MuteSessionAsync(Guid sessionId);

    Task UnmuteSessionAsync(Guid sessionId);

    Task DismissSessionAsync(Guid sessionId);


    Task<bool> SessionExistsAsync(Guid sessionId);

    Task<bool> PrivateSessionExistsAsync(Guid userId1, Guid userId2);
}