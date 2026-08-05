using Message.Infrastructure.Services;
using Message.Web.API.Application.Commands.Messages;
using Message.Web.API.Application.Queries.Messages;
using MessageEntity = Message.Domain.Entities.Message;

using Message.Web.API.Application.Commands.Files;
using Message.Web.API.Application.Queries.Files;
using Message.Web.API.Grpc;
using Message.Web.API.Extensions;

namespace Message.Web.API.APIs;

/// <summary>
/// 消息接口（静态函数模式 + CQRS）。
/// <para>
/// 设计约定：
/// - 所有端点处理程序均为<b>静态函数</b>（不使用 Action/Lambda 创建接口）；
/// - 依赖服务通过 <c>[FromServices]</c> 特性注入，生命周期由服务注册文件统一管理；
/// - 路由参数（如 {id}）由框架按名称绑定，请求体使用 <c>[FromBody]</c>；
/// - 数据写操作通过 <see cref="INotMediator"/> 分发到命令处理程序（Commands），
///   命令仅返回操作结果（bool / 新实体 ID），不返回业务实体/DTO；
/// - 数据读操作通过 <see cref="INotMediator"/> 分发到查询处理程序（Queries），
///   查询不修改任何数据状态，仅返回只读结果。
/// </para>
/// </summary>
public static class MessagesApi
{
    /// <summary>映射消息相关端点组</summary>
    public static RouteGroupBuilder MapMessagesApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/messages")
            .RequireAuthorization();

        // 1. POST / — 发送消息
        group.MapPost("/", SendMessageAsync)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 2. GET /{messageId}/attachments — 消息附件列表
        group.MapGet("/{messageId}/attachments", GetMessageAttachmentsAsync)
            .Produces<ApiResponse<IEnumerable<FileAttachmentDto>>>(StatusCodes.Status200OK)
            .WithTags("Messages");

        // 3. GET /attachments/{attachmentId} — 附件详情（含大小/类型/下载次数/MIME）
        group.MapGet("/attachments/{attachmentId}", GetAttachmentAsync)
            .Produces<ApiResponse<FileAttachmentDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<FileAttachmentDto>>(StatusCodes.Status404NotFound)
            .WithTags("Messages");

        // 4. POST /{messageId}/attachments — 给已发送消息补附件
        group.MapPost("/{messageId}/attachments", AddAttachmentAsync)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 5. DELETE /attachments/{attachmentId} — 删除附件（软删 + 级联 FileDev 物理删除）
        group.MapDelete("/attachments/{attachmentId}", DeleteAttachmentAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 6. GET /attachments/{attachmentId}/download — 流式下载（gRPC 代理，替代 302 跳转）
        group.MapGet("/attachments/{attachmentId}/download", DownloadAttachmentAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Messages");

        // 7. GET /attachments/{attachmentId}/preview — 图片预览（gRPC 代理，支持缩放）
        group.MapGet("/attachments/{attachmentId}/preview", PreviewAttachmentAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status415UnsupportedMediaType)
            .WithTags("Messages");

        // 8. GET /{id} — 获取消息详情
        group.MapGet("/{id}", GetMessageAsync)
            .Produces<ApiResponse<MessageDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<MessageDto>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<MessageDto>>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 3. GET /sessions/{sessionId}/messages — 获取会话消息列表
        group.MapGet("/sessions/{sessionId}/messages", GetSessionMessagesAsync)
            .Produces<ApiResponse<PagedResult<MessageDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<PagedResult<MessageDto>>>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 4. DELETE /{id} — 撤回消息
        group.MapDelete("/{id}", RecallMessageAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 5. POST /{id}/forward — 转发消息
        group.MapPost("/{id}/forward", ForwardMessageAsync)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 6. PUT /{id}/read — 标记已读
        group.MapPut("/{id}/read", MarkAsReadAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 7. GET /search — 搜索消息
        group.MapGet("/search", SearchMessagesAsync)
            .Produces<ApiResponse<PagedResult<MessageDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<PagedResult<MessageDto>>>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 8. GET /unread — 获取未读消息
        group.MapGet("/unread", GetUnreadMessagesAsync)
            .Produces<ApiResponse<IEnumerable<MessageDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<IEnumerable<MessageDto>>>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        return group;
    }

    /// <summary>
    /// 发送消息（命令侧）。
    /// 构造 SendMessageCommand 并按消息类型分发到领域服务创建消息实体，仅返回新消息 ID。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">发送消息请求体</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新消息 ID</returns>
    /// <summary>
    /// 获取消息附件列表（查询侧，复用 GetMessageFilesQuery；仅消息所属会话参与者可见）。
    /// </summary>
    /// <param name="messageId">消息ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>附件 DTO 列表</returns>
    private static async Task<IResult> GetMessageAttachmentsAsync(
        Guid messageId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var files = await mediator.SendAsync(new GetMessageFilesQuery(messageId), ct);
            return Results.Ok(ApiResponse<IEnumerable<FileAttachmentDto>>.Ok(files.Select(MapAttachmentToDto)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<FileAttachmentDto>>.Error($"获取消息附件失败: {ex.Message}"),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取附件详情（查询侧，GetFileQuery；合并旧 /api/files/{id}、/exists、/size、/type、/download-count 琐碎查询）。
    /// </summary>
    /// <param name="attachmentId">附件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>附件 DTO</returns>
    private static async Task<IResult> GetAttachmentAsync(
        Guid attachmentId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var file = await mediator.SendAsync(new GetFileQuery(attachmentId), ct);
            if (file == null)
                return Results.Json(ApiResponse<FileAttachmentDto>.NotFound("附件不存在"), statusCode: 404);

            return Results.Ok(ApiResponse<FileAttachmentDto>.Ok(MapAttachmentToDto(file)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<FileAttachmentDto>.Error($"获取附件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 给已发送消息补附件（AddAttachmentCommand：FileDev 归属校验 + 消息会话成员校验）。
    /// </summary>
    /// <param name="messageId">消息ID（路由参数）</param>
    /// <param name="request">补附件请求体（fileId）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新附件 ID</returns>
    private static async Task<IResult> AddAttachmentAsync(
        Guid messageId,
        [FromBody] AddAttachmentRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var attachmentId = await mediator.SendAsync(
                new AddAttachmentCommand(messageId, request.FileId, currentUser.GetUserId()), ct);
            return Results.Ok(ApiResponse<Guid>.Created(attachmentId, "附件添加成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Json(ApiResponse<Guid>.NotFound(ex.Message), statusCode: 404);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"添加附件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 删除附件（DeleteFileCommand：软删附件记录 + 级联 FileDev gRPC 删除物理文件）。
    /// </summary>
    /// <param name="attachmentId">附件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> DeleteAttachmentAsync(
        Guid attachmentId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new DeleteFileCommand(attachmentId), ct);
            return Results.Ok(ApiResponse.Ok("附件已删除"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Json(ApiResponse.NotFound(ex.Message), statusCode: 404);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"删除附件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 流式下载附件（重新设计 v2：替代旧 302 重定向直链）。
    /// <para>
    /// 链路：附件权限校验（GetFileQuery/FileAccessGuard）→ 记录下载次数 →
    /// FileDev gRPC DownloadFile 流式转发（元数据取附件记录，二进制流不落内存）。
    /// </para>
    /// </summary>
    /// <param name="attachmentId">附件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="fileStorage">文件存储 gRPC 客户端</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文件流响应</returns>
    private static async Task<IResult> DownloadAttachmentAsync(
        Guid attachmentId,
        [FromServices] INotMediator mediator,
        [FromServices] IFileStorageGrpcClient fileStorage,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct)
    {
        try
        {
            var file = await mediator.SendAsync(new GetFileQuery(attachmentId), ct);
            if (file == null)
                return Results.Json(ApiResponse.NotFound("附件不存在"), statusCode: 404);

            // 记录下载次数（GetFileQuery 已做 FileAccessGuard 权限校验）
            await mediator.SendAsync(new RecordDownloadCommand(attachmentId), ct);

            var result = await fileStorage.DownloadFileAsync(file.FileId, currentUser.GetUserId(), ct);
            if (!result.Success)
                return Results.Json(ApiResponse.Error(result.ErrorMessage ?? "下载失败"),
                    statusCode: StatusCodes.Status502BadGateway);

            var contentType = file.MimeType ?? MimeTypeMap.FromFileName(file.FileName);
            return Results.Stream(new AsyncEnumerableStream(result.Chunks), contentType, file.FileName);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"下载附件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 图片预览（重新设计 v2：替代旧 302 跳转直链）。
    /// <para>仅支持图片类型（否则 415）；经 FileDev gRPC DownloadImage 流式转发，支持服务端缩放。</para>
    /// </summary>
    /// <param name="attachmentId">附件ID（路由参数）</param>
    /// <param name="w">缩放宽度（可选）</param>
    /// <param name="h">缩放高度（可选）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="fileStorage">文件存储 gRPC 客户端</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>图片流响应</returns>
    private static async Task<IResult> PreviewAttachmentAsync(
        Guid attachmentId,
        int? w,
        int? h,
        [FromServices] INotMediator mediator,
        [FromServices] IFileStorageGrpcClient fileStorage,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct)
    {
        try
        {
            var file = await mediator.SendAsync(new GetFileQuery(attachmentId), ct);
            if (file == null)
                return Results.Json(ApiResponse.NotFound("附件不存在"), statusCode: 404);

            if (!file.IsImage())
                return Results.Json(ApiResponse.Error("该文件不支持预览"),
                    statusCode: StatusCodes.Status415UnsupportedMediaType);

            var result = await fileStorage.DownloadImageAsync(file.FileId, currentUser.GetUserId(), w, h, ct);
            if (!result.Success)
                return Results.Json(ApiResponse.Error(result.ErrorMessage ?? "预览失败"),
                    statusCode: StatusCodes.Status502BadGateway);

            var contentType = file.MimeType ?? MimeTypeMap.FromFileName(file.FileName);
            return Results.Stream(new AsyncEnumerableStream(result.Chunks), contentType, file.FileName);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"预览附件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>文件附件实体 → DTO 映射</summary>
    private static FileAttachmentDto MapAttachmentToDto(FileAttachment file) => new()
    {
        AttachmentId = file.AttachmentId,
        FileId = file.FileId,
        MessageId = file.MessageId,
        FileName = file.FileName,
        FileType = file.FileType,
        FileSize = file.FileSize,
        FileUrl = file.FileUri.ToString(),
        MimeType = file.MimeType,
        ThumbnailUrl = file.ThumbnailUri?.ToString(),
        UploadTime = file.UploadTime,
        DownloadCount = file.DownloadCount
    };

    private static async Task<IResult> SendMessageAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromBody] SendMessageRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var messageId = await mediator.SendAsync(new SendMessageCommand(
                request.SessionId,
                userId,
                request.MessageType,
                request.Content,
                request.FileId,
                request.ThumbnailFileId,
                request.Duration,
                request.Caption,
                request.Latitude,
                request.Longitude,
                request.LocationName,
                request.LinkUrl,
                request.LinkTitle,
                request.LinkDescription,
                request.ExpressionCode), ct);

            return Results.Ok(ApiResponse<Guid>.Created(messageId, "消息发送成功"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<Guid>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 获取消息详情（查询侧，Q-05：经 RedisCacheService 缓存，TTL 30min）。
    /// 缓存 Key 带用户维度：仅缓存本人有权查看的消息，且命中时无跨用户缓存绕过权限的风险（S-05）。
    /// </summary>
    /// <param name="id">消息ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="redisCache">通用 Redis 缓存服务</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>消息 DTO</returns>
    private static async Task<IResult> GetMessageAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] RedisCacheService redisCache,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var cacheKey = MessageCacheKey(userId, id);

            var cached = await redisCache.GetAsync<MessageDto>(cacheKey, ct);
            if (cached != null)
                return Results.Ok(ApiResponse<MessageDto>.Ok(cached));

            var message = await mediator.SendAsync(new GetMessageQuery(id), ct);
            if (message == null)
                return Results.NotFound(ApiResponse<MessageDto>.NotFound("消息不存在"));

            var dto = MapToDto(message);
            await redisCache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30), ct);
            return Results.Ok(ApiResponse<MessageDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<MessageDto>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 获取会话消息列表（查询侧，分页）。
    /// </summary>
    /// <param name="sessionId">会话ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页消息列表</returns>
    private static async Task<IResult> GetSessionMessagesAsync(
        Guid sessionId,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetSessionMessagesQuery(sessionId, page, pageSize), ct);

            var result = new PagedResult<MessageDto>
            {
                Items = paged.Items.Select(MapToDto).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };

            return Results.Ok(ApiResponse<PagedResult<MessageDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<PagedResult<MessageDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 撤回消息（命令侧）。
    /// </summary>
    /// <param name="id">消息ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="reason">撤回原因（查询参数）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> RecallMessageAsync(
        Guid id,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] RecallReason reason = RecallReason.UserRequest,
        CancellationToken ct = default)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new RecallMessageCommand(id, userId, reason), ct);
            return Results.Ok(ApiResponse.Ok("消息已撤回"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 转发消息到目标会话（命令侧）。
    /// </summary>
    /// <param name="id">源消息ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">转发请求体（目标会话/转发类型/附言）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新消息 ID</returns>
    private static async Task<IResult> ForwardMessageAsync(
        Guid id,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromBody] ForwardMessageRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var newMessageId = await mediator.SendAsync(new ForwardMessageCommand(
                id, request.TargetSessionId, userId, request.ForwardType, request.Comment), ct);

            return Results.Ok(ApiResponse<Guid>.Created(newMessageId, "消息转发成功"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<Guid>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 标记消息为已读（命令侧）。
    /// </summary>
    /// <param name="id">消息ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> MarkAsReadAsync(
        Guid id,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new MarkMessageAsReadCommand(id, userId), ct);
            return Results.Ok(ApiResponse.Ok("已标记为已读"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 在会话内搜索消息（查询侧，分页）。
    /// </summary>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="sessionId">会话ID（查询参数）</param>
    /// <param name="searchTerm">搜索关键词（查询参数）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页搜索结果</returns>
    private static async Task<IResult> SearchMessagesAsync(
        [FromServices] INotMediator mediator,
        [FromQuery] Guid sessionId,
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new SearchMessagesQuery(sessionId, searchTerm, page, pageSize), ct);

            var result = new PagedResult<MessageDto>
            {
                Items = paged.Items.Select(MapToDto).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };

            return Results.Ok(ApiResponse<PagedResult<MessageDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<PagedResult<MessageDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 获取当前用户的未读消息列表（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>未读消息列表</returns>
    private static async Task<IResult> GetUnreadMessagesAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var messages = await mediator.SendAsync(new GetUnreadMessagesQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<MessageDto>>.Ok(messages.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<IEnumerable<MessageDto>>.Error(ex.Message));
        }
    }

    /// <summary>消息实体 → DTO 映射</summary>
    private static MessageDto MapToDto(MessageEntity message) => message.MapToDto();

    /// <summary>消息详情缓存 Key（带用户维度，与 MessageHub 撤回失效保持一致）</summary>
    private static string MessageCacheKey(Guid userId, Guid messageId) => $"message:msg:{userId}:{messageId}";
}
