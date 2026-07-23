namespace Message.Web.API.APIs;

public static class SessionsApi
{
    public static RouteGroupBuilder MapSessionsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions")
            .WithTags("Sessions");

        group.MapPost("/", async (
            [FromBody] CreateSessionRequest request,
            IChatSessionProvider sessionProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var session = await sessionProvider.CreateSessionAsync(
                    userId, request.SessionType, request.FriendId,
                    request.GroupId, request.SessionName, request.InitialMembers);

                return Results.Ok(ApiResponse<SessionDto>.Created(MapToDto(session), "会话创建成功"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "创建会话失败");
                return Results.Ok(ApiResponse<SessionDto>.Error("创建会话失败"));
            }
        })
        .WithSummary("创建会话")
        .WithDescription("创建私聊或群聊会话")
        .Accepts<CreateSessionRequest>("application/json")
        .Produces<ApiResponse<SessionDto>>();

        group.MapGet("/", async (
            IChatSessionProvider sessionProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var sessions = await sessionProvider.GetUserSessionsAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<SessionDto>>.Ok(sessions.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取会话列表失败");
                return Results.Ok(ApiResponse<IEnumerable<SessionDto>>.Error("获取会话列表失败"));
            }
        })
        .WithSummary("获取会话列表")
        .WithDescription("获取当前用户的所有会话")
        .Produces<ApiResponse<IEnumerable<SessionDto>>>();

        group.MapGet("/{id}", async (
            Guid id,
            IChatSessionProvider sessionProvider,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                var session = await sessionProvider.GetSessionAsync(id);
                if (session == null)
                    return Results.Ok(ApiResponse<SessionDto>.NotFound("会话不存在"));

                return Results.Ok(ApiResponse<SessionDto>.Ok(MapToDto(session)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取会话详情失败");
                return Results.Ok(ApiResponse<SessionDto>.Error("获取会话详情失败"));
            }
        })
        .WithSummary("获取会话详情")
        .WithDescription("根据会话ID获取会话详情")
        .Produces<ApiResponse<SessionDto>>();

        group.MapPut("/{id}/pin", async (
            Guid id,
            IChatSessionProvider sessionProvider,
            ILoggerFactory loggerFactory,
            [FromQuery] bool pin = true) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                if (pin)
                    await sessionProvider.PinSessionAsync(id);
                else
                    await sessionProvider.UnpinSessionAsync(id);

                return Results.Ok(ApiResponse.Ok(pin ? "会话已置顶" : "会话已取消置顶"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "置顶操作失败");
                return Results.Ok(ApiResponse.Error("置顶操作失败"));
            }
        })
        .WithSummary("置顶/取消置顶会话")
        .WithDescription("设置会话的置顶状态，pin=true置顶，pin=false取消置顶")
        .Produces<ApiResponse>();

        group.MapPut("/{id}/mute", async (
            Guid id,
            IChatSessionProvider sessionProvider,
            ILoggerFactory loggerFactory,
            [FromQuery] bool mute = true) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                if (mute)
                    await sessionProvider.MuteSessionAsync(id);
                else
                    await sessionProvider.UnmuteSessionAsync(id);

                return Results.Ok(ApiResponse.Ok(mute ? "会话已静音" : "会话已取消静音"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "静音操作失败");
                return Results.Ok(ApiResponse.Error("静音操作失败"));
            }
        })
        .WithSummary("静音/取消静音会话")
        .WithDescription("设置会话的静音状态，mute=true静音，mute=false取消静音")
        .Produces<ApiResponse>();

        group.MapDelete("/{id}", async (
            Guid id,
            IChatSessionProvider sessionProvider,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                await sessionProvider.DismissSessionAsync(id);
                return Results.Ok(ApiResponse.Ok("会话已解散"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "解散会话失败");
                return Results.Ok(ApiResponse.Error("解散会话失败"));
            }
        })
        .WithSummary("解散会话")
        .WithDescription("解散指定会话")
        .Produces<ApiResponse>();

        group.MapGet("/{id}/participants", async (
            Guid id,
            IChatSessionProvider sessionProvider,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                var session = await sessionProvider.GetSessionAsync(id);
                if (session == null)
                    return Results.Ok(ApiResponse<IEnumerable<Guid>>.NotFound("会话不存在"));

                return Results.Ok(ApiResponse<IEnumerable<Guid>>.Ok(session.Participants));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取会话参与者失败");
                return Results.Ok(ApiResponse<IEnumerable<Guid>>.Error("获取会话参与者失败"));
            }
        })
        .WithSummary("获取会话参与者")
        .WithDescription("获取指定会话的所有参与者")
        .Produces<ApiResponse<IEnumerable<Guid>>>();

        group.MapPost("/{id}/participants", async (
            Guid id,
            [FromBody] AddParticipantRequest request,
            IChatSessionProvider sessionProvider,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                await sessionProvider.AddParticipantAsync(id, request.UserId);
                return Results.Ok(ApiResponse.Ok("成员已添加"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "添加参与者失败");
                return Results.Ok(ApiResponse.Error("添加参与者失败"));
            }
        })
        .WithSummary("添加会话参与者")
        .WithDescription("向指定会话添加参与者")
        .Accepts<AddParticipantRequest>("application/json")
        .Produces<ApiResponse>();

        group.MapDelete("/{id}/participants/{userId}", async (
            Guid id,
            Guid userId,
            IChatSessionProvider sessionProvider,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                await sessionProvider.RemoveParticipantAsync(id, userId);
                return Results.Ok(ApiResponse.Ok("成员已移除"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "移除参与者失败");
                return Results.Ok(ApiResponse.Error("移除参与者失败"));
            }
        })
        .WithSummary("移除会话参与者")
        .WithDescription("从指定会话移除参与者")
        .Produces<ApiResponse>();

        group.MapGet("/pinned", async (
            IChatSessionProvider sessionProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var sessions = await sessionProvider.GetPinnedSessionsAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<SessionDto>>.Ok(sessions.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取置顶会话列表失败");
                return Results.Ok(ApiResponse<IEnumerable<SessionDto>>.Error("获取置顶会话列表失败"));
            }
        })
        .WithSummary("获取置顶会话列表")
        .WithDescription("获取当前用户的置顶会话列表")
        .Produces<ApiResponse<IEnumerable<SessionDto>>>();

        group.MapGet("/unread-count", async (
            IChatSessionProvider sessionProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var count = await sessionProvider.GetTotalUnreadCountAsync(userId);
                return Results.Ok(ApiResponse<int>.Ok(count));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取未读消息数失败");
                return Results.Ok(ApiResponse<int>.Error("获取未读消息数失败"));
            }
        })
        .WithSummary("获取未读消息数")
        .WithDescription("获取当前用户所有会话的未读消息总数")
        .Produces<ApiResponse<int>>();

        return group;
    }

    private static SessionDto MapToDto(ChatSession session) => new()
    {
        SessionId = session.SessionId,
        SessionType = session.SessionType,
        SessionName = session.SessionName,
        GroupId = session.GroupId,
        CreatorId = session.CreatorId,
        Participants = session.Participants.ToList(),
        LastMessageId = session.LastMessageId,
        LastMessageContent = session.LastMessageContent,
        LastMessageTime = session.LastMessageTime,
        CreatedTime = session.CreatedTime,
        IsPinned = session.IsPinned,
        IsMuted = session.IsMuted
    };

    public record AddParticipantRequest(Guid UserId);
}
