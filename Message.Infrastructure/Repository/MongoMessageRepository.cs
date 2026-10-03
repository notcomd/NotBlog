using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Message.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Infrastructure.Repository;

/// <summary>
/// 聊天消息的 MongoDB 文档仓储（D2-1：message 集合）。
/// <para>
/// 替代原 EF 消息仓储：消息本体落 Mongo 并即时生效（无 SaveChanges 语义，对标
/// <c>MongoFileChunkRepository</c>）。<see cref="UnitOfWork"/> 仍返回共享的
/// <see cref="MessageDbContext"/>——社交域（FileAttachment/群/好友/Tweet）仍留 PG，
/// 命令层的 <c>UnitOfWork.SaveEntitiesAsync</c> 负责 EF 副作用提交与领域事件派发。
/// </para>
/// </summary>
public class MongoMessageRepository(IMongoDatabase database, MessageDbContext context) : IMessageRepository
{
    private readonly IMongoCollection<ChatMessageDocument> _messages = MongoChatCollection.GetMessages(database);

    /// <summary>获取当前数据库上下文作为工作单元（供社交域 EF 副作用提交）。</summary>
    public IUnitOfWork UnitOfWork => context;

    /// <summary>按消息 ID 从 MongoDB 获取消息，不存在时返回 null。</summary>
    public Task<MessageEntity?> GetByIdAsync(Guid messageId)
    {
        return _messages.Find(d => d.MessageId == messageId)
            .FirstOrDefaultAsync()
            .ContinueWith(t => t.Result is null ? null : ChatMessageMapper.ToEntity(t.Result),
                TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// <summary>分页获取会话内的未撤回消息，按发送时间与消息 ID 倒序。</summary>
    public async Task<IEnumerable<MessageEntity>> GetBySessionIdAsync(Guid sessionId, int page = 1, int pageSize = 50)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // ⚠️ 分页稳定性：SentTime 为毫秒精度，同一毫秒的多条消息若无第二排序键，
        // MongoDB 返回次序不稳定 → Skip/Limit 在「同时间戳组」内漂移 → 跨页重复/遗漏。
        // MessageId 作为 tie-breaker 保证全序确定（与 SentTime 同向 desc）。
        var docs = await _messages.Find(d => d.SessionId == sessionId && !d.IsRecalled)
            .SortByDescending(d => d.SentTime)
            .ThenByDescending(d => d.MessageId)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        return docs.Select(ChatMessageMapper.ToEntity);
    }

    /// <summary>分页获取指定发送者的消息，按发送时间与消息 ID 倒序。</summary>
    public async Task<IEnumerable<MessageEntity>> GetBySenderIdAsync(Guid senderId, int page = 1, int pageSize = 50)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var docs = await _messages.Find(d => d.SenderId == senderId)
            .SortByDescending(d => d.SentTime)
            .ThenByDescending(d => d.MessageId)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        return docs.Select(ChatMessageMapper.ToEntity);
    }

    /// <summary>分页获取指定接收者的消息，按发送时间与消息 ID 倒序。</summary>
    public async Task<IEnumerable<MessageEntity>> GetByReceiverIdAsync(Guid receiverId, int page = 1,
        int pageSize = 50)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var docs = await _messages.Find(d => d.ReceiverId == receiverId)
            .SortByDescending(d => d.SentTime)
            .ThenByDescending(d => d.MessageId)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        return docs.Select(ChatMessageMapper.ToEntity);
    }

    /// <summary>获取指定接收者收到的全部未读消息，按发送时间与消息 ID 升序。</summary>
    public async Task<IEnumerable<MessageEntity>> GetUnreadMessagesAsync(Guid userId)
    {
        var docs = await _messages.Find(d => d.ReceiverId == userId && d.Status == MessageStatus.Sent)
            .SortBy(d => d.SentTime)
            .ThenBy(d => d.MessageId)
            .ToListAsync();
        return docs.Select(ChatMessageMapper.ToEntity);
    }

    /// <summary>获取会话内指定类型的消息，按发送时间与消息 ID 倒序。</summary>
    public async Task<IEnumerable<MessageEntity>> GetMessagesByTypeAsync(MessageType messageType, Guid sessionId)
    {
        var docs = await _messages.Find(d => d.SessionId == sessionId && d.MessageType == messageType)
            .SortByDescending(d => d.SentTime)
            .ThenByDescending(d => d.MessageId)
            .ToListAsync();
        return docs.Select(ChatMessageMapper.ToEntity);
    }

    /// <summary>获取会话内指定时间范围内的消息，按发送时间与消息 ID 升序。</summary>
    public async Task<IEnumerable<MessageEntity>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime startDate,
        DateTime endDate)
    {
        var docs = await _messages.Find(d => d.SessionId == sessionId
                                             && d.SentTime >= startDate && d.SentTime <= endDate)
            .SortBy(d => d.SentTime)
            .ThenBy(d => d.MessageId)
            .ToListAsync();
        return docs.Select(ChatMessageMapper.ToEntity);
    }

    /// <summary>获取会话内最后一条未撤回消息。</summary>
    public async Task<MessageEntity?> GetLastMessageAsync(Guid sessionId)
    {
        var doc = await _messages.Find(d => d.SessionId == sessionId && !d.IsRecalled)
            .SortByDescending(d => d.SentTime)
            .ThenByDescending(d => d.MessageId)
            .FirstOrDefaultAsync();
        return doc is null ? null : ChatMessageMapper.ToEntity(doc);
    }

    /// <summary>新增消息（即时写入 MongoDB）并返回该消息。</summary>
    public async Task<MessageEntity> AddAsync(MessageEntity message)
    {
        // Mongo 即时持久化：不依赖 SaveEntitiesAsync 提交。
        await _messages.InsertOneAsync(ChatMessageMapper.ToDocument(message));
        return message;
    }

    /// <summary>整体替换消息文档并返回该消息。</summary>
    public async Task<MessageEntity> UpdateAsync(MessageEntity message)
    {
        // 整体替换文档（Repository/ReplacedOne 语义与 EF Update 对齐）。
        await _messages.ReplaceOneAsync(d => d.MessageId == message.MessageId,
            ChatMessageMapper.ToDocument(message));
        return message;
    }

    /// <summary>删除指定消息文档。</summary>
    public async Task DeleteAsync(Guid messageId)
    {
        await _messages.DeleteOneAsync(d => d.MessageId == messageId);
    }

    /// <summary>判断指定消息是否存在。</summary>
    public Task<bool> ExistsAsync(Guid messageId)
    {
        return _messages.Find(d => d.MessageId == messageId).AnyAsync();
    }

    /// <summary>统计会话内指定用户的未读消息数量。</summary>
    public async Task<int> GetUnreadCountAsync(Guid sessionId, Guid userId)
    {
        return (int)await _messages.CountDocumentsAsync(d => d.SessionId == sessionId
                                                             && d.ReceiverId == userId
                                                             && d.Status == MessageStatus.Sent);
    }

    /// <summary>统计会话内的消息总数。</summary>
    public async Task<int> GetMessageCountBySessionAsync(Guid sessionId)
    {
        return (int)await _messages.CountDocumentsAsync(d => d.SessionId == sessionId);
    }

    /// <summary>统计与指定用户相关（发送或接收）的消息总数。</summary>
    public async Task<int> GetMessageCountByUserAsync(Guid userId)
    {
        return (int)await _messages.CountDocumentsAsync(
            Builders<ChatMessageDocument>.Filter.Or(
                Builders<ChatMessageDocument>.Filter.Eq(d => d.SenderId, userId),
                Builders<ChatMessageDocument>.Filter.Eq(d => d.ReceiverId, userId)));
    }

    /// <summary>将指定消息标记为已读（仅当读取者为接收者时生效）。</summary>
    public async Task MarkAsReadAsync(Guid messageId, Guid readerId)
    {
        var message = await GetByIdAsync(messageId);
        if (message is not null && message.ReceiverId == readerId)
        {
            message.MarkAsRead();
            await _messages.ReplaceOneAsync(d => d.MessageId == messageId, ChatMessageMapper.ToDocument(message));
        }
    }

    /// <summary>将会话内指定用户收到的全部未读消息标记为已读。</summary>
    public async Task MarkAllAsReadAsync(Guid sessionId, Guid userId)
    {
        var docs = await _messages.Find(d => d.SessionId == sessionId
                                             && d.ReceiverId == userId
                                             && d.Status == MessageStatus.Sent)
            .ToListAsync();

        foreach (var doc in docs)
        {
            doc.Status = MessageStatus.Read;
            doc.ReadTime = DateTime.UtcNow;
            await _messages.ReplaceOneAsync(d => d.MessageId == doc.MessageId, doc);
        }
    }

    /// <summary>在会话内按内容正则搜索消息（不区分大小写），按发送时间与消息 ID 倒序。</summary>
    public async Task<IEnumerable<MessageEntity>> SearchAsync(Guid sessionId, string searchTerm, int page,
        int pageSize)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var regex = new BsonRegularExpression(RegexEscape(searchTerm), "i");
        var filter = Builders<ChatMessageDocument>.Filter.And(
            Builders<ChatMessageDocument>.Filter.Eq(d => d.SessionId, sessionId),
            Builders<ChatMessageDocument>.Filter.Eq(d => d.IsRecalled, false),
            Builders<ChatMessageDocument>.Filter.Regex(d => d.Content, regex));

        var docs = await _messages.Find(filter)
            .SortByDescending(d => d.SentTime)
            .ThenByDescending(d => d.MessageId)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        return docs.Select(ChatMessageMapper.ToEntity);
    }

    /// <summary>统计会话内搜索匹配的消息总数（供分页 TotalCount 使用）。</summary>
    public async Task<int> SearchCountAsync(Guid sessionId, string searchTerm)
    {
        var regex = new BsonRegularExpression(RegexEscape(searchTerm), "i");
        var filter = Builders<ChatMessageDocument>.Filter.And(
            Builders<ChatMessageDocument>.Filter.Eq(d => d.SessionId, sessionId),
            Builders<ChatMessageDocument>.Filter.Eq(d => d.IsRecalled, false),
            Builders<ChatMessageDocument>.Filter.Regex(d => d.Content, regex));
        return (int)await _messages.CountDocumentsAsync(filter);
    }

    /// <summary>获取由指定原始消息转发产生的消息集合，按发送时间与消息 ID 倒序。</summary>
    public async Task<IEnumerable<MessageEntity>> GetForwardedMessagesAsync(Guid originalMessageId)
    {
        var docs = await _messages.Find(d => d.OriginalMessageId == originalMessageId)
            .SortByDescending(d => d.SentTime)
            .ThenByDescending(d => d.MessageId)
            .ToListAsync();
        return docs.Select(ChatMessageMapper.ToEntity);
    }

    private static string RegexEscape(string term) =>
        System.Text.RegularExpressions.Regex.Escape(term ?? string.Empty);
}