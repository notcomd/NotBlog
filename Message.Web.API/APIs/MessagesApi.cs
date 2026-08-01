using Message.Web.API.Application.Commands.Messages;
using Message.Web.API.Application.Queries.Messages;
using MessageEntity = Message.Domain.Entities.Message;

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
        var group = app.MapGroup("/api/messages");

        // 1. POST / — 发送消息
        group.MapPost("/", SendMessageAsync)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status400BadRequest)
            .WithTags("Messages");

        // 2. GET /{id} — 获取消息详情
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
                request.MediaUrl is null ? null : new Uri(request.MediaUrl),
                request.ThumbnailUrl,
                request.FileName,
                request.FileSize,
                request.MimeType,
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
    /// 获取消息详情（查询侧）。
    /// </summary>
    /// <param name="id">消息ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>消息 DTO</returns>
    private static async Task<IResult> GetMessageAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var message = await mediator.SendAsync(new GetMessageQuery(id), ct);
            if (message == null)
                return Results.NotFound(ApiResponse<MessageDto>.NotFound("消息不存在"));

            return Results.Ok(ApiResponse<MessageDto>.Ok(MapToDto(message)));
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
}
