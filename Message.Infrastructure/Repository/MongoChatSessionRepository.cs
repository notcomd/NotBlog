using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Message.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Message.Infrastructure.Repository;

/// <summary>
/// 聊天会话的 MongoDB 文档仓储（D2-1：chat_session 集合）。
/// <para>
/// 替代原 EF 会话仓储：会话本体（含成员未读 <see cref="ChatSessionDocument.UnreadCount"/>）落 Mongo 并即时生效。
/// <see cref="UnitOfWork"/> 仍返回共享 <see cref="MessageDbContext"/>，供社交域 EF 副作用提交。
/// </para>
/// </summary>
public class MongoChatSessionRepository(IMongoDatabase database, MessageDbContext context) : IChatSessionRepository
{
    private readonly IMongoCollection<ChatSessionDocument> _sessions = MongoChatCollection.GetSessions(database);

    /// <summary>获取当前数据库上下文作为工作单元（供社交域 EF 副作用提交）。</summary>
    public IUnitOfWork UnitOfWork => context;

    /// <summary>按会话 ID 从 MongoDB 获取会话，不存在时返回 null。</summary>
    public async Task<ChatSession?> GetByIdAsync(Guid sessionId)
    {
        var doc = await _sessions.Find(d => d.SessionId == sessionId).FirstOrDefaultAsync();
        return doc is null ? null : ChatSessionMapper.ToEntity(doc);
    }

    /// <summary>获取两名用户之间有效的私聊会话（排除已解散会话）。</summary>
    public async Task<ChatSession?> GetPrivateSessionAsync(Guid userId1, Guid userId2)
    {
        // 与 EF 语义一致：已解散会话不视为有效私聊会话。
        var doc = await _sessions.Find(d => d.SessionType == SessionType.Private
                                            && !d.IsDismissed
                                            && d.Participants.Contains(userId1)
                                            && d.Participants.Contains(userId2)
                                            && d.Participants.Count == 2)
            .FirstOrDefaultAsync();
        return doc is null ? null : ChatSessionMapper.ToEntity(doc);
    }

    /// <summary>获取指定用户参与的所有未解散会话。</summary>
    public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId)
    {
        var docs = await _sessions.Find(d => !d.IsDismissed && d.Participants.Contains(userId)).ToListAsync();
        return docs.Select(ChatSessionMapper.ToEntity);
    }

    /// <summary>获取指定用户置顶的会话，按最后消息时间倒序。</summary>
    public async Task<IEnumerable<ChatSession>> GetPinnedSessionsAsync(Guid userId)
    {
        // 置顶为用户维度：匹配该成员的状态片段 IsPinned==true
        var docs = await _sessions.Find(d => !d.IsDismissed
                                             && d.Participants.Contains(userId)
                                             && d.MemberStates.Any(s => s.MemberId == userId && s.IsPinned))
            .SortByDescending(d => d.LastMessageTime)
            .ToListAsync();
        return docs.Select(ChatSessionMapper.ToEntity);
    }

    /// <summary>按会话类型获取未解散的会话。</summary>
    public async Task<IEnumerable<ChatSession>> GetByTypeAsync(SessionType sessionType)
    {
        var docs = await _sessions.Find(d => d.SessionType == sessionType && !d.IsDismissed).ToListAsync();
        return docs.Select(ChatSessionMapper.ToEntity);
    }

    /// <summary>获取指定用户的活跃会话，按最后消息时间倒序。</summary>
    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(Guid userId)
    {
        var docs = await _sessions.Find(d => !d.IsDismissed && d.Participants.Contains(userId))
            .SortByDescending(d => d.LastMessageTime)
            .ToListAsync();
        return docs.Select(ChatSessionMapper.ToEntity);
    }

    /// <summary>按群组 ID 获取未解散的会话。</summary>
    public async Task<ChatSession?> GetByGroupIdAsync(Guid groupId)
    {
        var doc = await _sessions.Find(d => d.GroupId == groupId && !d.IsDismissed).FirstOrDefaultAsync();
        return doc is null ? null : ChatSessionMapper.ToEntity(doc);
    }

    /// <summary>按圈子 ID 获取未解散的会话。</summary>
    public async Task<ChatSession?> GetByCircleIdAsync(Guid circleId)
    {
        var doc = await _sessions.Find(d => d.CircleId == circleId && !d.IsDismissed).FirstOrDefaultAsync();
        return doc is null ? null : ChatSessionMapper.ToEntity(doc);
    }

    /// <summary>新增会话（即时写入 MongoDB）并返回该会话。</summary>
    public async Task<ChatSession> AddAsync(ChatSession session)
    {
        await _sessions.InsertOneAsync(ChatSessionMapper.ToDocument(session));
        return session;
    }

    /// <summary>整体替换会话文档并返回该会话。</summary>
    public async Task<ChatSession> UpdateAsync(ChatSession session)
    {
        await ReplaceAsync(ChatSessionMapper.ToDocument(session));
        return session;
    }

    /// <summary>删除会话 = 标记解散（与 EF 语义一致），并即时持久化。</summary>
    public async Task DeleteAsync(Guid sessionId)
    {
        var doc = await _sessions.Find(d => d.SessionId == sessionId).FirstOrDefaultAsync();
        if (doc is null)
            return;
        var session = ChatSessionMapper.ToEntity(doc);
        session.Dismiss();
        await ReplaceAsync(ChatSessionMapper.ToDocument(session));
    }

    /// <summary>判断指定会话是否存在。</summary>
    public Task<bool> ExistsAsync(Guid sessionId)
    {
        return _sessions.Find(d => d.SessionId == sessionId).AnyAsync();
    }

    /// <summary>判断两名用户之间是否存在有效私聊会话。</summary>
    public Task<bool> PrivateSessionExistsAsync(Guid userId1, Guid userId2)
    {
        return _sessions.Find(d => d.SessionType == SessionType.Private
                                   && !d.IsDismissed
                                   && d.Participants.Contains(userId1)
                                   && d.Participants.Contains(userId2))
            .AnyAsync();
    }

    /// <summary>统计指定用户的未解散会话数量。</summary>
    public async Task<int> GetSessionCountByUserAsync(Guid userId)
    {
        return (int)await _sessions.CountDocumentsAsync(d => !d.IsDismissed && d.Participants.Contains(userId));
    }

    /// <summary>统计用户在所有会话中的未读消息总数。</summary>
    public async Task<int> GetTotalUnreadCountAsync(Guid userId)
    {
        var sessions = await _sessions.Find(d => !d.IsDismissed && d.Participants.Contains(userId)).ToListAsync();
        return sessions.Sum(d => d.MemberStates.FirstOrDefault(s => s.MemberId == userId)?.UnreadCount ?? 0);
    }

    /// <summary>获取用户存在未读消息的会话集合。</summary>
    public async Task<IEnumerable<ChatSession>> GetSessionsWithUnreadMessagesAsync(Guid userId)
    {
        var docs = await _sessions.Find(d => !d.IsDismissed && d.Participants.Contains(userId)).ToListAsync();
        return docs
            .Where(d => (d.MemberStates.FirstOrDefault(s => s.MemberId == userId)?.UnreadCount ?? 0) > 0)
            .Select(ChatSessionMapper.ToEntity);
    }

    /// <summary>向会话添加参与者并即时持久化。</summary>
    public async Task AddParticipantAsync(Guid sessionId, Guid userId)
    {
        var doc = await _sessions.Find(d => d.SessionId == sessionId).FirstOrDefaultAsync();
        if (doc is null)
            return;
        var session = ChatSessionMapper.ToEntity(doc);
        session.AddParticipant(userId);
        await ReplaceAsync(ChatSessionMapper.ToDocument(session));
    }

    /// <summary>从会话移除参与者并即时持久化。</summary>
    public async Task RemoveParticipantAsync(Guid sessionId, Guid userId)
    {
        var doc = await _sessions.Find(d => d.SessionId == sessionId).FirstOrDefaultAsync();
        if (doc is null)
            return;
        var session = ChatSessionMapper.ToEntity(doc);
        session.RemoveParticipant(userId);
        await ReplaceAsync(ChatSessionMapper.ToDocument(session));
    }

    /// <summary>更新会话的最后一条消息信息（消息 ID 与内容摘要）并即时持久化。</summary>
    public async Task UpdateLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        var doc = await _sessions.Find(d => d.SessionId == sessionId).FirstOrDefaultAsync();
        if (doc is null)
            return;
        var session = ChatSessionMapper.ToEntity(doc);
        session.UpdateLastMessage(messageId, content);
        await ReplaceAsync(ChatSessionMapper.ToDocument(session));
    }

    /// <summary>整体替换文档并递增乐观锁版本。</summary>
    private async Task ReplaceAsync(ChatSessionDocument doc)
    {
        doc.Version++;
        var filter = Builders<ChatSessionDocument>.Filter.Eq(d => d.SessionId, doc.SessionId);
        await _sessions.ReplaceOneAsync(filter, doc);
    }
}