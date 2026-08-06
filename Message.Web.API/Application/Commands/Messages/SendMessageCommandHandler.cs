using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Commands.Messages;
/// <summary>
/// 发送消息命令处理程序。
/// <para>按消息类型分发到领域服务创建消息实体（文本/图片/视频/音频/文件/位置/链接/表情）。</para>
/// </summary>
public class SendMessageCommandHandler(
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    IFileAttachmentRepository fileRepository,
    IFileStorageGrpcClient fileStorage,
    ILogger<SendMessageCommandHandler> logger,
    UnreadCountCacheService unreadCountCache,
    SessionCacheService sessionCache) : IRequestHandler<SendMessageCommand, Guid>
{
    public async Task<Guid> Handler(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var message = command.MessageType switch
        {
            MessageType.MessageText => await SendTextMessageAsync(command.SessionId, command.SenderId,
                command.Content ?? "", cancellationToken),
            MessageType.MessageImage or MessageType.MessageVideo or MessageType.MessageAudio or MessageType.MessageFile
                => await SendMediaMessageAsync(command, cancellationToken),
            MessageType.MessageLocation => await SendLocationMessageAsync(command.SessionId, command.SenderId,
                command.Latitude ?? 0, command.Longitude ?? 0, command.LocationName!, cancellationToken),
            MessageType.MessageLink => await SendLinkMessageAsync(command.SessionId, command.SenderId,
                command.LinkUrl!, command.LinkTitle, command.LinkDescription, cancellationToken),
            MessageType.MessageExpression => await SendExpressionMessageAsync(command.SessionId,
                command.SenderId, command.ExpressionCode!, cancellationToken),
            _ => throw new NotSupportedException($"不支持的消息类型: {command.MessageType}")
        };

        // Q-05：未读计数缓存采用"写时失效"策略——私聊消息会改变接收者未读数，直接删除缓存键，
        // 读路径 miss 时回源 DB 重建（群聊消息 ReceiverId 为空，不参与未读计数，无需失效）
        if (message.ReceiverId is { } receiverId)
            await unreadCountCache.InvalidateAsync(receiverId, cancellationToken);

        // Q-05：会话最后消息已变化，失效会话详情缓存
        await sessionCache.InvalidateSessionAsync(command.SessionId, cancellationToken);

        logger.LogInformation("发送消息成功：{MessageId}，类型={MessageType}，会话={SessionId}",
            message.MessageId, message.MessageType, message.SessionId);
        return message.MessageId;
    }

    /// <summary>
    /// 发送媒体消息（图片/视频/音频/文件）——统一链路：
    /// 1. FileDev gRPC GetFileInfo 校验文件存在且归属当前用户（防 IDOR：不能引用他人文件）；
    /// 2. 服务端用 FileDev 元数据填充 Message（MediaUri/FileName/FileSize/MimeType），客户端不再提供；
    /// 3. 同事务创建 FileAttachment 附件记录（消息附件事实源）。
    /// </summary>
    private async Task<MessageEntity> SendMediaMessageAsync(SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (command.FileId is not { } fileId)
            throw new ArgumentException("媒体消息必须携带 FileId（先调用上传接口获取）");

        var session = await ValidateSessionAndSenderAsync(command.SessionId, command.SenderId);

        // 1. 主文件校验（存在性 + 归属）
        var info = await fileStorage.GetFileInfoAsync(fileId, cancellationToken);
        if (!info.Success || info.FileId == null)
            throw new KeyNotFoundException(info.ErrorMessage ?? "文件不存在");
        if (info.UserId != command.SenderId)
            throw new UnauthorizedAccessException("无权使用该文件");
        var fileUri = info.FileUri ?? throw new InvalidOperationException("文件URI缺失");

        // 2. 缩略图（可选）：同样校验存在性与归属，失败不阻断主流程
        Uri? thumbnailUri = null;
        if (command.ThumbnailFileId is { } thumbnailFileId)
        {
            var thumbInfo = await fileStorage.GetFileInfoAsync(thumbnailFileId, cancellationToken);
            if (thumbInfo is { Success: true, UserId: not null }
                && thumbInfo.UserId == command.SenderId && thumbInfo.FileUri != null)
                thumbnailUri = thumbInfo.FileUri;
        }

        var mimeType = MimeTypeMap.FromFileName(info.FileName);
        var caption = SanitizeField(command.Caption);

        // 3. 创建消息实体（服务端统一填充媒体元数据）
        var message = command.MessageType switch
        {
            MessageType.MessageImage => MessageEntity.CreateImageMessage(command.SessionId, command.SenderId,
                fileUri, caption, thumbnailUri?.ToString()),
            MessageType.MessageVideo => MessageEntity.CreateVideoMessage(command.SessionId, command.SenderId,
                fileUri, command.Duration ?? 0, caption, thumbnailUri?.ToString()),
            MessageType.MessageAudio => MessageEntity.CreateAudioMessage(command.SessionId, command.SenderId,
                fileUri, command.Duration ?? 0, caption),
            MessageType.MessageFile => MessageEntity.CreateFileMessage(command.SessionId, command.SenderId,
                fileUri, info.FileName, info.FileSize, mimeType),
            _ => throw new NotSupportedException($"不支持的消息类型: {command.MessageType}")
        };
        ApplyPrivateReceiver(message, session, command.SenderId);

        // 4. 同事务创建附件记录（FileType/MimeType 均为 MIME 字符串，供 IsImage() 等分类判断）
        var attachment = new FileAttachment(
            message.MessageId, fileId, info.FileName, mimeType, info.FileSize, fileUri, mimeType, thumbnailUri);
        message.AddAttachment(attachment);
        await fileRepository.AddAsync(attachment);

        await messageRepository.AddAsync(message);
        await UpdateSessionLastMessageAsync(command.SessionId, message.MessageId,
            MediaMessageSummary(command.MessageType, info.FileName));
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("发送媒体消息成功：{MessageId}，类型={MessageType}，FileId={FileId}，附件={AttachmentId}",
            message.MessageId, message.MessageType, fileId, attachment.AttachmentId);
        return message;
    }

    /// <summary>媒体消息的会话最后消息摘要（[图片]/[视频]/[语音]/[文件] 文件名）</summary>
    private static string MediaMessageSummary(MessageType type, string fileName) => type switch
    {
        MessageType.MessageImage => "[图片]",
        MessageType.MessageVideo => "[视频]",
        MessageType.MessageAudio => "[语音]",
        _ => $"[文件] {fileName}"
    };

    private async Task<MessageEntity> SendTextMessageAsync(Guid sessionId, Guid senderId, string content,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        // S-17：内容净化 + 长度校验 + 敏感词过滤（拒绝策略）
        content = SafeContentSanitizer.Sanitize(content);
        if (content.Length > 2000)
            throw new ArgumentException("消息内容不能超过2000个字符");
        RejectIfSensitive(content, "消息");

        var message = MessageEntity.CreateTextMessage(sessionId, senderId, content);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, content);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendLocationMessageAsync(Guid sessionId, Guid senderId, double latitude,
        double longitude, string locationName, CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateLocationMessage(sessionId, senderId, latitude, longitude, locationName);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[位置] {locationName}");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendLinkMessageAsync(Guid sessionId, Guid senderId, string linkUrl,
        string? title = null, string? description = null, CancellationToken cancellationToken = default)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        title = SanitizeField(title);
        description = SanitizeField(description);
        var message = MessageEntity.CreateLinkMessage(sessionId, senderId, linkUrl, title, description);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, title ?? linkUrl);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendExpressionMessageAsync(Guid sessionId, Guid senderId, string expressionCode,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateExpressionMessage(sessionId, senderId, expressionCode);
        ApplyPrivateReceiver(message, session, senderId);

        await messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[表情]");
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    /// <summary>
    /// F-04：私聊会话设置消息接收者（对端用户），激活离线消息/未读查询（GetUnreadMessagesAsync 依赖 ReceiverId）；群聊保持 null。
    /// </summary>
    private static void ApplyPrivateReceiver(MessageEntity message, ChatSession session, Guid senderId)
    {
        if (session.SessionType != SessionType.Private)
            return;
        var receiver = session.Participants.FirstOrDefault(p => p != senderId);
        if (receiver != Guid.Empty)
            message.SetReceiver(receiver);
    }

    /// <summary>S-17：净化可选文本字段（null/空白原样保留，避免引入空字符串语义差异）。</summary>
    private static string? SanitizeField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var sanitized = SafeContentSanitizer.Sanitize(value);
        // P-05：附加文本字段（媒体描述/链接标题/链接描述）长度上限
        if (sanitized.Length > 2000)
            throw new ArgumentException("消息附加文本不能超过2000个字符");
        return sanitized;
    }

    /// <summary>S-17：敏感词拒绝策略，命中时抛出业务异常（API 层映射为 400）。</summary>
    private static void RejectIfSensitive(string content, string fieldName)
    {
        var (isSensitive, matchedWord) = SensitiveWordFilter.ContainsSensitive(content);
        if (isSensitive)
            throw new InvalidOperationException($"{fieldName}包含敏感内容（{matchedWord}），已拒绝发送");
    }

    private async Task<ChatSession> ValidateSessionAndSenderAsync(Guid sessionId, Guid senderId)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException("会话不存在");
        // 修复 S-05：与 MessageHub 行为一致，非参与者禁止向会话发送消息（REST 与 Hub 对齐）
        if (!session.IsParticipant(senderId))
            throw new InvalidOperationException("您不是该会话的参与者");
        return session;
    }

    private async Task UpdateSessionLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        await sessionRepository.UpdateLastMessageAsync(sessionId, messageId, content);
    }
}
