namespace Message.Infrastructure.Provider;

public class ChatSessionProvider : IChatSessionProvider
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;


    public ChatSessionProvider(
        IChatSessionRepository sessionRepository,
        IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;

        _unitOfWork = unitOfWork;
    }

    public async Task<ChatSession> CreatePrivateSessionAsync(Guid userId1, Guid userId2)
    {
        var existingSession = await _sessionRepository.GetPrivateSessionAsync(userId1, userId2);
        if (existingSession != null)
            return existingSession;


        var session = ChatSession.CreatePrivateSession(userId1, userId2);
        await _sessionRepository.AddAsync(session);
        await _unitOfWork.SaveEntitiesAsync();

        return session;
    }

    public async Task<ChatSession> CreateGroupSessionAsync(Guid groupId, Guid creatorId, string groupName,
        HashSet<Guid> initialMembers)
    {
        var session = ChatSession.CreateGroupSession(groupId, creatorId, groupName, initialMembers);
        await _sessionRepository.AddAsync(session);
        await _unitOfWork.SaveEntitiesAsync();

        return session;
    }

    public async Task<ChatSession> CreateSessionAsync(Guid userId, SessionType sessionType, Guid? friendId,
        Guid? groupId, string? sessionName, HashSet<Guid>? initialMembers)
    {
        if (sessionType == SessionType.Private)
        {
            return await CreatePrivateSessionAsync(userId, friendId!.Value);
        }

        return await CreateGroupSessionAsync(
            groupId!.Value,
            userId,
            sessionName!,
            initialMembers ?? new HashSet<Guid>());
    }

    public async Task<ChatSession?> GetSessionAsync(Guid sessionId)
    {
        return await _sessionRepository.GetByIdAsync(sessionId);
    }

    public async Task<ChatSession?> GetPrivateSessionAsync(Guid userId1, Guid userId2)
    {
        return await _sessionRepository.GetPrivateSessionAsync(userId1, userId2);
    }

    public async Task<IEnumerable<ChatSession>> GetUserSessionsAsync(Guid userId)
    {
        return await _sessionRepository.GetByUserIdAsync(userId);
    }

    public async Task<IEnumerable<ChatSession>> GetPinnedSessionsAsync(Guid userId)
    {
        return await _sessionRepository.GetPinnedSessionsAsync(userId);
    }

    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(Guid userId)
    {
        return await _sessionRepository.GetActiveSessionsAsync(userId);
    }

    public async Task<ChatSession?> GetSessionByGroupIdAsync(Guid groupId)
    {
        return await _sessionRepository.GetByGroupIdAsync(groupId);
    }

    public async Task<int> GetSessionCountByUserAsync(Guid userId)
    {
        return await _sessionRepository.GetSessionCountByUserAsync(userId);
    }

    public async Task<int> GetTotalUnreadCountAsync(Guid userId)
    {
        return await _sessionRepository.GetTotalUnreadCountAsync(userId);
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsWithUnreadMessagesAsync(Guid userId)
    {
        return await _sessionRepository.GetSessionsWithUnreadMessagesAsync(userId);
    }

    public async Task AddParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");


        session.AddParticipant(userId);
        await _sessionRepository.UpdateAsync(session);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task RemoveParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.RemoveParticipant(userId);
        await _sessionRepository.UpdateAsync(session);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task<bool> IsParticipantAsync(Guid sessionId, Guid userId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        return session?.IsParticipant(userId) ?? false;
    }

    public async Task UpdateLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        await _sessionRepository.UpdateLastMessageAsync(sessionId, messageId, content);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task MarkSessionAsReadAsync(Guid sessionId, Guid userId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.MarkAsRead(userId);
        await _sessionRepository.UpdateAsync(session);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task PinSessionAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.Pin();
        await _sessionRepository.UpdateAsync(session);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task UnpinSessionAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.Unpin();
        await _sessionRepository.UpdateAsync(session);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task MuteSessionAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.Mute();
        await _sessionRepository.UpdateAsync(session);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task UnmuteSessionAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.Unmute();
        await _sessionRepository.UpdateAsync(session);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task DismissSessionAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.Dismiss();
        await _sessionRepository.UpdateAsync(session);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task<bool> SessionExistsAsync(Guid sessionId)
    {
        return await _sessionRepository.ExistsAsync(sessionId);
    }

    public async Task<bool> PrivateSessionExistsAsync(Guid userId1, Guid userId2)
    {
        return await _sessionRepository.PrivateSessionExistsAsync(userId1, userId2);
    }
}