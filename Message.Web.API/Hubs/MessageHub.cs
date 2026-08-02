using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Message.Infrastructure.Services;
using Message.Web.API.Dto.Request;
using Message.Web.API.Grpc;
using Message.Web.API.Services;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Hubs;

/// <summary>
/// 消息实时通信 Hub。
/// <para>
/// 核心职责：
/// - <b>连接管理</b>：基于 Redis 连接管理器登记/注销连接、维护用户在线状态、离线消息补推；
/// - <b>会话消息</b>：私聊/群聊消息收发（<see cref="SendMessage"/>），通过 <see cref="MessageDeliveryService"/>
///   按连接<b>并行</b>推送，保证高并发下的响应速度与跨实例一致性（无重复投递）；
/// - <b>会话群组</b>：<see cref="JoinSession"/>/<see cref="LeaveSession"/> 将连接加入/移出
///   <c>session:{id}</c> 群组，支持客户端按会话订阅实时广播；
/// - <b>文件分片上传</b>（断点续传）：SignalR 通道，内部统一调用 FileDev.Web.API 的 gRPC 文件服务，
///   支持初始化、单分片上传、状态查询、合并、取消及带进度反馈的断点续传。
/// </para>
/// <para>
/// 认证策略（修复 S-02）：<c>[Authorize]</c> 强制 JWT 认证，未认证连接一律拒绝；
/// 用户身份从 <c>sub</c>/<c>NameIdentifier</c>/<c>user_guid</c> Claim 解析，不再回退信任 X-User-Id 请求头。
/// </para>
/// </summary>
[Authorize]
public class MessageHub : Hub<IMessageClient>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConnectionManager _connectionManager;
    private readonly MessageDeliveryService _deliveryService;
    private readonly IFileStorageGrpcClient _fileStorageGrpcClient;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MessageHub> _logger;
    private readonly UserStatusCacheService _userStatusCache;
    private readonly UnreadCountCacheService _unreadCountCache;
    private readonly SessionCacheService _sessionCache;
    private readonly RedisCacheService _redisCache;

    public MessageHub(
        IMessageRepository messageRepository,
        IChatSessionRepository sessionRepository,
        IUnitOfWork unitOfWork,
        IConnectionManager connectionManager,
        MessageDeliveryService deliveryService,
        IFileStorageGrpcClient fileStorageGrpcClient,
        ICurrentUserService currentUserService,
        ILogger<MessageHub> logger,
        UserStatusCacheService userStatusCache,
        UnreadCountCacheService unreadCountCache,
        SessionCacheService sessionCache,
        RedisCacheService redisCache)
    {
        _messageRepository = messageRepository;
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _connectionManager = connectionManager;
        _deliveryService = deliveryService;
        _fileStorageGrpcClient = fileStorageGrpcClient;
        _currentUserService = currentUserService;
        _logger = logger;
        _userStatusCache = userStatusCache;
        _unreadCountCache = unreadCountCache;
        _sessionCache = sessionCache;
        _redisCache = redisCache;
    }

    // ═══════════════════════════════════════════════════════
    // 连接生命周期
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 用户建立连接时：登记连接、置为在线，并补推离线期间未读消息。
    /// </summary>
    [HubMethodName("OnConnected")]
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();

        try
        {
            var connectionId = Context.ConnectionId;

            // 登记连接（Redis 连接管理器）
            await _connectionManager.AddConnectionAsync(userId, connectionId);
            // Q-05：在线状态统一经 UserStatusCacheService 写入（与 RedisConnectionManager 同一套 Key：message:user:status:{userId} + message:online:users）
            await _userStatusCache.SetUserOnlineAsync(userId);

            _logger.LogInformation("用户连接: UserId={UserId}, ConnectionId={ConnectionId}",
                userId, connectionId);

            // 补推离线期间积压的未读消息（并行推送避免顺序等待）
            var offlineMessages = await _messageRepository.GetUnreadMessagesAsync(userId);
            if (offlineMessages.Any())
            {
                var dtos = offlineMessages.Select(m => m.MapToDto()).ToArray();
                await Task.WhenAll(dtos.Select(dto => Clients.Caller.ReceiveMessage(dto)));
                _logger.LogInformation("用户 {UserId} 离线消息补推完成，共 {Count} 条", userId, dtos.Length);
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "用户连接时发生错误: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 用户断开连接时：移除连接；若无其他在线连接则标记为离线。
    /// </summary>
    /// <param name="exception">断开连接时发生的异常</param>
    [HubMethodName("OnDisconnected")]
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        try
        {
            var connectionId = Context.ConnectionId;

            await _connectionManager.RemoveConnectionAsync(userId, connectionId);

            // 仅当该用户没有任何剩余连接时才置为离线（Q-05：经 UserStatusCacheService，唯一在线状态入口）
            var hasOtherConnections = await _connectionManager.HasOtherConnectionsAsync(userId);
            if (!hasOtherConnections)
                await _userStatusCache.SetUserOfflineAsync(userId);

            _logger.LogInformation("用户断开连接: UserId={UserId}, ConnectionId={ConnectionId}", userId, connectionId);

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "用户断开连接时发生错误: UserId={UserId}", userId);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════
    // 会话消息
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 发送消息到指定会话（私聊或群聊）。
    /// <para>
    /// 流程：按消息类型调用领域服务创建消息 → 并行推送给会话全部参与者连接 →
    /// 通过群组（session:{id}）广播，保证会话与群组通道均能正确收到消息。
    /// </para>
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="request">消息请求（内容/媒体/链接等）</param>
    [HubMethodName("SendMessage")]
    public async Task SendMessage(Guid sessionId, SendMessageRequest request)
    {
        var userId = GetUserId();
        if (request is null)
            throw new HubException("消息请求不能为空");

        try
        {
            // 会话校验：存在且当前用户为参与者（防越权）
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session is null)
                throw new HubException("会话不存在");
            if (!session.IsParticipant(userId))
                throw new HubException("您不是该会话的参与者");

            var message = await CreateMessageAsync(sessionId, userId, request);
            var dto = message.MapToDto();

            // 1) 按连接并行推送（Redis 连接管理器为权威来源，跨实例无重复）
            await _deliveryService.DeliverMessageAsync(
                sessionId, dto, session.Participants, ct: Context.ConnectionAborted);

            // 2) 通过会话群组广播（支持按 session:{id} 订阅的客户端）
            if (Context.Items.TryGetValue(GroupKey(sessionId), out var joined) && joined is true)
                await Clients.Group(SessionGroupName(sessionId)).ReceiveMessage(dto);

            _logger.LogDebug("会话 {SessionId} 消息 {MessageId} 已推送，类型 {MessageType}",
                sessionId, message.MessageId, message.MessageType);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw WrapError("发送消息", ex);
        }
    }

    /// <summary>
    /// 标记消息为已读，并通知会话内其他参与者。
    /// </summary>
    /// <param name="messageId">消息ID</param>
    [HubMethodName("MarkAsRead")]
    public async Task MarkAsRead(Guid messageId)
    {
        var userId = GetUserId();

        try
        {
            await _messageRepository.MarkAsReadAsync(messageId, userId);
            await _unitOfWork.SaveEntitiesAsync(Context.ConnectionAborted);
            await Clients.Caller.MessageRead(messageId, userId);

            // Q-05：已读会改变未读数，写时失效未读计数缓存（读时回源 DB 重建）
            await _unreadCountCache.InvalidateAsync(userId, Context.ConnectionAborted);

            // 通知其他参与者该消息已被读取
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message is not null)
            {
                var session = await _sessionRepository.GetByIdAsync(message.SessionId);
                if (session is not null)
                {
                    await _deliveryService.NotifyMessageReadAsync(
                        message.SessionId, messageId, userId, session.Participants,
                        ct: Context.ConnectionAborted);
                }
            }
        }
        catch (Exception ex)
        {
            throw WrapError("标记消息已读", ex);
        }
    }

    /// <summary>
    /// 撤回指定消息，并通知会话内所有参与者。
    /// </summary>
    /// <param name="messageId">消息ID</param>
    [HubMethodName("RecallMessage")]
    public async Task RecallMessage(Guid messageId)
    {
        var userId = GetUserId();

        try
        {
            var recalled = await _messageRepository.GetByIdAsync(messageId);
            if (recalled is null)
                throw new KeyNotFoundException("消息不存在");
            recalled.Recall(userId, RecallReason.UserRequest, null);
            await _messageRepository.UpdateAsync(recalled);
            await _unitOfWork.SaveEntitiesAsync(Context.ConnectionAborted);
            await Clients.Caller.MessageRecalled(messageId);

            // Q-05：撤回改变消息状态，失效发送者/接收者的消息详情缓存（带用户维度的 Key）
            await _redisCache.RemoveAsync(MessageCacheKey(recalled.SenderId, messageId), Context.ConnectionAborted);
            if (recalled.ReceiverId is { } receiverId)
                await _redisCache.RemoveAsync(MessageCacheKey(receiverId, messageId), Context.ConnectionAborted);

            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message is not null)
            {
                var session = await _sessionRepository.GetByIdAsync(message.SessionId);
                if (session is not null)
                {
                    await _deliveryService.NotifyMessageRecalledAsync(
                        message.SessionId, messageId, session.Participants,
                        ct: Context.ConnectionAborted);
                }
            }
        }
        catch (Exception ex)
        {
            throw WrapError("撤回消息", ex);
        }
    }

    /// <summary>
    /// 发送"正在输入"指示给会话内其他参与者。
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    [HubMethodName("SendTypingIndicator")]
    public async Task SendTypingIndicator(Guid sessionId)
    {
        var userId = GetUserId();

        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session is null)
                throw new HubException("会话不存在");

            await _deliveryService.NotifyTypingAsync(
                sessionId, userId, session.Participants, ct: Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("发送输入指示", ex);
        }
    }

    // ═══════════════════════════════════════════════════════
    // 会话群组（session:{id}）
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 将当前连接加入指定会话群组（仅限会话参与者）。
    /// 加入后可接收群组广播（如系统通知、消息回执等）。
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    [HubMethodName("JoinSession")]
    public async Task JoinSession(Guid sessionId)
    {
        var userId = GetUserId();

        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session is null)
                throw new HubException("会话不存在");
            if (!session.IsParticipant(userId))
                throw new HubException("您不是该会话的参与者");

            await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroupName(sessionId));
            Context.Items[GroupKey(sessionId)] = true;

            _logger.LogDebug("连接 {ConnectionId} 加入会话群组 {GroupName}",
                Context.ConnectionId, SessionGroupName(sessionId));
        }
        catch (Exception ex)
        {
            throw WrapError("加入会话", ex);
        }
    }

    /// <summary>
    /// 将当前连接移出指定会话群组。
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    [HubMethodName("LeaveSession")]
    public async Task LeaveSession(Guid sessionId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroupName(sessionId));
            Context.Items.Remove(GroupKey(sessionId));

            _logger.LogDebug("连接 {ConnectionId} 离开会话群组 {GroupName}",
                Context.ConnectionId, SessionGroupName(sessionId));
        }
        catch (Exception ex)
        {
            throw WrapError("离开会话", ex);
        }
    }

    // ═══════════════════════════════════════════════════════
    // 文件分片上传（断点续传，SignalR 通道）
    // 内部统一调用 FileDev.Web.API 的 gRPC 文件服务
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 初始化分片上传，返回 fileKey 与分片参数；若存在已上传分片则一并返回（断点续传基础）。
    /// </summary>
    /// <param name="request">初始化分片上传请求</param>
    [HubMethodName("InitChunkUpload")]
    public async Task<ChunkUploadInitResult> InitChunkUpload(ChunkUploadInitRequest request)
    {
        var userId = GetUserId();
        if (request is null)
            throw new HubException("初始化请求不能为空");

        try
        {
            return await _fileStorageGrpcClient.InitChunkUploadAsync(
                userId, request.FileName, request.TotalSize, request.FileMd5,
                request.Description, request.IsPublic, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("初始化分片上传", ex);
        }
    }

    /// <summary>
    /// 上传单个分片。
    /// </summary>
    /// <param name="request">分片上传请求</param>
    [HubMethodName("UploadChunk")]
    public async Task<ChunkUploadResult> UploadChunk(ChunkUploadRequest request)
    {
        _ = GetUserId();
        if (request is null)
            throw new HubException("分片上传请求不能为空");

        try
        {
            return await _fileStorageGrpcClient.UploadChunkAsync(
                request.FileKey, request.ChunkIndex, request.ChunkData,
                request.ChunkMd5, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("上传分片", ex);
        }
    }

    /// <summary>
    /// 查询分片上传状态（已上传分片索引），供客户端决定续传哪些分片。
    /// </summary>
    /// <param name="request">状态查询请求</param>
    [HubMethodName("GetChunkStatus")]
    public async Task<ChunkStatusResult> GetChunkStatus(ChunkStatusRequest request)
    {
        _ = GetUserId();
        if (request is null)
            throw new HubException("状态查询请求不能为空");

        try
        {
            return await _fileStorageGrpcClient.GetChunkStatusAsync(request.FileKey, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("查询分片状态", ex);
        }
    }

    /// <summary>
    /// 合并分片，生成最终文件并返回文件元数据。
    /// </summary>
    /// <param name="request">合并分片请求</param>
    [HubMethodName("MergeChunks")]
    public async Task<MergeChunksResult> MergeChunks(ChunkMergeRequest request)
    {
        var userId = GetUserId();
        if (request is null)
            throw new HubException("合并请求不能为空");

        try
        {
            return await _fileStorageGrpcClient.MergeChunksAsync(
                request.FileKey, userId, request.FileName, request.Description,
                Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("合并分片", ex);
        }
    }

    /// <summary>
    /// 取消分片上传，清理服务端临时数据。
    /// </summary>
    /// <param name="request">取消分片上传请求</param>
    [HubMethodName("CancelChunkUpload")]
    public async Task<CancelChunkUploadResult> CancelChunkUpload(ChunkCancelRequest request)
    {
        _ = GetUserId();
        if (request is null)
            throw new HubException("取消请求不能为空");

        try
        {
            return await _fileStorageGrpcClient.CancelChunkUploadAsync(request.FileKey, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("取消分片上传", ex);
        }
    }

    /// <summary>
    /// 断点续传：一次性提交缺失分片集合，服务端仅上传未完成分片，
    /// 每个分片完成后通过 <see cref="IMessageClient.UploadProgress"/> 实时推送上传进度。
    /// </summary>
    /// <param name="request">断点续传请求（含缺失分片数据）</param>
    [HubMethodName("ResumeChunkUpload")]
    public async Task<ChunkStatusResult> ResumeChunkUpload(ChunkResumeRequest request)
    {
        _ = GetUserId();
        if (request is null)
            throw new HubException("断点续传请求不能为空");

        try
        {
            // 进度回调：每完成一个分片即向当前连接推送一次进度
            var progress = new ChunkUploadProgressReporter(this);
            return await _fileStorageGrpcClient.ResumeChunkUploadAsync(
                request.FileKey, request.TotalChunks, request.ChunkSize,
                request.Chunks, progress, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("断点续传", ex);
        }
    }

    // ═══════════════════════════════════════════════════════
    // 私有辅助
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 按消息类型分发到领域服务创建消息实体。
    /// </summary>
    private async Task<Domain.Entities.Message> CreateMessageAsync(
        Guid sessionId, Guid userId, SendMessageRequest request)
    {
        return request.MessageType switch
        {
            MessageType.MessageText => await SendTextMessageAsync(sessionId, userId,
                request.Content ?? string.Empty, Context.ConnectionAborted),

            MessageType.MessageImage => await SendImageMessageAsync(sessionId, userId,
                RequireUri(request.MediaUrl, nameof(request.MediaUrl)), request.Caption, request.ThumbnailUrl,
                Context.ConnectionAborted),

            MessageType.MessageVideo => await SendVideoMessageAsync(sessionId, userId,
                RequireUri(request.MediaUrl, nameof(request.MediaUrl)), request.Duration ?? 0, request.Caption,
                request.ThumbnailUrl, Context.ConnectionAborted),

            MessageType.MessageAudio => await SendAudioMessageAsync(sessionId, userId,
                RequireUri(request.MediaUrl, nameof(request.MediaUrl)), request.Duration ?? 0, request.Caption,
                Context.ConnectionAborted),

            MessageType.MessageFile => await SendFileMessageAsync(sessionId, userId,
                RequireUri(request.MediaUrl, nameof(request.MediaUrl)), request.FileName ?? string.Empty,
                request.FileSize ?? 0, request.MimeType ?? string.Empty, Context.ConnectionAborted),

            MessageType.MessageLocation => await SendLocationMessageAsync(sessionId, userId,
                request.Latitude ?? 0, request.Longitude ?? 0, request.LocationName ?? string.Empty,
                Context.ConnectionAborted),

            MessageType.MessageLink => await SendLinkMessageAsync(sessionId, userId,
                RequireUri(request.LinkUrl, nameof(request.LinkUrl)).AbsoluteUri, request.LinkTitle,
                request.LinkDescription, Context.ConnectionAborted),

            MessageType.MessageExpression => await SendExpressionMessageAsync(sessionId, userId,
                request.ExpressionCode ?? string.Empty, Context.ConnectionAborted),

            _ => throw new NotSupportedException($"不支持的消息类型: {request.MessageType}")
        };
    }

    private async Task<MessageEntity> SendTextMessageAsync(Guid sessionId, Guid senderId, string content,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        // S-17：内容净化 + 长度校验 + 敏感词过滤（拒绝策略，与 REST 通道 SendMessageCommand 保持一致）
        content = SafeContentSanitizer.Sanitize(content);
        if (content.Length > 2000)
            throw new HubException("消息内容不能超过2000个字符");
        RejectIfSensitive(content, "消息");

        var message = MessageEntity.CreateTextMessage(sessionId, senderId, content);
        ApplyPrivateReceiver(message, session, senderId);

        await _messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, content);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendImageMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        string? caption = null, string? thumbnailUri = null, CancellationToken cancellationToken = default)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        caption = SanitizeField(caption);
        var message = MessageEntity.CreateImageMessage(sessionId, senderId, mediaUri, caption, thumbnailUri);
        ApplyPrivateReceiver(message, session, senderId);

        await _messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[图片]");
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendVideoMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        double durationSeconds, string? caption = null, string? thumbnailUri = null,
        CancellationToken cancellationToken = default)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        caption = SanitizeField(caption);
        var message =
            MessageEntity.CreateVideoMessage(sessionId, senderId, mediaUri, durationSeconds, caption, thumbnailUri);
        ApplyPrivateReceiver(message, session, senderId);

        await _messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[视频]");
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendAudioMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri,
        double durationSeconds, string? caption = null, CancellationToken cancellationToken = default)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        caption = SanitizeField(caption);
        var message = MessageEntity.CreateAudioMessage(sessionId, senderId, mediaUri, durationSeconds, caption);
        ApplyPrivateReceiver(message, session, senderId);

        await _messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[语音]");
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendFileMessageAsync(Guid sessionId, Guid senderId, Uri mediaUri, string fileName,
        long fileSize, string mimeType, CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateFileMessage(sessionId, senderId, mediaUri, fileName, fileSize, mimeType);
        ApplyPrivateReceiver(message, session, senderId);

        await _messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[文件] {fileName}");
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    private async Task<MessageEntity> SendLocationMessageAsync(Guid sessionId, Guid senderId, double latitude,
        double longitude, string locationName, CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        locationName = SafeContentSanitizer.Sanitize(locationName);
        var message = MessageEntity.CreateLocationMessage(sessionId, senderId, latitude, longitude, locationName);
        ApplyPrivateReceiver(message, session, senderId);

        await _messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, $"[位置] {locationName}");
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

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

        await _messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, title ?? linkUrl);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return message;
    }

    /// <summary>S-17：净化可选文本字段（null/空白原样保留，避免引入空字符串语义差异）。</summary>
    private static string? SanitizeField(string? value) =>
        string.IsNullOrWhiteSpace(value) ? value : SafeContentSanitizer.Sanitize(value);

    /// <summary>S-17：敏感词拒绝策略，命中时抛出 <see cref="HubException"/>（与 REST 通道语义一致）。</summary>
    private static void RejectIfSensitive(string content, string fieldName)
    {
        var (isSensitive, matchedWord) = SensitiveWordFilter.ContainsSensitive(content);
        if (isSensitive)
            throw new HubException($"{fieldName}包含敏感内容（{matchedWord}），已拒绝发送");
    }

    private async Task<MessageEntity> SendExpressionMessageAsync(Guid sessionId, Guid senderId, string expressionCode,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAndSenderAsync(sessionId, senderId);

        var message = MessageEntity.CreateExpressionMessage(sessionId, senderId, expressionCode);
        ApplyPrivateReceiver(message, session, senderId);

        await _messageRepository.AddAsync(message);

        await UpdateSessionLastMessageAsync(sessionId, message.MessageId, "[表情]");
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

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

    private async Task<ChatSession> ValidateSessionAndSenderAsync(Guid sessionId, Guid senderId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException("会话不存在");
        return session;
    }

    private async Task UpdateSessionLastMessageAsync(Guid sessionId, Guid messageId, string? content)
    {
        await _sessionRepository.UpdateLastMessageAsync(sessionId, messageId, content);

        // Q-05：会话最后消息已变化，失效会话详情缓存
        await _sessionCache.InvalidateSessionAsync(sessionId, Context.ConnectionAborted);
    }

    /// <summary>解析并校验必填 URI 参数，缺失时抛出 <see cref="HubException"/></summary>
    private static Uri RequireUri(string? uri, string paramName)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new HubException($"{paramName} 不能为空");

        return Uri.TryCreate(uri, UriKind.Absolute, out var result)
            ? result
            : throw new HubException($"{paramName} 不是合法的绝对地址");
    }

    /// <summary>会话对应的 SignalR 群组名</summary>
    private static string SessionGroupName(Guid sessionId) => MessageDeliveryService.SessionGroupName(sessionId);

    /// <summary>消息详情缓存 Key（与 MessagesApi.GetMessageAsync 保持一致，带用户维度防止跨用户缓存命中绕过权限）</summary>
    private static string MessageCacheKey(Guid userId, Guid messageId) => $"message:msg:{userId}:{messageId}";

    /// <summary>用于记录连接是否已加入某会话群组的 Items 键</summary>
    private static string GroupKey(Guid sessionId) => $"joined:{sessionId}";

    /// <summary>
    /// 获取当前连接的用户ID（JWT 认证后的 Claim：sub / NameIdentifier / user_guid）。
    /// 兜底读取 <see cref="ICurrentUserService"/>（其数据仅由认证后的 UserContextMiddleware 从 JWT Claim 注入，与上方同源）。
    /// </summary>
    /// <returns>用户ID</returns>
    /// <exception cref="HubException">无法解析用户时抛出</exception>
    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
                          ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("user_guid")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;

        try
        {
            return _currentUserService.GetUserId();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("无法从连接 {ConnectionId} 解析用户标识", Context.ConnectionId);
            throw new HubException("无效的用户标识");
        }
    }

    /// <summary>
    /// 将异常包装为对客户端友好的 <see cref="HubException"/>，并记录错误日志。
    /// 领域层主动抛出的 <see cref="HubException"/> 原样透传。
    /// </summary>
    private HubException WrapError(string operation, Exception ex)
    {
        if (ex is HubException hubException)
            return hubException;

        var userId = Context.User?.FindFirst("sub")?.Value ?? "unknown";
        _logger.LogError(ex, "SignalR 方法 {Operation} 执行失败, UserId={UserId}, ConnectionId={ConnectionId}",
            operation, userId, Context.ConnectionId);

        return new HubException($"操作失败({operation}): {ex.Message}");
    }

    /// <summary>
    /// gRPC 断点续传进度回调：将上传进度推送给当前 SignalR 连接。
    /// <see cref="IProgress{T}.Report"/> 为同步回调，推送使用 fire-and-forget 不阻塞上传流程。
    /// </summary>
    private sealed class ChunkUploadProgressReporter(MessageHub hub) : IProgress<ChunkUploadProgress>
    {
        public void Report(ChunkUploadProgress value)
        {
            _ = hub.Clients.Caller.UploadProgress(value);
        }
    }
}
