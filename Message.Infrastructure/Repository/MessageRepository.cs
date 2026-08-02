using Message.Infrastructure.EntityFramework;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Infrastructure.Repository;

public class MessageRepository(MessageDbContext context) :  IMessageRepository
{
  
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<MessageEntity> DbSet = context.Messages;

    public async Task<MessageEntity?> GetByIdAsync(Guid messageId)
    {
        return await DbSet
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.MessageId == messageId);
    }

    public async Task<IEnumerable<MessageEntity>> GetBySessionIdAsync(Guid sessionId, int page = 1, int pageSize = 50)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .Include(m => m.Attachments)
            .Where(m => m.SessionId == sessionId && !m.IsRecalled)
            .OrderByDescending(m => m.SentTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetBySenderIdAsync(Guid senderId, int page = 1, int pageSize = 50)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .Include(m => m.Attachments)
            .Where(m => m.SenderId == senderId)
            .OrderByDescending(m => m.SentTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetByReceiverIdAsync(Guid receiverId, int page = 1, int pageSize = 50)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .Include(m => m.Attachments)
            .Where(m => m.ReceiverId == receiverId)
            .OrderByDescending(m => m.SentTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetUnreadMessagesAsync(Guid userId)
    {
        return await DbSet
            .Include(m => m.Attachments)
            .Where(m => m.ReceiverId == userId && m.Status == MessageStatus.Sent)
            .OrderBy(m => m.SentTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetMessagesByTypeAsync(MessageType messageType, Guid sessionId)
    {
        return await DbSet
            .Where(m => m.SessionId == sessionId && m.MessageType == messageType)
            .OrderByDescending(m => m.SentTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime startDate,
        DateTime endDate)
    {
        return await DbSet
            .Include(m => m.Attachments)
            .Where(m => m.SessionId == sessionId && m.SentTime >= startDate && m.SentTime <= endDate)
            .OrderBy(m => m.SentTime)
            .ToListAsync();
    }

    public async Task<MessageEntity?> GetLastMessageAsync(Guid sessionId)
    {
        return await DbSet
            .Where(m => m.SessionId == sessionId && !m.IsRecalled)
            .OrderByDescending(m => m.SentTime)
            .FirstOrDefaultAsync();
    }

    public async Task<MessageEntity> AddAsync(MessageEntity message)
    {
        var entry = await DbSet.AddAsync(message);
        return entry.Entity;
    }

    public async Task<MessageEntity> UpdateAsync(MessageEntity message)
    {
        var entry = DbSet.Update(message);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid messageId)
    {
        var message = await GetByIdAsync(messageId);
        if (message is not null)
        {
            DbSet.Remove(message);
        }
    }

    public async Task<bool> ExistsAsync(Guid messageId)
    {
        return await DbSet.AnyAsync(m => m.MessageId == messageId);
    }

    public async Task<int> GetUnreadCountAsync(Guid sessionId, Guid userId)
    {
        return await DbSet
            .CountAsync(m => m.SessionId == sessionId &&
                             m.ReceiverId == userId &&
                             m.Status == MessageStatus.Sent);
    }

    public async Task<int> GetMessageCountBySessionAsync(Guid sessionId)
    {
        return await DbSet.CountAsync(m => m.SessionId == sessionId);
    }

    public async Task<int> GetMessageCountByUserAsync(Guid userId)
    {
        return await DbSet.CountAsync(m => m.SenderId == userId || m.ReceiverId == userId);
    }

    public async Task MarkAsReadAsync(Guid messageId, Guid readerId)
    {
        var message = await GetByIdAsync(messageId);
        if (message is not null && message.ReceiverId == readerId)
        {
            message.MarkAsRead();
            DbSet.Update(message);
        }
    }

    public async Task MarkAllAsReadAsync(Guid sessionId, Guid userId)
    {
        var messages = await DbSet
            .Where(m => m.SessionId == sessionId && m.ReceiverId == userId && m.Status == MessageStatus.Sent)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.MarkAsRead();
        }

        DbSet.UpdateRange(messages);
    }

    public async Task<IEnumerable<MessageEntity>> SearchAsync(Guid sessionId, string searchTerm, int page, int pageSize)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .Include(m => m.Attachments)
            .Where(m => m.SessionId == sessionId &&
                        !m.IsRecalled &&
                        (m.Content != null && m.Content.Contains(searchTerm)));

        return await query.OrderByDescending(m => m.SentTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>统计会话内搜索匹配的消息总数（供分页 TotalCount 使用，P-05）。</summary>
    public async Task<int> SearchCountAsync(Guid sessionId, string searchTerm)
    {
        return await DbSet.CountAsync(m => m.SessionId == sessionId &&
                                           !m.IsRecalled &&
                                           (m.Content != null && m.Content.Contains(searchTerm)));
    }

    public async Task<IEnumerable<MessageEntity>> GetForwardedMessagesAsync(Guid originalMessageId)
    {
        return await DbSet
            .Where(m => m.OriginalMessageId == originalMessageId)
            .OrderByDescending(m => m.SentTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetRecalledMessagesAsync(Guid sessionId)
    {
        return await DbSet
            .Where(m => m.SessionId == sessionId && m.IsRecalled)
            .OrderByDescending(m => m.SentTime)
            .ToListAsync();
    }
}