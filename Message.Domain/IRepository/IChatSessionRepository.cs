
namespace Message.Domain.IRepository;

/// <summary>
/// 会话仓储接口（ChatSession 聚合根）。
/// </summary>
public interface IChatSessionRepository : IRepository<ChatSession, IUnitOfWork>
{
    /// <summary>按会话 ID 查询（不存在返回 null）</summary>
    Task<ChatSession?> GetByIdAsync(Guid sessionId);
    /// <summary>获取两人之间的私聊会话（不存在返回 null）</summary>
    Task<ChatSession?> GetPrivateSessionAsync(Guid userId1, Guid userId2);
    /// <summary>获取用户参与的全部会话</summary>
    Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId);
    /// <summary>获取用户置顶的会话</summary>
    Task<IEnumerable<ChatSession>> GetPinnedSessionsAsync(Guid userId);
    /// <summary>按会话类型查询会话</summary>
    Task<IEnumerable<ChatSession>> GetByTypeAsync(SessionType sessionType);
    /// <summary>获取用户的活跃会话</summary>
    Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(Guid userId);
    /// <summary>按群组 ID 查询关联会话（不存在返回 null）</summary>
    Task<ChatSession?> GetByGroupIdAsync(Guid groupId);
    /// <summary>按圈子 ID 查询关联会话（不存在返回 null）</summary>
    Task<ChatSession?> GetByCircleIdAsync(Guid circleId);
    /// <summary>新增会话</summary>
    Task<ChatSession> AddAsync(ChatSession session);
    /// <summary>更新会话</summary>
    Task<ChatSession> UpdateAsync(ChatSession session);
    /// <summary>删除会话</summary>
    Task DeleteAsync(Guid sessionId);
    /// <summary>判断会话是否存在</summary>
    Task<bool> ExistsAsync(Guid sessionId);
    /// <summary>判断两人之间的私聊会话是否存在</summary>
    Task<bool> PrivateSessionExistsAsync(Guid userId1, Guid userId2);
    /// <summary>获取用户参与的会话数量</summary>
    Task<int> GetSessionCountByUserAsync(Guid userId);
    /// <summary>获取用户全部会话的未读消息总数</summary>
    Task<int> GetTotalUnreadCountAsync(Guid userId);
    /// <summary>获取含未读消息的会话列表</summary>
    Task<IEnumerable<ChatSession>> GetSessionsWithUnreadMessagesAsync(Guid userId);
    /// <summary>向会话添加参与者</summary>
    Task AddParticipantAsync(Guid sessionId, Guid userId);
    /// <summary>从会话移除参与者</summary>
    Task RemoveParticipantAsync(Guid sessionId, Guid userId);
    /// <summary>更新会话最后一条消息</summary>
    Task UpdateLastMessageAsync(Guid sessionId, Guid messageId, string? content);
}