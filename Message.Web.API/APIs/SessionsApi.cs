using Message.Infrastructure.Services;

namespace Message.Web.API.APIs;

/// <summary>
/// 会话接口（静态函数模式 + CQRS）。
/// <para>
/// 设计约定：
/// - 所有端点处理程序均为<b>静态函数</b>（不使用 Action/Lambda 创建接口）；
/// - 依赖服务通过 <c>[FromServices]</c> 特性注入，生命周期由服务注册文件统一管理；
/// - 数据写操作通过 <see cref="INotMediator"/> 分发到命令处理程序（Commands），
///   命令仅返回操作结果（bool / 新实体 ID），不返回业务实体/DTO；
/// - 数据读操作通过 <see cref="INotMediator"/> 分发到查询处理程序（Queries），
///   查询不修改任何数据状态，仅返回只读结果。
/// </para>
/// </summary>
public static class SessionsApi
{
    /// <summary>映射会话相关端点组</summary>
    public static RouteGroupBuilder MapSessionsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions")
            .WithTags("Sessions")
            .RequireAuthorization()
            .RequireResourcePermissions("api:session");

        // POST / — 创建会话
        group.MapPost("/", CreateSessionAsync)
            .WithSummary("创建会话")
            .WithDescription("创建私聊或群聊会话")
            .Accepts<CreateSessionRequest>("application/json")
            .Produces<ApiResponseResult<Guid>>();

        // GET / — 获取会话列表
        group.MapGet("/", GetUserSessionsAsync)
            .WithSummary("获取会话列表")
            .WithDescription("获取当前用户的所有会话")
            .Produces<ApiResponseResult<IEnumerable<SessionDto>>>();

        // GET /{id} — 获取会话详情
        group.MapGet("/{id}", GetSessionAsync)
            .WithSummary("获取会话详情")
            .WithDescription("根据会话ID获取会话详情")
            .Produces<ApiResponseResult<SessionDto>>();

        // PUT /{id}/pin — 置顶/取消置顶会话
        group.MapPut("/{id}/pin", SetPinStatusAsync)
            .WithSummary("置顶/取消置顶会话")
            .WithDescription("设置会话的置顶状态，pin=true置顶，pin=false取消置顶")
            .Produces<ApiResponseResult>();

        // PUT /{id}/mute — 静音/取消静音会话
        group.MapPut("/{id}/mute", SetMuteStatusAsync)
            .WithSummary("静音/取消静音会话")
            .WithDescription("设置会话的静音状态，mute=true静音，mute=false取消静音")
            .Produces<ApiResponseResult>();

        // DELETE /{id} — 解散会话
        group.MapDelete("/{id}", DismissSessionAsync)
            .WithSummary("解散会话")
            .WithDescription("解散指定会话")
            .Produces<ApiResponseResult>();

        // GET /{id}/participants — 获取会话参与者
        group.MapGet("/{id}/participants", GetSessionParticipantsAsync)
            .WithSummary("获取会话参与者")
            .WithDescription("获取指定会话的所有参与者")
            .Produces<ApiResponseResult<IEnumerable<Guid>>>();

        // POST /{id}/participants — 添加会话参与者
        group.MapPost("/{id}/participants", AddParticipantAsync)
            .WithSummary("添加会话参与者")
            .WithDescription("向指定会话添加参与者")
            .Accepts<AddParticipantRequest>("application/json")
            .Produces<ApiResponseResult>();

        // DELETE /{id}/participants/{userId} — 移除会话参与者
        group.MapDelete("/{id}/participants/{userId}", RemoveParticipantAsync)
            .WithSummary("移除会话参与者")
            .WithDescription("从指定会话移除参与者")
            .Produces<ApiResponseResult>();

        // GET /pinned — 获取置顶会话列表
        group.MapGet("/pinned", GetPinnedSessionsAsync)
            .WithSummary("获取置顶会话列表")
            .WithDescription("获取当前用户的置顶会话列表")
            .Produces<ApiResponseResult<IEnumerable<SessionDto>>>();

        // GET /unread-count — 获取未读消息数
        group.MapGet("/unread-count", GetTotalUnreadCountAsync)
            .WithSummary("获取未读消息数")
            .WithDescription("获取当前用户所有会话的未读消息总数")
            .Produces<ApiResponseResult<int>>();

        return group;
    }

    /// <summary>
    /// 创建会话（私聊或群聊）。
    /// 命令侧（CreateSessionCommand）：仅返回新会话 ID。
    /// </summary>
    /// <param name="request">创建会话请求体</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新会话 ID</returns>
    private static async Task<IResult> CreateSessionAsync(
        [FromBody] CreateSessionRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var sessionId = await mediator.SendAsync(
                new CreateSessionCommand(
                    userId,
                    request.SessionType,
                    request.FriendId,
                    request.GroupId,
                    request.InitialMembers),
                ct);

            return Results.Ok(ApiResponseResult<Guid>.Created(sessionId, "会话创建成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<Guid>.Error($"创建会话失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取当前用户的会话列表（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>会话 DTO 列表</returns>
    private static async Task<IResult> GetUserSessionsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] ICircleRepository circleRepository,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var sessions = await mediator.SendAsync(new GetUserSessionsQuery(userId), ct);
            return Results.Ok(ApiResponseResult<IEnumerable<SessionDto>>.Ok(await MapToDtosAsync(sessions, userId, groupRepository, circleRepository)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<IEnumerable<SessionDto>>.Error($"获取会话列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取会话详情（查询侧，Q-05：经 SessionCacheService 缓存，TTL 30min）。
    /// 缓存为跨用户共享，命中时仍须按参与者校验权限，避免缓存绕过权限（S-05）。
    /// </summary>
    /// <param name="id">会话ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="sessionCache">会话缓存服务</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>会话 DTO</returns>
    private static async Task<IResult> GetSessionAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] ICircleRepository circleRepository,
        [FromServices] SessionCacheService sessionCache,
        CancellationToken ct)
    {
        try
        {
            var callerId = currentUser.GetUserId();

            var cached = await sessionCache.GetSessionAsync<SessionDto>(id, ct);
            if (cached != null)
            {
                if (!cached.Participants.Contains(callerId))
                    return Results.Json(ApiResponseResult<SessionDto>.NotFound("会话不存在"), statusCode: 404);

                return Results.Ok(ApiResponseResult<SessionDto>.Ok(cached));
            }

            var session = await mediator.SendAsync(new GetSessionQuery(id), ct);
            if (session == null)
                return Results.Json(ApiResponseResult<SessionDto>.NotFound("会话不存在"), statusCode: 404);

            var dto = await MapToDtoAsync(session, callerId, groupRepository, circleRepository);
            await sessionCache.CacheSessionAsync(id, dto, ct);
            return Results.Ok(ApiResponseResult<SessionDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<SessionDto>.Error($"获取会话详情失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 置顶/取消置顶会话（命令侧）。
    /// </summary>
    /// <param name="id">会话ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="pin">是否置顶（查询参数，默认置顶）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> SetPinStatusAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        [FromQuery] bool pin = true,
        CancellationToken ct = default)
    {
        try
        {
            await mediator.SendAsync(new SetSessionPinCommand(id, pin), ct);
            return Results.Ok(ApiResponseResult.Ok(pin ? "会话已置顶" : "会话已取消置顶"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"置顶操作失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 静音/取消静音会话（命令侧）。
    /// </summary>
    /// <param name="id">会话ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="mute">是否静音（查询参数，默认静音）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> SetMuteStatusAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        [FromQuery] bool mute = true,
        CancellationToken ct = default)
    {
        try
        {
            await mediator.SendAsync(new SetSessionMuteCommand(id, mute), ct);
            return Results.Ok(ApiResponseResult.Ok(mute ? "会话已静音" : "会话已取消静音"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"静音操作失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 解散会话（命令侧）。
    /// </summary>
    /// <param name="id">会话ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> DismissSessionAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new DismissSessionCommand(id), ct);
            return Results.Ok(ApiResponseResult.Ok("会话已解散"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"解散会话失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取会话参与者列表（查询侧）。
    /// </summary>
    /// <param name="id">会话ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>参与者用户ID列表</returns>
    private static async Task<IResult> GetSessionParticipantsAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var session = await mediator.SendAsync(new GetSessionParticipantsQuery(id), ct);
            if (session == null)
                return Results.Json(ApiResponseResult<IEnumerable<Guid>>.NotFound("会话不存在"), statusCode: 404);

            return Results.Ok(ApiResponseResult<IEnumerable<Guid>>.Ok(session.Participants));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<IEnumerable<Guid>>.Error($"获取会话参与者失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 向会话添加参与者（命令侧）。
    /// </summary>
    /// <param name="id">会话ID（路由参数）</param>
    /// <param name="request">添加参与者请求体</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> AddParticipantAsync(
        Guid id,
        [FromBody] AddParticipantRequest request,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new AddSessionParticipantCommand(id, request.UserId), ct);
            return Results.Ok(ApiResponseResult.Ok("成员已添加"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"添加参与者失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 从会话移除参与者（命令侧）。
    /// </summary>
    /// <param name="id">会话ID（路由参数）</param>
    /// <param name="userId">要移除的用户ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> RemoveParticipantAsync(
        Guid id,
        Guid userId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new RemoveSessionParticipantCommand(id, userId), ct);
            return Results.Ok(ApiResponseResult.Ok("成员已移除"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"移除参与者失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取当前用户的置顶会话列表（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>置顶会话 DTO 列表</returns>
    private static async Task<IResult> GetPinnedSessionsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] ICircleRepository circleRepository,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var sessions = await mediator.SendAsync(new GetPinnedSessionsQuery(userId), ct);
            return Results.Ok(ApiResponseResult<IEnumerable<SessionDto>>.Ok(await MapToDtosAsync(sessions, userId, groupRepository, circleRepository)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<IEnumerable<SessionDto>>.Error($"获取置顶会话列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取当前用户所有会话的未读消息总数（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>未读消息总数</returns>
    private static async Task<IResult> GetTotalUnreadCountAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var count = await mediator.SendAsync(new GetTotalUnreadCountQuery(userId), ct);
            return Results.Ok(ApiResponseResult<int>.Ok(count));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<int>.Error($"获取未读消息数失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 会话实体 → DTO 映射（查询期投影）。
    /// <para>方案A：群聊会话名称不再存储于 ChatSession，改为按 GroupId 从群组查询（GroupName）、
    /// 社区频道会话按 CircleId 从社区查询（Name），消除与 Group/Circle 实体的功能重叠；私聊会话无名称，置空。</para>
    /// <para>IsPinned/IsMuted 按成员维度，取决于请求用户。</para>
    /// </summary>
    private static SessionDto MapToDto(ChatSession session, Guid userId, string? sessionName = null)
    {
        var state = session.MemberStates.TryGetValue(userId, out var st) ? st : null;
        return new SessionDto
        {
            SessionId = session.SessionId,
            SessionType = session.SessionType,
            SessionName = sessionName,
            GroupId = session.GroupId,
            CircleId = session.CircleId,
            CreatorId = session.CreatorId,
            Participants = session.Participants.ToList(),
            LastMessageId = session.LastMessageId,
            LastMessageContent = session.LastMessageContent,
            LastMessageTime = session.LastMessageTime,
            CreatedTime = session.CreatedTime,
            IsPinned = state?.IsPinned ?? false,
            IsMuted = state?.IsMuted ?? false
        };
    }

    /// <summary>单个会话按 GroupId/CircleId 投影名称后映射为 DTO。</summary>
    private static async Task<SessionDto> MapToDtoAsync(
        ChatSession session, Guid userId, IGroupRepository groupRepository, ICircleRepository circleRepository)
    {
        return MapToDto(session, userId, await ResolveSessionNameAsync(session, groupRepository, circleRepository));
    }

    /// <summary>批量会话映射为 DTO（一次性解析全部群名/社区名，避免 N+1 查询）。</summary>
    private static async Task<IEnumerable<SessionDto>> MapToDtosAsync(
        IEnumerable<ChatSession> sessions, Guid userId, IGroupRepository groupRepository, ICircleRepository circleRepository)
    {
        var list = sessions.ToList();
        var groupIds = list
            .Where(s => s.SessionType == SessionType.Group && s.GroupId.HasValue)
            .Select(s => s.GroupId!.Value)
            .Distinct()
            .ToList();

        var groupNames = new Dictionary<Guid, string>();
        foreach (var groupId in groupIds)
        {
            var group = await groupRepository.GetByIdAsync(groupId);
            if (group != null)
                groupNames[groupId] = group.GroupName;
        }

        var circleIds = list
            .Where(s => s.SessionType == SessionType.Channel && s.CircleId.HasValue)
            .Select(s => s.CircleId!.Value)
            .Distinct()
            .ToList();

        var circleNames = new Dictionary<Guid, string>();
        foreach (var circleId in circleIds)
        {
            var circle = await circleRepository.GetByIdAsync(circleId);
            if (circle != null)
                circleNames[circleId] = circle.Name;
        }

        return list.Select(s => MapToDto(s, userId,
            ResolveName(s, groupNames, circleNames)));
    }

    /// <summary>解析会话名称（群聊取群名，社区频道取社区名，私聊返回 null）。</summary>
    private static async Task<string?> ResolveSessionNameAsync(
        ChatSession session, IGroupRepository groupRepository, ICircleRepository circleRepository)
    {
        if (session.SessionType == SessionType.Group && session.GroupId.HasValue)
        {
            var group = await groupRepository.GetByIdAsync(session.GroupId.Value);
            return group?.GroupName;
        }
        if (session.SessionType == SessionType.Channel && session.CircleId.HasValue)
        {
            var circle = await circleRepository.GetByIdAsync(session.CircleId.Value);
            return circle?.Name;
        }
        return null;
    }

    /// <summary>从已解析的群名/社区名字典中取出会话名称（群聊/社区频道专用）。</summary>
    private static string? ResolveName(
        ChatSession session, IReadOnlyDictionary<Guid, string> groupNames, IReadOnlyDictionary<Guid, string> circleNames)
    {
        if (session.SessionType == SessionType.Group && session.GroupId.HasValue &&
            groupNames.TryGetValue(session.GroupId.Value, out var groupName))
            return groupName;
        if (session.SessionType == SessionType.Channel && session.CircleId.HasValue &&
            circleNames.TryGetValue(session.CircleId.Value, out var circleName))
            return circleName;
        return null;
    }

    /// <summary>添加会话参与者请求</summary>
    public record AddParticipantRequest(Guid UserId);
}
