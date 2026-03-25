using Message.Domain.Enums;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Domain.IProvider;

public interface IMessageProvider
{
    Task<MessageEntity> SendTextMessageAsync(Guid sessionId, Guid senderId, string content);

    Task<MessageEntity> SendImageMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri, string? caption = null,
        string? thumbnailUri = null);

    Task<MessageEntity> SendVideoMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null, string? thumbnailUri = null);

    Task<MessageEntity> SendAudioMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri, double durationSeconds,
        string? caption = null);

    Task<MessageEntity> SendFileMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        long fileSize, string mimeType);

    Task<MessageEntity> SendLocationMessageAsync(Guid sessionId, Guid senderId, double latitude, double longitude,
        string locationName);

    Task<MessageEntity> SendLinkMessageAsync(Guid sessionId, Guid senderId, string linkUrl, string? title = null,
        string? description = null);

    Task<MessageEntity> SendExpressionMessageAsync(Guid sessionId, Guid senderId, string expressionCode);

    Task<MessageEntity?> GetMessageAsync(Guid messageId);
    Task<IEnumerable<MessageEntity>> GetSessionMessagesAsync(Guid sessionId, int page = 1, int pageSize = 50);
    Task<IEnumerable<MessageEntity>> GetMessagesBySenderAsync(Guid senderId, int page = 1, int pageSize = 50);
    Task<IEnumerable<MessageEntity>> GetUnreadMessagesAsync(Guid userId);
    Task<IEnumerable<MessageEntity>> GetMessagesByTypeAsync(Guid sessionId, MessageType messageType);
    Task<IEnumerable<MessageEntity>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime startDate, DateTime endDate);
    Task<MessageEntity?> GetLastMessageAsync(Guid sessionId);

    Task<int> GetUnreadCountAsync(Guid sessionId, Guid userId);
    Task<int> GetMessageCountBySessionAsync(Guid sessionId);
    Task<int> GetMessageCountByUserAsync(Guid userId);

    Task MarkAsSentAsync(Guid messageId);
    Task MarkAsDeliveredAsync(Guid messageId);
    Task MarkAsReadAsync(Guid messageId, Guid readerId);
    Task MarkAllAsReadAsync(Guid sessionId, Guid userId);

    Task RecallMessageAsync(Guid messageId, Guid recalledBy, RecallReason reason, string? originalContent = null);

    Task<MessageEntity> ForwardMessageAsync(Guid originalMessageId, Guid targetSessionId, Guid forwardedBy,
        ForwardType forwardType, string? comment = null);

    Task<MessageEntity> ReplyToMessageAsync(Guid originalMessageId, Guid sessionId, Guid senderId, string content);

    Task<IEnumerable<MessageEntity>> SearchMessagesAsync(Guid sessionId, string searchTerm, int page = 1,
        int pageSize = 50);

    Task DeleteMessageAsync(Guid messageId);
}