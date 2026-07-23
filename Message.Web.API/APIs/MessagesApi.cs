namespace Message.Web.API.APIs;

public static class MessagesApi
{
    public static RouteGroupBuilder MapMessagesApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/messages");

        // 1. POST / — 发送消息
        group.MapPost("/", async (ICurrentUserService currentUser, IMessageProvider messageProvider, [FromBody] SendMessageRequest request) =>
        {
            try
            {
                var userId = currentUser.GetUserId();
                var message = request.MessageType switch
                {
                    MessageType.MessageText => await messageProvider.SendTextMessageAsync(request.SessionId, userId,
                        request.Content ?? ""),
                    MessageType.MessageImage => await messageProvider.SendImageMessageAsync(request.SessionId, userId,
                        new Uri(request.MediaUrl!), request.Caption, request.ThumbnailUrl),
                    MessageType.MessageVideo => await messageProvider.SendVideoMessageAsync(request.SessionId, userId,
                        new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption, request.ThumbnailUrl),
                    MessageType.MessageAudio => await messageProvider.SendAudioMessageAsync(request.SessionId, userId,
                        new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption),
                    MessageType.MessageFile => await messageProvider.SendFileMessageAsync(request.SessionId, userId,
                        new Uri(request.MediaUrl!), request.FileName!, request.FileSize ?? 0, request.MimeType!),
                    MessageType.MessageLocation => await messageProvider.SendLocationMessageAsync(request.SessionId, userId,
                        request.Latitude ?? 0, request.Longitude ?? 0, request.LocationName!),
                    MessageType.MessageLink => await messageProvider.SendLinkMessageAsync(request.SessionId, userId,
                        request.LinkUrl!, request.LinkTitle, request.LinkDescription),
                    MessageType.MessageExpression => await messageProvider.SendExpressionMessageAsync(request.SessionId,
                        userId, request.ExpressionCode!),
                    _ => throw new NotSupportedException($"不支持的消息类型: {request.MessageType}")
                };

                var dto = message.MapToDto();
                return Results.Ok(ApiResponse<MessageDto>.Created(dto, "消息发送成功"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<MessageDto>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status400BadRequest)
        .WithTags("Messages");

        // 2. GET /{id} — 获取消息详情
        group.MapGet("/{id}", async (Guid id, IMessageProvider messageProvider) =>
        {
            try
            {
                var message = await messageProvider.GetMessageAsync(id);
                if (message == null)
                    return Results.NotFound(ApiResponse<MessageDto>.NotFound("消息不存在"));

                return Results.Ok(ApiResponse<MessageDto>.Ok(message.MapToDto()));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<MessageDto>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status400BadRequest)
        .WithTags("Messages");

        // 3. GET /sessions/{sessionId}/messages — 获取会话消息列表
        group.MapGet("/sessions/{sessionId}/messages", async (Guid sessionId, IMessageProvider messageProvider, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            try
            {
                var messages = await messageProvider.GetSessionMessagesAsync(sessionId, page, pageSize);
                var totalCount = await messageProvider.GetMessageCountBySessionAsync(sessionId);

                var result = new PagedResult<MessageDto>
                {
                    Items = messages.Select(m => m.MapToDto()).ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<MessageDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<PagedResult<MessageDto>>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<PagedResult<MessageDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<PagedResult<MessageDto>>>(StatusCodes.Status400BadRequest)
        .WithTags("Messages");

        // 4. DELETE /{id} — 撤回消息
        group.MapDelete("/{id}", async (Guid id, ICurrentUserService currentUser, IMessageProvider messageProvider, [FromQuery] RecallReason reason = RecallReason.UserRequest) =>
        {
            try
            {
                var userId = currentUser.GetUserId();
                await messageProvider.RecallMessageAsync(id, userId, reason);
                return Results.Ok(ApiResponse.Ok("消息已撤回"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Messages");

        // 5. POST /{id}/forward — 转发消息
        group.MapPost("/{id}/forward", async (Guid id, ICurrentUserService currentUser, IMessageProvider messageProvider, [FromBody] ForwardMessageRequest request) =>
        {
            try
            {
                var userId = currentUser.GetUserId();
                var message = await messageProvider.ForwardMessageAsync(id, request.TargetSessionId, userId,
                    request.ForwardType, request.Comment);
                return Results.Ok(ApiResponse<MessageDto>.Created(message.MapToDto(), "消息转发成功"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<MessageDto>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status400BadRequest)
        .WithTags("Messages");

        // 6. PUT /{id}/read — 标记已读
        group.MapPut("/{id}/read", async (Guid id, ICurrentUserService currentUser, IMessageProvider messageProvider) =>
        {
            try
            {
                var userId = currentUser.GetUserId();
                await messageProvider.MarkAsReadAsync(id, userId);
                return Results.Ok(ApiResponse.Ok("已标记为已读"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Messages");

        // 7. GET /search — 搜索消息
        group.MapGet("/search", async (IMessageProvider messageProvider, [FromQuery] Guid sessionId, [FromQuery] string searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            try
            {
                var messages = await messageProvider.SearchMessagesAsync(sessionId, searchTerm, page, pageSize);
                var result = new PagedResult<MessageDto>
                {
                    Items = messages.Select(m => m.MapToDto()).ToList(),
                    TotalCount = messages.Count(),
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<MessageDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<PagedResult<MessageDto>>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<PagedResult<MessageDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<PagedResult<MessageDto>>>(StatusCodes.Status400BadRequest)
        .WithTags("Messages");

        // 8. GET /unread — 获取未读消息
        group.MapGet("/unread", async (ICurrentUserService currentUser, IMessageProvider messageProvider) =>
        {
            try
            {
                var userId = currentUser.GetUserId();
                var messages = await messageProvider.GetUnreadMessagesAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<MessageDto>>.Ok(messages.Select(m => m.MapToDto())));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<IEnumerable<MessageDto>>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<IEnumerable<MessageDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<IEnumerable<MessageDto>>>(StatusCodes.Status400BadRequest)
        .WithTags("Messages");

        return group;
    }
}
