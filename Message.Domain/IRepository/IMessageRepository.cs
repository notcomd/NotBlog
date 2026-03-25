using Message.Domain.Enums;
using Message.Domain.SeedWork;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Domain.IRepository;

public interface IMessageRepository : IRepository<MessageEntity>
{
    Task<MessageEntity?> GetByIdAsync(Guid messageId);
    Task<IEnumerable<MessageEntity>> GetBySessionIdAsync(Guid sessionId, int page = 1, int pageSize = 50);
    Task<IEnumerable<MessageEntity>> GetBySenderIdAsync(Guid senderId, int page = 1, int pageSize = 50);
    Task<IEnumerable<MessageEntity>> GetByReceiverIdAsync(Guid receiverId, int page = 1, int pageSize = 50);
    Task<IEnumerable<MessageEntity>> GetUnreadMessagesAsync(Guid userId);
    Task<IEnumerable<MessageEntity>> GetMessagesByTypeAsync(MessageType messageType, Guid sessionId);
    Task<IEnumerable<MessageEntity>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime startDate, DateTime endDate);
    Task<MessageEntity?> GetLastMessageAsync(Guid sessionId);
    Task<MessageEntity> AddAsync(MessageEntity message);
    Task<MessageEntity> UpdateAsync(MessageEntity message);
    Task DeleteAsync(Guid messageId);
    Task<bool> ExistsAsync(Guid messageId);
    Task<int> GetUnreadCountAsync(Guid sessionId, Guid userId);
    Task<int> GetMessageCountBySessionAsync(Guid sessionId);
    Task<int> GetMessageCountByUserAsync(Guid userId);
    Task MarkAsReadAsync(Guid messageId, Guid readerId);
    Task MarkAllAsReadAsync(Guid sessionId, Guid userId);
    Task<IEnumerable<MessageEntity>> SearchAsync(Guid sessionId, string searchTerm, int page = 1, int pageSize = 50);
    Task<IEnumerable<MessageEntity>> GetForwardedMessagesAsync(Guid originalMessageId);
}