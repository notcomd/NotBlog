namespace Message.Web.API.APIs;

public static class FriendsApi
{
    public static RouteGroupBuilder MapFriendsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/friends")
            .WithTags("Friends");

        // Create logger once for all handlers
        var loggerFactory = app.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("FriendsApi");

        // 1. POST /request — 发送好友请求
        group.MapPost("/request", async (
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider,
            [FromBody] SendFriendRequestRequest request) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var friendship = await friendProvider.SendFriendRequestAsync(userId, request.FriendId);
                return Results.Ok(ApiResponse<FriendDto>.Created(MapToDto(friendship), "好友请求已发送"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "发送好友请求失败");
                return Results.Ok(ApiResponse<FriendDto>.Error("发送好友请求失败"));
            }
        })
        .WithSummary("发送好友请求")
        .WithDescription("向指定用户发送好友请求")
        .Produces<ApiResponse<FriendDto>>()
        .Accepts<SendFriendRequestRequest>("application/json");

        // 2. PUT /request/{friendId} — 处理好友请求
        group.MapPut("/request/{friendId}", async (
            Guid friendId,
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider,
            [FromBody] HandleFriendRequestRequest request) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                if (request.Accept)
                    await friendProvider.AcceptFriendRequestAsync(userId, friendId);
                else
                    await friendProvider.RejectFriendRequestAsync(userId, friendId);

                return Results.Ok(ApiResponse.Ok(request.Accept ? "好友请求已接受" : "好友请求已拒绝"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理好友请求失败");
                return Results.Ok(ApiResponse.Error("处理好友请求失败"));
            }
        })
        .WithSummary("处理好友请求")
        .WithDescription("接受或拒绝好友请求")
        .Produces<ApiResponse>()
        .Accepts<HandleFriendRequestRequest>("application/json");

        // 3. GET / — 获取好友列表
        group.MapGet("/", async (
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var friends = await friendProvider.GetFriendsAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取好友列表失败");
                return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Error("获取好友列表失败"));
            }
        })
        .WithSummary("获取好友列表")
        .WithDescription("获取当前用户的所有好友")
        .Produces<ApiResponse<IEnumerable<FriendDto>>>();

        // 4. GET /requests — 获取收到的好友请求
        group.MapGet("/requests", async (
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var requests = await friendProvider.GetPendingRequestsAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<FriendRequestDto>>.Ok(requests.Select(MapRequestToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取好友请求失败");
                return Results.Ok(ApiResponse<IEnumerable<FriendRequestDto>>.Error("获取好友请求失败"));
            }
        })
        .WithSummary("获取收到的好友请求")
        .WithDescription("获取当前用户收到的待处理好友请求")
        .Produces<ApiResponse<IEnumerable<FriendRequestDto>>>();

        // 5. GET /sent-requests — 获取发出的好友请求
        group.MapGet("/sent-requests", async (
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var requests = await friendProvider.GetSentRequestsAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<FriendRequestDto>>.Ok(requests.Select(MapRequestToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取已发送好友请求失败");
                return Results.Ok(ApiResponse<IEnumerable<FriendRequestDto>>.Error("获取已发送好友请求失败"));
            }
        })
        .WithSummary("获取发出的好友请求")
        .WithDescription("获取当前用户发出的待处理好友请求")
        .Produces<ApiResponse<IEnumerable<FriendRequestDto>>>();

        // 6. DELETE /{friendId} — 删除好友
        group.MapDelete("/{friendId}", async (
            Guid friendId,
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var friendship = await friendProvider.GetFriendshipAsync(userId, friendId);
                if (friendship == null)
                    return Results.Json(ApiResponse.NotFound("好友关系不存在"), statusCode: 404);

                await friendProvider.DeleteFriendshipAsync(friendship.FriendshipId);
                return Results.Ok(ApiResponse.Ok("好友已删除"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "删除好友失败");
                return Results.Ok(ApiResponse.Error("删除好友失败"));
            }
        })
        .WithSummary("删除好友")
        .WithDescription("删除指定好友关系")
        .Produces<ApiResponse>();

        // 7. PUT /{friendId}/block — 屏蔽/取消屏蔽好友
        group.MapPut("/{friendId}/block", async (
            Guid friendId,
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider,
                        [FromQuery] bool block = true) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                if (block)
                    await friendProvider.BlockUserAsync(userId, friendId);
                else
                    await friendProvider.UnblockUserAsync(userId, friendId);

                return Results.Ok(ApiResponse.Ok(block ? "用户已屏蔽" : "用户已取消屏蔽"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "屏蔽好友操作失败");
                return Results.Ok(ApiResponse.Error("屏蔽好友操作失败"));
            }
        })
        .WithSummary("屏蔽/取消屏蔽好友")
        .WithDescription("屏蔽或取消屏蔽指定好友")
        .Produces<ApiResponse>();

        // 8. PUT /{friendId}/remark — 更新好友备注
        group.MapPut("/{friendId}/remark", async (
            Guid friendId,
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider,
                        [FromBody] UpdateFriendRemarkRequest request) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                await friendProvider.UpdateFriendRemarkAsync(userId, friendId, request.Remark);
                return Results.Ok(ApiResponse.Ok("备注已更新"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "更新好友备注失败");
                return Results.Ok(ApiResponse.Error("更新好友备注失败"));
            }
        })
        .WithSummary("更新好友备注")
        .WithDescription("更新指定好友的备注名称")
        .Produces<ApiResponse>()
        .Accepts<UpdateFriendRemarkRequest>("application/json");

        // 9. PUT /{friendId}/star — 星标/取消星标好友
        group.MapPut("/{friendId}/star", async (
            Guid friendId,
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider,
                        [FromQuery] bool star = true) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                if (star)
                    await friendProvider.StarFriendAsync(userId, friendId);
                else
                    await friendProvider.UnstarFriendAsync(userId, friendId);

                return Results.Ok(ApiResponse.Ok(star ? "好友已星标" : "好友已取消星标"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "星标好友操作失败");
                return Results.Ok(ApiResponse.Error("星标好友操作失败"));
            }
        })
        .WithSummary("星标/取消星标好友")
        .WithDescription("星标或取消星标指定好友")
        .Produces<ApiResponse>();

        // 10. PUT /{friendId}/mute — 静音/取消静音好友
        group.MapPut("/{friendId}/mute", async (
            Guid friendId,
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider,
                        [FromQuery] bool mute = true) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                if (mute)
                    await friendProvider.MuteFriendAsync(userId, friendId);
                else
                    await friendProvider.UnmuteFriendAsync(userId, friendId);

                return Results.Ok(ApiResponse.Ok(mute ? "好友已静音" : "好友已取消静音"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "静音好友操作失败");
                return Results.Ok(ApiResponse.Error("静音好友操作失败"));
            }
        })
        .WithSummary("静音/取消静音好友")
        .WithDescription("静音或取消静音指定好友")
        .Produces<ApiResponse>();

        // 11. GET /blocked — 获取已屏蔽好友列表
        group.MapGet("/blocked", async (
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var users = await friendProvider.GetBlockedUsersAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(users.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取已屏蔽好友列表失败");
                return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Error("获取已屏蔽好友列表失败"));
            }
        })
        .WithSummary("获取已屏蔽好友列表")
        .WithDescription("获取当前用户已屏蔽的好友列表")
        .Produces<ApiResponse<IEnumerable<FriendDto>>>();

        // 12. GET /starred — 获取星标好友列表
        group.MapGet("/starred", async (
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var friends = await friendProvider.GetStarredFriendsAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取星标好友列表失败");
                return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Error("获取星标好友列表失败"));
            }
        })
        .WithSummary("获取星标好友列表")
        .WithDescription("获取当前用户已星标的好友列表")
        .Produces<ApiResponse<IEnumerable<FriendDto>>>();

        // 13. GET /count — 获取好友数量
        group.MapGet("/count", async (
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var count = await friendProvider.GetFriendCountAsync(userId);
                return Results.Ok(ApiResponse<int>.Ok(count));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取好友数量失败");
                return Results.Ok(ApiResponse<int>.Error("获取好友数量失败"));
            }
        })
        .WithSummary("获取好友数量")
        .WithDescription("获取当前用户的好友总数")
        .Produces<ApiResponse<int>>();

        // 14. GET /search — 搜索好友
        group.MapGet("/search", async (
            ICurrentUserService currentUserService,
            IFriendProvider friendProvider,
                        [FromQuery] string searchTerm) =>
        {
            try
            {
                var userId = currentUserService.GetUserId();
                var friends = await friendProvider.SearchFriendsAsync(userId, searchTerm);
                return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "搜索好友失败");
                return Results.Ok(ApiResponse<IEnumerable<FriendDto>>.Error("搜索好友失败"));
            }
        })
        .WithSummary("搜索好友")
        .WithDescription("按搜索词查找好友")
        .Produces<ApiResponse<IEnumerable<FriendDto>>>();

        return group;
    }

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

    private static FriendRequestDto MapRequestToDto(MessageFriends request) => new()
    {
        FriendshipId = request.FriendshipId,
        RequesterId = request.UserId,
        Status = request.Status,
        CreatedTime = request.CreatedTime
    };
}
