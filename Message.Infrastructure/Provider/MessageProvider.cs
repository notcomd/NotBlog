using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Domain.SeedWork;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Infrastructure.Provider;

public class MessageProvider(
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IMessageProvider
{
    public async Task<MessageEntity> SendTextMessageAsync(Guid sessionId, Guid senderId, string content)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateTextMessage(sessionId, senderId, content);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, content);
        await unitOfWork.SavaEntitiesAsync();

        return message;
    }

    public async Task<MessageEntity> SendImageMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        string? caption = null, string? thumbnailUri = null)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateImageMessage(sessionId, senderId, mediaUri, caption, thumbnailUri);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[图片]");
        await unitOfWork.SavaEntitiesAsync();

        return message;
    }

    public async Task<MessageEntity> SendVideoMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        double durationSeconds, string? caption = null, string? thumbnailUri = null)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message =
            MessageEntity.CreateVideoMessage(sessionId, senderId, mediaUri, durationSeconds, caption, thumbnailUri);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[视频]");
        await unitOfWork.SavaEntitiesAsync();

        return message;
    }

    public async Task<MessageEntity> SendAudioMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        double durationSeconds, string? caption = null)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateAudioMessage(sessionId, senderId, mediaUri, durationSeconds, caption);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[语音]");
        await unitOfWork.SavaEntitiesAsync();

        return message;
    }

    public async Task<MessageEntity> SendFileMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        long fileSize, string mimeType)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateFileMessage(sessionId, senderId, mediaUri, fileName, fileSize, mimeType);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[文件] {fileName}");
        await unitOfWork.SavaEntitiesAsync();

        return message;
    }

    public async Task<MessageEntity> SendLocationMessageAsync(Guid sessionId, Guid senderId, double latitude,
        double longitude, string locationName)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateLocationMessage(sessionId, senderId, latitude, longitude, locationName);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[位置] {locationName}");
        await unitOfWork.SavaEntitiesAsync();

        return message;
    }

    public async Task<MessageEntity> SendLinkMessageAsync(Guid sessionId, Guid senderId, string linkUrl,
        string? title = null, string? description = null)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateLinkMessage(sessionId, senderId, linkUrl, title, description);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, title ?? linkUrl);
        await unitOfWork.SavaEntitiesAsync();

        return message;
    }

    public async Task<MessageEntity> SendExpressionMessageAsync(Guid sessionId, Guid senderId, string expressionCode)
    {
        await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateExpressionMessage(sessionId, senderId, expressionCode);
        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[表情]");
        await unitOfWork.SavaEntitiesAsync();

        return message;
    }

    public async Task<MessageEntity?> GetMessageAsync(Guid messageId)
    {
        return await messageRepository.GetByIdAsync(messageId);
    }

    public async Task<IEnumerable<MessageEntity>> GetSessionMessagesAsync(Guid sessionId, int page = 1,
        int pageSize = 50)
    {
        return await messageRepository.GetBySessionIdAsync(sessionId, page, pageSize);
    }

    public async Task<IEnumerable<MessageEntity>> GetMessagesBySenderAsync(Guid senderId, int page = 1,
        int pageSize = 50)
    {
        return await messageRepository.GetBySenderIdAsync(senderId, page, pageSize);
    }

    public async Task<IEnumerable<MessageEntity>> GetUnreadMessagesAsync(Guid userId)
    {
        return await messageRepository.GetUnreadMessagesAsync(userId);
    }

    public async Task<IEnumerable<MessageEntity>> GetMessagesByTypeAsync(Guid sessionId, MessageType messageType)
    {
        return await messageRepository.GetMessagesByTypeAsync(messageType, sessionId);
    }

    public async Task<IEnumerable<MessageEntity>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime startDate,
        DateTime endDate)
    {
        return await messageRepository.GetMessagesByDateRangeAsync(sessionId, startDate, endDate);
    }

    public async Task<MessageEntity?> GetLastMessageAsync(Guid sessionId)
    {
        return await messageRepository.GetLastMessageAsync(sessionId);
    }

    public async Task<int> GetUnreadCountAsync(Guid sessionId, Guid userId)
    {
        return await messageRepository.GetUnreadCountAsync(sessionId, userId);
    }

    public async Task<int> GetMessageCountBySessionAsync(Guid sessionId)
    {
        return await messageRepository.GetMessageCountBySessionAsync(sessionId);
    }

    public async Task<int> GetMessageCountByUserAsync(Guid userId)
    {
        return await messageRepository.GetMessageCountByUserAsync(userId);
    }

    public async Task MarkAsSentAsync(Guid messageId)
    {
        var message = await messageRepository.GetByIdAsync(messageId);
        if (message == null)
            throw new KeyNotFoundException("消息不存在");

        message.MarkAsSent();
        await messageRepository.UpdateAsync(message);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task MarkAsDeliveredAsync(Guid messageId)
    {
        var message = await messageRepository.GetByIdAsync(messageId);
        if (message == null)
            throw new KeyNotFoundException("消息不存在");

        message.MarkAsDelivered();
        await messageRepository.UpdateAsync(message);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task MarkAsReadAsync(Guid messageId, Guid readerId)
    {
        await messageRepository.MarkAsReadAsync(messageId, readerId);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid sessionId, Guid userId)
    {
        await messageRepository.MarkAllAsReadAsync(sessionId, userId);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task RecallMessageAsync(Guid messageId, Guid recalledBy, RecallReason reason,
        string? originalContent = null)
    {
        var message = await messageRepository.GetByIdAsync(messageId);
        if (message == null)
            throw new KeyNotFoundException("消息不存在");

        message.Recall(recalledBy, reason, originalContent);
        await messageRepository.UpdateAsync(message);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task<MessageEntity> ForwardMessageAsync(Guid originalMessageId, Guid targetSessionId, Guid forwardedBy,
        ForwardType forwardType, string? comment = null)
    {
        var originalMessage = await messageRepository.GetByIdAsync(originalMessageId);
        if (originalMessage == null)
            throw new KeyNotFoundException("原消息不存在");

        await ValidateSessionAndSenderAsync(targetSessionId, forwardedBy);

        var forwardedMessage = CreateForwardedMessage(originalMessage, targetSessionId, forwardedBy);
        forwardedMessage.MarkAsForwarded(originalMessageId);

        await messageRepository.AddAsync(forwardedMessage);
        await unitOfWork.SavaEntitiesAsync();

        return forwardedMessage;
    }

    public async Task<MessageEntity> ReplyToMessageAsync(Guid originalMessageId, Guid sessionId, Guid senderId,
        string content)
    {
        var originalMessage = await messageRepository.GetByIdAsync(originalMessageId);
        if (originalMessage == null)
            throw new KeyNotFoundException("原消息不存在");

        var replyMessage = MessageEntity.CreateTextMessage(sessionId, senderId, content);
        replyMessage.SetReplyTo(originalMessageId);

        await messageRepository.AddAsync(replyMessage);
        await UpdateSessionLastMessageAsync(sessionId, replyMessage.MessageId, content);
        await unitOfWork.SavaEntitiesAsync();

        return replyMessage;
    }

    public async Task<IEnumerable<MessageEntity>> SearchMessagesAsync(Guid sessionId, string searchTerm, int page = 1,
        int pageSize = 50)
    {
        return await messageRepository.SearchAsync(sessionId, searchTerm, page, pageSize);
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        await messageRepository.DeleteAsync(messageId);
        await unitOfWork.SavaEntitiesAsync();
    }

    private async Task ValidateSessionAndSenderAsync(Guid sessionId, Guid senderId)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException("会话不存在");

        var sender = await userRepository.GetByIdAsync(senderId);
        if (sender == null)
            throw new InvalidOperationException("发送者不存在");
    }

    private async Task UpdateSessionLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        await sessionRepository.UpdateLastMessageAsync(sessionId, messageId, content);
    }

    /// <summary>
    /// 创建转发消息
    /// </summary>
    /// <param name="original"></param>
    /// <param name="targetSessionId"></param>
    /// <param name="forwardedBy"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private MessageEntity CreateForwardedMessage(MessageEntity original, Guid targetSessionId, Guid forwardedBy)
    {
        return original.MessageType switch
        {
            MessageType.MessageText => MessageEntity.CreateTextMessage(targetSessionId, forwardedBy,
                original.Content ?? ""),
            MessageType.MessageImage => MessageEntity.CreateImageMessage(targetSessionId, forwardedBy,
                original.MediaUri!, original.Caption, original.ThumbnailUri),
            MessageType.MessageVideo => MessageEntity.CreateVideoMessage(targetSessionId, forwardedBy,
                original.MediaUri!, original.Duration ?? 0, original.Caption, original.ThumbnailUri),
            MessageType.MessageAudio => MessageEntity.CreateAudioMessage(targetSessionId, forwardedBy,
                original.MediaUri!, original.Duration ?? 0, original.Caption),
            MessageType.MessageFile => MessageEntity.CreateFileMessage(targetSessionId, forwardedBy, original.MediaUri!,
                original.FileName ?? "", (long)(original.FileSize ?? 0), original.MimeType ?? ""),
            MessageType.MessageLocation => MessageEntity.CreateLocationMessage(targetSessionId, forwardedBy,
                original.Latitude ?? 0, original.Longitude ?? 0, original.LocationName ?? ""),
            MessageType.MessageLink => MessageEntity.CreateLinkMessage(targetSessionId, forwardedBy,
                original.LinkUrl ?? "", original.LinkTitle, original.LinkDescription),
            MessageType.MessageExpression => MessageEntity.CreateExpressionMessage(targetSessionId, forwardedBy,
                original.ExpressionCode ?? ""),
            _ => throw new NotSupportedException($"不支持的消息类型: {original.MessageType}")
        };
    }
}