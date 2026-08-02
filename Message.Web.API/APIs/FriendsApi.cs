using Message.Web.API.Application.Commands.Friends;
using Message.Web.API.Application.Queries.Friends;

namespace Message.Web.API.APIs;

/// <summary>
/// 好友接口（静态函数模式 + CQRS）。
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
public static class FriendsApi
{
    /// <summary>映射好友相关端点组</summary>
    public static RouteGroupBuilder MapFriendsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/friends")
            .WithTags("Friends")
            .RequireAuthorization();

        // 1. POST /request — 发送好友请求
        group.MapPost("/request", SendFriendRequestAsync)
            .WithSummary("发送好友请求")
            .WithDescription("向指定用户发送好友请求")
            .Produces<ApiResponse<Guid>>()
            .Accepts<SendFriendRequestRequest>("application/json");

        // 2. PUT /request/{friendId} — 处理好友请求
        group.MapPut("/request/{friendId}", HandleFriendRequestAsync)
            .WithSummary("处理好友请求")
            .WithDescription("接受或拒绝好友请求")
            .Produces<ApiResponse>()
            .Accepts<HandleFriendRequestRequest>("application/json");

        // 3. GET / — 获取好友列表
        group.MapGet("/", GetFriendsAsync)
            .WithSummary("获取好友列表")
            .WithDescription("获取当前用户的所有好友")
            .Produces<ApiResponse<IEnumerable<FriendDto>>>();

        // 4. GET /requests — 获取收到的好友请求
        group.MapGet("/requests", GetPendingRequestsAsync)
            .WithSummary("获取收到的好友请求")
            .WithDescription("获取当前用户收到的待处理好友请求")
            .Produces<ApiResponse<IEnumerable<FriendRequestDto>>>();

        // 5. GET /sent-requests — 获取发出的好友请求
        group.MapGet("/sent-requests", GetSentRequestsAsync)
            .WithSummary("获取发出的好友请求")
            .WithDescription("获取当前用户发出的待处理好友请求")
            .Produces<ApiResponse<IEnumerable<FriendRequestDto>>>();

        // 6. DELETE /{friendId} — 删除好友
        group.MapDelete("/{friendId}", DeleteFriendAsync)
            .WithSummary("删除好友")
            .WithDescription("删除指定好友关系")
            .Produces<ApiResponse>();

        // 7. PUT /{friendId}/block — 屏蔽/取消屏蔽好友
        group.MapPut("/{friendId}/block", SetBlockStatusAsync)
            .WithSummary("屏蔽/取消屏蔽好友")
            .WithDescription("屏蔽或取消屏蔽指定好友")
            .Produces<ApiResponse>();

        // 8. PUT /{friendId}/remark — 更新好友备注
        group.MapPut("/{friendId}/remark", UpdateFriendRemarkAsync)
            .WithSummary("更新好友备注")
            .WithDescription("更新指定好友的备注名称")
            .Produces<ApiResponse>()
            .Accepts<UpdateFriendRemarkRequest>("application/json");

        // 9. PUT /{friendId}/star — 星标/取消星标好友
        group.MapPut("/{friendId}/star", SetStarStatusAsync)
            .WithSummary("星标/取消星标好友")
            .WithDescription("星标或取消星标指定好友")
            .Produces<ApiResponse>();

        // 10. PUT /{friendId}/mute — 静音/取消静音好友
        group.MapPut("/{friendId}/mute", SetMuteStatusAsync)
            .WithSummary("静音/取消静音好友")
            .WithDescription("静音或取消静音指定好友")
            .Produces<ApiResponse>();

        // 11. GET /blocked — 获取已屏蔽好友列表
        group.MapGet("/blocked", GetBlockedUsersAsync)
            .WithSummary("获取已屏蔽好友列表")
            .WithDescription("获取当前用户已屏蔽的好友列表")
            .Produces<ApiResponse<IEnumerable<FriendDto>>>();

        // 12. GET /starred — 获取星标好友列表
        group.MapGet("/starred", GetStarredFriendsAsync)
            .WithSummary("获取星标好友列表")
            .WithDescription("获取当前用户已星标的好友列表")
            .Produces<ApiResponse<IEnumerable<FriendDto>>>();

        // 13. GET /count — 获取好友数量
        group.MapGet("/count", GetFriendCountAsync)
            .WithSummary("获取好友数量")
            .WithDescription("获取当前用户的好友总数")
            .Produces<ApiResponse<int>>();

        // 14. GET /search — 搜索好友
        group.MapGet("/search", SearchFriendsAsync)
            .WithSummary("搜索好友")
            .WithDescription("按搜索词查找好友")
            .Produces<ApiResponse<IEnumerable<FriendDto>>>();

        return group;
    }

    /// <summary>
    /// 发送好友请求。
    /// 命令侧（SendFriendRequestCommand）：仅返回新友谊关系 ID。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">好友请求体（目标用户ID）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新友谊关系 ID</returns>
    private static async Task<IResult> SendFriendRequestAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromBody] SendFriendRequestRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var friendshipId = await mediator.SendAsync(
                new SendFriendRequestCommand(userId, request.FriendId), ct);

            return Results.Ok(ApiResponse<Guid>.Created(friendshipId, "好友请求已发送"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"发送好友请求失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 处理好友请求（接受或拒绝）。
    /// 命令侧（HandleFriendRequestCommand）：仅返回操作结果。
    /// </summary>
    /// <param name="friendId">发送请求的用户ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">处理请求体（是否接受）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> HandleFriendRequestAsync(
        Guid friendId,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromBody] HandleFriendRequestRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new HandleFriendRequestCommand(userId, friendId, request.Accept), ct);

            return Results.Ok(ApiResponse.Ok(request.Accept ? "好友请求已接受" : "好友请求已拒绝"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"处理好友请求失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取好友列表（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>好友 DTO 列表</returns>
    private static async Task<IResult> GetFriendsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var friends = await mediator.SendAsync(new GetFriendsQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<FriendDto>>.Error($"获取好友列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取收到的好友请求（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>待处理好友请求列表</returns>
    private static async Task<IResult> GetPendingRequestsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var requests = await mediator.SendAsync(new GetPendingRequestsQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<FriendRequestDto>>.Ok(requests.Select(MapRequestToDto)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<FriendRequestDto>>.Error($"获取好友请求失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取发出的好友请求（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>发出的待处理好友请求列表</returns>
    private static async Task<IResult> GetSentRequestsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var requests = await mediator.SendAsync(new GetSentRequestsQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<FriendRequestDto>>.Ok(requests.Select(MapRequestToDto)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<FriendRequestDto>>.Error($"获取已发送好友请求失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 删除好友关系。
    /// 命令侧（DeleteFriendCommand）：好友关系不存在时抛出 KeyNotFoundException。
    /// </summary>
    /// <param name="friendId">好友用户ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> DeleteFriendAsync(
        Guid friendId,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new DeleteFriendCommand(userId, friendId), ct);
            return Results.Ok(ApiResponse.Ok("好友已删除"));
        }
        catch (KeyNotFoundException)
        {
            return Results.Json(ApiResponse.NotFound("好友关系不存在"), statusCode: 404);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"删除好友失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 屏蔽/取消屏蔽好友。
    /// 命令侧（BlockFriendCommand）。
    /// </summary>
    /// <param name="friendId">好友用户ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="block">是否屏蔽（查询参数，默认屏蔽）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> SetBlockStatusAsync(
        Guid friendId,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] bool block = true,
        CancellationToken ct = default)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new BlockFriendCommand(userId, friendId, block), ct);

            return Results.Ok(ApiResponse.Ok(block ? "用户已屏蔽" : "用户已取消屏蔽"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"屏蔽好友操作失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 更新好友备注。
    /// 命令侧（UpdateFriendRemarkCommand）。
    /// </summary>
    /// <param name="friendId">好友用户ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">备注请求体</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> UpdateFriendRemarkAsync(
        Guid friendId,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromBody] UpdateFriendRemarkRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new UpdateFriendRemarkCommand(userId, friendId, request.Remark), ct);
            return Results.Ok(ApiResponse.Ok("备注已更新"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"更新好友备注失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 星标/取消星标好友。
    /// 命令侧（StarFriendCommand）。
    /// </summary>
    /// <param name="friendId">好友用户ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="star">是否星标（查询参数，默认星标）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> SetStarStatusAsync(
        Guid friendId,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] bool star = true,
        CancellationToken ct = default)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new StarFriendCommand(userId, friendId, star), ct);

            return Results.Ok(ApiResponse.Ok(star ? "好友已星标" : "好友已取消星标"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"星标好友操作失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 静音/取消静音好友。
    /// 命令侧（MuteFriendCommand）。
    /// </summary>
    /// <param name="friendId">好友用户ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="mute">是否静音（查询参数，默认静音）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> SetMuteStatusAsync(
        Guid friendId,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] bool mute = true,
        CancellationToken ct = default)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new MuteFriendCommand(userId, friendId, mute), ct);

            return Results.Ok(ApiResponse.Ok(mute ? "好友已静音" : "好友已取消静音"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"静音好友操作失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取已屏蔽的好友列表（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>已屏蔽用户 DTO 列表</returns>
    private static async Task<IResult> GetBlockedUsersAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var users = await mediator.SendAsync(new GetBlockedUsersQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(users.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<FriendDto>>.Error($"获取已屏蔽好友列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取星标好友列表（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>星标好友 DTO 列表</returns>
    private static async Task<IResult> GetStarredFriendsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var friends = await mediator.SendAsync(new GetStarredFriendsQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<FriendDto>>.Error($"获取星标好友列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取好友数量（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>好友总数</returns>
    private static async Task<IResult> GetFriendCountAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var count = await mediator.SendAsync(new GetFriendCountQuery(userId), ct);
            return Results.Ok(ApiResponse<int>.Ok(count));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<int>.Error($"获取好友数量失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 搜索好友（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="searchTerm">搜索关键词（查询参数）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>匹配的好友 DTO 列表</returns>
    private static async Task<IResult> SearchFriendsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] string searchTerm,
        CancellationToken ct = default)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var friends = await mediator.SendAsync(new SearchFriendsQuery(userId, searchTerm), ct);
            return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<FriendDto>>.Error($"搜索好友失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>好友关系实体 → DTO 映射</summary>
    private static FriendDto MapToDto(MessageFriends friendship) => new()
    {
        FriendshipId = friendship.FriendshipId,
        FriendId = friendship.FriendId,
        Status = friendship.Status,
        Remark = friendship.Remark,
        FriendGroupName = friendship.FriendGroupName,
        IsBlocked = friendship.IsBlocked,
        IsMuted = friendship.IsMuted,
        IsStarred = friendship.IsStarred,
        CreatedTime = friendship.CreatedTime,
        LastInteractionTime = friendship.LastInteractionTime
    };

    /// <summary>好友请求实体 → DTO 映射</summary>
    private static FriendRequestDto MapRequestToDto(MessageFriends request) => new()
    {
        FriendshipId = request.FriendshipId,
        RequesterId = request.UserId,
        Status = request.Status,
        CreatedTime = request.CreatedTime
    };
}
