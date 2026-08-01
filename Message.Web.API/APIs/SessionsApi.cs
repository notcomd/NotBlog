using Message.Web.API.Application.Commands.Sessions;
using Message.Web.API.Application.Queries.Sessions;

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
            .WithTags("Sessions");

        // POST / — 创建会话
        group.MapPost("/", CreateSessionAsync)
            .WithSummary("创建会话")
            .WithDescription("创建私聊或群聊会话")
            .Accepts<CreateSessionRequest>("application/json")
            .Produces<ApiResponse<Guid>>();

        // GET / — 获取会话列表
        group.MapGet("/", GetUserSessionsAsync)
            .WithSummary("获取会话列表")
            .WithDescription("获取当前用户的所有会话")
            .Produces<ApiResponse<IEnumerable<SessionDto>>>();

        // GET /{id} — 获取会话详情
        group.MapGet("/{id}", GetSessionAsync)
            .WithSummary("获取会话详情")
            .WithDescription("根据会话ID获取会话详情")
            .Produces<ApiResponse<SessionDto>>();

        // PUT /{id}/pin — 置顶/取消置顶会话
        group.MapPut("/{id}/pin", SetPinStatusAsync)
            .WithSummary("置顶/取消置顶会话")
            .WithDescription("设置会话的置顶状态，pin=true置顶，pin=false取消置顶")
            .Produces<ApiResponse>();

        // PUT /{id}/mute — 静音/取消静音会话
        group.MapPut("/{id}/mute", SetMuteStatusAsync)
            .WithSummary("静音/取消静音会话")
            .WithDescription("设置会话的静音状态，mute=true静音，mute=false取消静音")
            .Produces<ApiResponse>();

        // DELETE /{id} — 解散会话
        group.MapDelete("/{id}", DismissSessionAsync)
            .WithSummary("解散会话")
            .WithDescription("解散指定会话")
            .Produces<ApiResponse>();

        // GET /{id}/participants — 获取会话参与者
        group.MapGet("/{id}/participants", GetSessionParticipantsAsync)
            .WithSummary("获取会话参与者")
            .WithDescription("获取指定会话的所有参与者")
            .Produces<ApiResponse<IEnumerable<Guid>>>();

        // POST /{id}/participants — 添加会话参与者
        group.MapPost("/{id}/participants", AddParticipantAsync)
            .WithSummary("添加会话参与者")
            .WithDescription("向指定会话添加参与者")
            .Accepts<AddParticipantRequest>("application/json")
            .Produces<ApiResponse>();

        // DELETE /{id}/participants/{userId} — 移除会话参与者
        group.MapDelete("/{id}/participants/{userId}", RemoveParticipantAsync)
            .WithSummary("移除会话参与者")
            .WithDescription("从指定会话移除参与者")
            .Produces<ApiResponse>();

        // GET /pinned — 获取置顶会话列表
        group.MapGet("/pinned", GetPinnedSessionsAsync)
            .WithSummary("获取置顶会话列表")
            .WithDescription("获取当前用户的置顶会话列表")
            .Produces<ApiResponse<IEnumerable<SessionDto>>>();

        // GET /unread-count — 获取未读消息数
        group.MapGet("/unread-count", GetTotalUnreadCountAsync)
            .WithSummary("获取未读消息数")
            .WithDescription("获取当前用户所有会话的未读消息总数")
            .Produces<ApiResponse<int>>();

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
                    request.SessionName,
                    request.InitialMembers),
                ct);

            return Results.Ok(ApiResponse<Guid>.Created(sessionId, "会话创建成功"));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<Guid>.Error($"创建会话失败: {ex.Message}"));
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
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var sessions = await mediator.SendAsync(new GetUserSessionsQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<SessionDto>>.Ok(sessions.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<IEnumerable<SessionDto>>.Error($"获取会话列表失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取会话详情（查询侧）。
    /// </summary>
    /// <param name="id">会话ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>会话 DTO</returns>
    private static async Task<IResult> GetSessionAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var session = await mediator.SendAsync(new GetSessionQuery(id), ct);
            if (session == null)
                return Results.Ok(ApiResponse<SessionDto>.NotFound("会话不存在"));

            return Results.Ok(ApiResponse<SessionDto>.Ok(MapToDto(session)));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<SessionDto>.Error($"获取会话详情失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse.Ok(pin ? "会话已置顶" : "会话已取消置顶"));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse.Error($"置顶操作失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse.Ok(mute ? "会话已静音" : "会话已取消静音"));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse.Error($"静音操作失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse.Ok("会话已解散"));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse.Error($"解散会话失败: {ex.Message}"));
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
                return Results.Ok(ApiResponse<IEnumerable<Guid>>.NotFound("会话不存在"));

            return Results.Ok(ApiResponse<IEnumerable<Guid>>.Ok(session.Participants));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<IEnumerable<Guid>>.Error($"获取会话参与者失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse.Ok("成员已添加"));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse.Error($"添加参与者失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse.Ok("成员已移除"));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse.Error($"移除参与者失败: {ex.Message}"));
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
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var sessions = await mediator.SendAsync(new GetPinnedSessionsQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<SessionDto>>.Ok(sessions.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<IEnumerable<SessionDto>>.Error($"获取置顶会话列表失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse<int>.Ok(count));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<int>.Error($"获取未读消息数失败: {ex.Message}"));
        }
    }

    /// <summary>会话实体 → DTO 映射</summary>
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

    /// <summary>添加会话参与者请求</summary>
    public record AddParticipantRequest(Guid UserId);
}
