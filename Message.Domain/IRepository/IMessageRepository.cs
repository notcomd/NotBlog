using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Domain.IRepository;

/// <summary>
/// 消息仓储接口（Message 聚合根）。
/// </summary>
public interface IMessageRepository : IRepository<MessageEntity, IUnitOfWork>
{
    /// <summary>按消息 ID 查询（不存在返回 null）</summary>
    Task<MessageEntity?> GetByIdAsync(Guid messageId);
    /// <summary>分页获取会话消息（按时间倒序）</summary>
    Task<IEnumerable<MessageEntity>> GetBySessionIdAsync(Guid sessionId, int page = 1, int pageSize = 50);
    /// <summary>分页获取某发送者的消息</summary>
    Task<IEnumerable<MessageEntity>> GetBySenderIdAsync(Guid senderId, int page = 1, int pageSize = 50);
    /// <summary>分页获取某接收者的消息</summary>
    Task<IEnumerable<MessageEntity>> GetByReceiverIdAsync(Guid receiverId, int page = 1, int pageSize = 50);
    /// <summary>获取用户的未读消息</summary>
    Task<IEnumerable<MessageEntity>> GetUnreadMessagesAsync(Guid userId);
    /// <summary>按消息类型获取会话内的消息</summary>
    Task<IEnumerable<MessageEntity>> GetMessagesByTypeAsync(MessageType messageType, Guid sessionId);
    /// <summary>按日期范围获取会话内的消息</summary>
    Task<IEnumerable<MessageEntity>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime startDate, DateTime endDate);
    /// <summary>获取会话最后一条消息（不存在返回 null）</summary>
    Task<MessageEntity?> GetLastMessageAsync(Guid sessionId);
    /// <summary>新增消息</summary>
    Task<MessageEntity> AddAsync(MessageEntity message);
    /// <summary>更新消息</summary>
    Task<MessageEntity> UpdateAsync(MessageEntity message);
    /// <summary>删除消息</summary>
    Task DeleteAsync(Guid messageId);
    /// <summary>判断消息是否存在</summary>
    Task<bool> ExistsAsync(Guid messageId);
    /// <summary>获取用户在指定会话中的未读消息数</summary>
    Task<int> GetUnreadCountAsync(Guid sessionId, Guid userId);
    /// <summary>获取会话的消息总数</summary>
    Task<int> GetMessageCountBySessionAsync(Guid sessionId);
    /// <summary>获取用户的消息总数</summary>
    Task<int> GetMessageCountByUserAsync(Guid userId);
    /// <summary>将单条消息标记为已读</summary>
    Task MarkAsReadAsync(Guid messageId, Guid readerId);
    /// <summary>将会话内全部消息标记为已读</summary>
    Task MarkAllAsReadAsync(Guid sessionId, Guid userId);
    /// <summary>分页搜索会话内的消息</summary>
    Task<IEnumerable<MessageEntity>> SearchAsync(Guid sessionId, string searchTerm, int page = 1, int pageSize = 50);
    /// <summary>统计会话内搜索结果数量</summary>
    Task<int> SearchCountAsync(Guid sessionId, string searchTerm);
    /// <summary>获取转发自指定原始消息的消息列表</summary>
    Task<IEnumerable<MessageEntity>> GetForwardedMessagesAsync(Guid originalMessageId);
}