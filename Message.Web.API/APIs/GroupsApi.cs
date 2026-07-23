namespace Message.Web.API.APIs;

public static class GroupsApi
{
    public static RouteGroupBuilder MapGroupsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/groups");

        // 1. POST / — 创建群组
        group.MapPost("/", async (ICurrentUserService currentUser, IGroupProvider groupProvider, [FromBody] CreateGroupRequest request) =>
        {
            try
            {
                var userId = currentUser.GetUserId();
                var grp = await groupProvider.CreateGroupAsync(userId, request.GroupName, request.MaxMembers,
                    request.IsPublic);

                if (request.InitialMembers != null && request.InitialMembers.Any())
                {
                    foreach (var memberId in request.InitialMembers)
                    {
                        await groupProvider.AddMemberAsync(grp.GroupId, memberId);
                    }
                }

                return Results.Ok(ApiResponse<GroupDto>.Created(MapToDto(grp), "群组创建成功"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<GroupDto>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<GroupDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<GroupDto>>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 2. GET / — 获取我的群组列表
        group.MapGet("/", async (ICurrentUserService currentUser, IGroupProvider groupProvider) =>
        {
            try
            {
                var userId = currentUser.GetUserId();
                var groups = await groupProvider.GetUserGroupsAsync(userId);
                return Results.Ok(ApiResponse<IEnumerable<GroupDto>>.Ok(groups.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<IEnumerable<GroupDto>>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<IEnumerable<GroupDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<IEnumerable<GroupDto>>>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 3. GET /{id} — 获取群组详情
        group.MapGet("/{id}", async (Guid id, IGroupProvider groupProvider) =>
        {
            try
            {
                var grp = await groupProvider.GetGroupAsync(id);
                if (grp == null)
                    return Results.NotFound(ApiResponse<GroupDto>.NotFound("群组不存在"));

                return Results.Ok(ApiResponse<GroupDto>.Ok(MapToDto(grp)));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<GroupDto>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<GroupDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<GroupDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<GroupDto>>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 4. PUT /{id}/info — 更新群组信息
        group.MapPut("/{id}/info", async (Guid id, IGroupProvider groupProvider, [FromBody] UpdateGroupInfoRequest request) =>
        {
            try
            {
                await groupProvider.UpdateGroupInfoAsync(id, request.GroupName, request.Description);
                return Results.Ok(ApiResponse.Ok("群组信息已更新"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 5. DELETE /{id} — 解散群组
        group.MapDelete("/{id}", async (Guid id, IGroupProvider groupProvider) =>
        {
            try
            {
                await groupProvider.DismissGroupAsync(id);
                return Results.Ok(ApiResponse.Ok("群组已解散"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 6. GET /{id}/members — 获取群组成员列表
        group.MapGet("/{id}/members", async (Guid id, IGroupProvider groupProvider) =>
        {
            try
            {
                var members = await groupProvider.GetMembersAsync(id);
                return Results.Ok(ApiResponse<IEnumerable<GroupMemberDto>>.Ok(members.Select(MapMemberToDto)));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<IEnumerable<GroupMemberDto>>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<IEnumerable<GroupMemberDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<IEnumerable<GroupMemberDto>>>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 7. POST /{id}/members — 添加群组成员
        group.MapPost("/{id}/members", async (Guid id, IGroupProvider groupProvider, [FromBody] AddGroupMemberRequest request) =>
        {
            try
            {
                await groupProvider.AddMemberAsync(id, request.UserId, request.Role);
                return Results.Ok(ApiResponse.Ok("成员已添加"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 8. DELETE /{id}/members/{userId} — 移除群组成员
        group.MapDelete("/{id}/members/{userId}", async (Guid id, Guid userId, IGroupProvider groupProvider) =>
        {
            try
            {
                await groupProvider.RemoveMemberAsync(id, userId);
                return Results.Ok(ApiResponse.Ok("成员已移除"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 9. PUT /{id}/admins — 设置/取消管理员
        group.MapPut("/{id}/admins", async (Guid id, IGroupProvider groupProvider, [FromBody] SetAdminRequest request) =>
        {
            try
            {
                if (request.IsAdmin)
                    await groupProvider.PromoteToAdminAsync(id, request.UserId);
                else
                    await groupProvider.DemoteToMemberAsync(id, request.UserId);

                return Results.Ok(ApiResponse.Ok(request.IsAdmin ? "已设为管理员" : "已取消管理员"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 10. PUT /{id}/transfer — 转让群主
        group.MapPut("/{id}/transfer", async (Guid id, IGroupProvider groupProvider, [FromBody] TransferOwnershipRequest request) =>
        {
            try
            {
                await groupProvider.TransferOwnershipAsync(id, request.NewOwnerId);
                return Results.Ok(ApiResponse.Ok("群主已转让"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 11. PUT /{id}/members/{userId}/mute — 禁言成员
        group.MapPut("/{id}/members/{userId}/mute", async (Guid id, Guid userId, IGroupProvider groupProvider, [FromBody] MuteMemberRequest request) =>
        {
            try
            {
                await groupProvider.MuteMemberAsync(id, userId, TimeSpan.FromMinutes(request.DurationMinutes));
                return Results.Ok(ApiResponse.Ok("成员已禁言"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 12. DELETE /{id}/members/{userId}/mute — 解除禁言
        group.MapDelete("/{id}/members/{userId}/mute", async (Guid id, Guid userId, IGroupProvider groupProvider) =>
        {
            try
            {
                await groupProvider.UnmuteMemberAsync(id, userId);
                return Results.Ok(ApiResponse.Ok("成员已解除禁言"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 13. PUT /{id}/members/{userId}/ban — 封禁成员
        group.MapPut("/{id}/members/{userId}/ban", async (Guid id, Guid userId, IGroupProvider groupProvider) =>
        {
            try
            {
                await groupProvider.BanMemberAsync(id, userId);
                return Results.Ok(ApiResponse.Ok("成员已封禁"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 14. DELETE /{id}/members/{userId}/ban — 解除封禁
        group.MapDelete("/{id}/members/{userId}/ban", async (Guid id, Guid userId, IGroupProvider groupProvider) =>
        {
            try
            {
                await groupProvider.UnbanMemberAsync(id, userId);
                return Results.Ok(ApiResponse.Ok("成员已解除封禁"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Error(ex.Message));
            }
        })
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 15. GET /public — 获取公开群组列表
        group.MapGet("/public", async (IGroupProvider groupProvider) =>
        {
            try
            {
                var groups = await groupProvider.GetPublicGroupsAsync();
                return Results.Ok(ApiResponse<IEnumerable<GroupDto>>.Ok(groups.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<IEnumerable<GroupDto>>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<IEnumerable<GroupDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<IEnumerable<GroupDto>>>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 16. GET /search — 搜索群组
        group.MapGet("/search", async (IGroupProvider groupProvider, [FromQuery] string searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        {
            try
            {
                var groups = await groupProvider.SearchGroupsAsync(searchTerm, page, pageSize);
                var result = new PagedResult<GroupDto>
                {
                    Items = groups.Select(MapToDto).ToList(),
                    TotalCount = groups.Count(),
                    Page = page,
                    PageSize = pageSize
                };
                return Results.Ok(ApiResponse<PagedResult<GroupDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<PagedResult<GroupDto>>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<PagedResult<GroupDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<PagedResult<GroupDto>>>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 17. GET /{id}/member-count — 获取群组成员数
        group.MapGet("/{id}/member-count", async (Guid id, IGroupProvider groupProvider) =>
        {
            try
            {
                var count = await groupProvider.GetMemberCountAsync(id);
                return Results.Ok(ApiResponse<int>.Ok(count));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<int>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<int>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<int>>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        // 18. GET /{id}/is-member/{userId} — 检查是否为群组成员
        group.MapGet("/{id}/is-member/{userId}", async (Guid id, Guid userId, IGroupProvider groupProvider) =>
        {
            try
            {
                var isMember = await groupProvider.IsMemberAsync(id, userId);
                return Results.Ok(ApiResponse<bool>.Ok(isMember));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<bool>.Error(ex.Message));
            }
        })
        .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<bool>>(StatusCodes.Status400BadRequest)
        .WithTags("Groups");

        return group;
    }

    private static GroupDto MapToDto(Group group) => new()
    {
        GroupId = group.GroupId,
        GroupName = group.GroupName,
        Description = group.Description,
        OwnerId = group.OwnerId,
        MaxMembers = group.MaxMembers,
        MemberCount = group.MemberCount,
        IsPublic = group.IsPublic,
        CreatedTime = group.CreatedTime,
        IsDismissed = group.IsDismissed
    };

    private static GroupMemberDto MapMemberToDto(GroupMember member) => new()
    {
        MemberId = member.MemberId,
        GroupId = member.GroupId,
        UserId = member.UserId,
        Role = member.Role,
        Nickname = member.Nickname,
        JoinTime = member.JoinTime,
        IsMuted = member.IsMuted,
        IsBanned = member.IsBanned
    };
}
