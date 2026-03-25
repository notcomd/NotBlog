using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Infrastructure.Repository;

public class MessageRepository : Repository<MessageEntity>, IMessageRepository
{
    public MessageRepository(MessageDbContext context) : base(context)
    {
    }

    public async Task<MessageEntity?> GetByIdAsync(Guid messageId)
    {
        return await _dbSet
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.MessageId == messageId);
    }

    public async Task<IEnumerable<MessageEntity>> GetBySessionIdAsync(Guid sessionId, int page = 1, int pageSize = 50)
    {
        var query = _dbSet
            .Include(m => m.Attachments)
            .Where(m => m.SessionId == sessionId && !m.IsRecalled)
            .OrderByDescending(m => m.SentTime);

        return await ApplyPaging(query, page, pageSize).ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetBySenderIdAsync(Guid senderId, int page = 1, int pageSize = 50)
    {
        var query = _dbSet
            .Include(m => m.Attachments)
            .Where(m => m.SenderId == senderId)
            .OrderByDescending(m => m.SentTime);

        return await ApplyPaging(query, page, pageSize).ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetByReceiverIdAsync(Guid receiverId, int page = 1, int pageSize = 50)
    {
        var query = _dbSet
            .Include(m => m.Attachments)
            .Where(m => m.ReceiverId == receiverId)
            .OrderByDescending(m => m.SentTime);

        return await ApplyPaging(query, page, pageSize).ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetUnreadMessagesAsync(Guid userId)
    {
        return await _dbSet
            .Include(m => m.Attachments)
            .Where(m => m.ReceiverId == userId && m.Status == MessageStatus.Sent)
            .OrderBy(m => m.SentTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetMessagesByTypeAsync(MessageType messageType, Guid sessionId)
    {
        return await _dbSet
            .Where(m => m.SessionId == sessionId && m.MessageType == messageType)
            .OrderByDescending(m => m.SentTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime startDate,
        DateTime endDate)
    {
        return await _dbSet
            .Include(m => m.Attachments)
            .Where(m => m.SessionId == sessionId && m.SentTime >= startDate && m.SentTime <= endDate)
            .OrderBy(m => m.SentTime)
            .ToListAsync();
    }

    public async Task<MessageEntity?> GetLastMessageAsync(Guid sessionId)
    {
        return await _dbSet
            .Where(m => m.SessionId == sessionId && !m.IsRecalled)
            .OrderByDescending(m => m.SentTime)
            .FirstOrDefaultAsync();
    }

    public new async Task<MessageEntity> AddAsync(MessageEntity message)
    {
        var entry = await _dbSet.AddAsync(message);
        return entry.Entity;
    }

    public new async Task<MessageEntity> UpdateAsync(MessageEntity message)
    {
        var entry = _dbSet.Update(message);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid messageId)
    {
        var message = await GetByIdAsync(messageId);
        if (message != null)
        {
            _dbSet.Remove(message);
        }
    }

    public async Task<bool> ExistsAsync(Guid messageId)
    {
        return await _dbSet.AnyAsync(m => m.MessageId == messageId);
    }

    public async Task<int> GetUnreadCountAsync(Guid sessionId, Guid userId)
    {
        return await _dbSet
            .CountAsync(m => m.SessionId == sessionId &&
                             m.ReceiverId == userId &&
                             m.Status == MessageStatus.Sent);
    }

    public async Task<int> GetMessageCountBySessionAsync(Guid sessionId)
    {
        return await _dbSet.CountAsync(m => m.SessionId == sessionId);
    }

    public async Task<int> GetMessageCountByUserAsync(Guid userId)
    {
        return await _dbSet.CountAsync(m => m.SenderId == userId || m.ReceiverId == userId);
    }

    public async Task MarkAsReadAsync(Guid messageId, Guid readerId)
    {
        var message = await GetByIdAsync(messageId);
        if (message != null && message.ReceiverId == readerId)
        {
            message.MarkAsRead();
            _dbSet.Update(message);
        }
    }

    public async Task MarkAllAsReadAsync(Guid sessionId, Guid userId)
    {
        var messages = await _dbSet
            .Where(m => m.SessionId == sessionId && m.ReceiverId == userId && m.Status == MessageStatus.Sent)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.MarkAsRead();
        }

        _dbSet.UpdateRange(messages);
    }

    public async Task<IEnumerable<MessageEntity>> SearchAsync(Guid sessionId, string searchTerm, int page, int pageSize)
    {
        var query = _dbSet
            .Include(m => m.Attachments)
            .Where(m => m.SessionId == sessionId &&
                        !m.IsRecalled &&
                        (m.Content != null && m.Content.Contains(searchTerm)));

        return await ApplyPaging(query.OrderByDescending(m => m.SentTime), page, pageSize).ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetForwardedMessagesAsync(Guid originalMessageId)
    {
        return await _dbSet
            .Where(m => m.OriginalMessageId == originalMessageId)
            .OrderByDescending(m => m.SentTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageEntity>> GetRecalledMessagesAsync(Guid sessionId)
    {
        return await _dbSet
            .Where(m => m.SessionId == sessionId && m.IsRecalled)
            .OrderByDescending(m => m.SentTime)
            .ToListAsync();
    }
}