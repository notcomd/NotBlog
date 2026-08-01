using Message.Web.API.Application.Commands.Groups;
using Message.Web.API.Application.Queries.Groups;

namespace Message.Web.API.APIs;

/// <summary>
/// 群组接口（静态函数模式 + CQRS）。
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
public static class GroupsApi
{
    /// <summary>映射群组相关端点组</summary>
    public static RouteGroupBuilder MapGroupsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/groups");

        // 1. POST / — 创建群组
        group.MapPost("/", CreateGroupAsync)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 2. GET / — 获取我的群组列表
        group.MapGet("/", GetUserGroupsAsync)
            .Produces<ApiResponse<IEnumerable<GroupDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<IEnumerable<GroupDto>>>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 3. GET /{id} — 获取群组详情
        group.MapGet("/{id}", GetGroupAsync)
            .Produces<ApiResponse<GroupDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<GroupDto>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<GroupDto>>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 4. PUT /{id}/info — 更新群组信息
        group.MapPut("/{id}/info", UpdateGroupInfoAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 5. DELETE /{id} — 解散群组
        group.MapDelete("/{id}", DismissGroupAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 6. GET /{id}/members — 获取群组成员列表
        group.MapGet("/{id}/members", GetMembersAsync)
            .Produces<ApiResponse<IEnumerable<GroupMemberDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<IEnumerable<GroupMemberDto>>>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 7. POST /{id}/members — 添加群组成员
        group.MapPost("/{id}/members", AddMemberAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 8. DELETE /{id}/members/{userId} — 移除群组成员
        group.MapDelete("/{id}/members/{userId}", RemoveMemberAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 9. PUT /{id}/admins — 设置/取消管理员
        group.MapPut("/{id}/admins", SetAdminAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 10. PUT /{id}/transfer — 转让群主
        group.MapPut("/{id}/transfer", TransferOwnershipAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 11. PUT /{id}/members/{userId}/mute — 禁言成员
        group.MapPut("/{id}/members/{userId}/mute", MuteMemberAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 12. DELETE /{id}/members/{userId}/mute — 解除禁言
        group.MapDelete("/{id}/members/{userId}/mute", UnmuteMemberAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 13. PUT /{id}/members/{userId}/ban — 封禁成员
        group.MapPut("/{id}/members/{userId}/ban", BanMemberAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 14. DELETE /{id}/members/{userId}/ban — 解除封禁
        group.MapDelete("/{id}/members/{userId}/ban", UnbanMemberAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 15. GET /public — 获取公开群组列表
        group.MapGet("/public", GetPublicGroupsAsync)
            .Produces<ApiResponse<IEnumerable<GroupDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<IEnumerable<GroupDto>>>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 16. GET /search — 搜索群组
        group.MapGet("/search", SearchGroupsAsync)
            .Produces<ApiResponse<PagedResult<GroupDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<PagedResult<GroupDto>>>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 17. GET /{id}/member-count — 获取群组成员数
        group.MapGet("/{id}/member-count", GetMemberCountAsync)
            .Produces<ApiResponse<int>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<int>>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        // 18. GET /{id}/is-member/{userId} — 检查是否为群组成员
        group.MapGet("/{id}/is-member/{userId}", IsMemberAsync)
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<bool>>(StatusCodes.Status400BadRequest)
            .WithTags("Groups");

        return group;
    }

    /// <summary>
    /// 创建群组，并可一次性添加初始成员。
    /// 命令侧（CreateGroupCommand）：仅返回新群组 ID。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">创建群组请求体</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新群组 ID</returns>
    private static async Task<IResult> CreateGroupAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromBody] CreateGroupRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var groupId = await mediator.SendAsync(
                new CreateGroupCommand(
                    userId,
                    request.GroupName,
                    request.MaxMembers,
                    request.IsPublic,
                    request.InitialMembers),
                ct);

            return Results.Ok(ApiResponse<Guid>.Created(groupId, "群组创建成功"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<Guid>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 获取当前用户加入的群组列表（查询侧）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>群组 DTO 列表</returns>
    private static async Task<IResult> GetUserGroupsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var groups = await mediator.SendAsync(new GetUserGroupsQuery(userId), ct);
            return Results.Ok(ApiResponse<IEnumerable<GroupDto>>.Ok(groups.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<IEnumerable<GroupDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 获取群组详情（查询侧）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>群组 DTO</returns>
    private static async Task<IResult> GetGroupAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var grp = await mediator.SendAsync(new GetGroupQuery(id), ct);
            if (grp == null)
                return Results.NotFound(ApiResponse<GroupDto>.NotFound("群组不存在"));

            return Results.Ok(ApiResponse<GroupDto>.Ok(MapToDto(grp)));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<GroupDto>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 更新群组信息（名称/描述）。
    /// 命令侧（UpdateGroupInfoCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">更新信息请求体</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> UpdateGroupInfoAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        [FromBody] UpdateGroupInfoRequest request,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new UpdateGroupInfoCommand(id, request.GroupName, request.Description), ct);
            return Results.Ok(ApiResponse.Ok("群组信息已更新"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 解散群组。
    /// 命令侧（DismissGroupCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> DismissGroupAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new DismissGroupCommand(id), ct);
            return Results.Ok(ApiResponse.Ok("群组已解散"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 获取群组成员列表（查询侧）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>群组成员 DTO 列表</returns>
    private static async Task<IResult> GetMembersAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var members = await mediator.SendAsync(new GetGroupMembersQuery(id), ct);
            return Results.Ok(ApiResponse<IEnumerable<GroupMemberDto>>.Ok(members.Select(MapMemberToDto)));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<IEnumerable<GroupMemberDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 添加群组成员。
    /// 命令侧（AddGroupMemberCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">添加成员请求体（用户ID与角色）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> AddMemberAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        [FromBody] AddGroupMemberRequest request,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new AddGroupMemberCommand(id, request.UserId, request.Role), ct);
            return Results.Ok(ApiResponse.Ok("成员已添加"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 移除群组成员。
    /// 命令侧（RemoveGroupMemberCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="userId">要移除的用户ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> RemoveMemberAsync(
        Guid id,
        Guid userId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new RemoveGroupMemberCommand(id, userId), ct);
            return Results.Ok(ApiResponse.Ok("成员已移除"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 设置/取消管理员。
    /// 命令侧（SetAdminCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">设置管理员请求体（用户ID与是否设为管理员）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> SetAdminAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        [FromBody] SetAdminRequest request,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new SetAdminCommand(id, request.UserId, request.IsAdmin), ct);
            return Results.Ok(ApiResponse.Ok(request.IsAdmin ? "已设为管理员" : "已取消管理员"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 转让群主。
    /// 命令侧（TransferOwnershipCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">转让请求体（新群主ID）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> TransferOwnershipAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        [FromBody] TransferOwnershipRequest request,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new TransferOwnershipCommand(id, request.NewOwnerId), ct);
            return Results.Ok(ApiResponse.Ok("群主已转让"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 禁言群组成员。
    /// 命令侧（MuteGroupMemberCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="userId">被禁言的用户ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="request">禁言请求体（禁言时长/分钟）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> MuteMemberAsync(
        Guid id,
        Guid userId,
        [FromServices] INotMediator mediator,
        [FromBody] MuteMemberRequest request,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new MuteGroupMemberCommand(id, userId, request.DurationMinutes), ct);
            return Results.Ok(ApiResponse.Ok("成员已禁言"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 解除群组成员禁言。
    /// 命令侧（UnmuteGroupMemberCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="userId">被解除禁言的用户ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> UnmuteMemberAsync(
        Guid id,
        Guid userId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new UnmuteGroupMemberCommand(id, userId), ct);
            return Results.Ok(ApiResponse.Ok("成员已解除禁言"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 封禁群组成员。
    /// 命令侧（BanGroupMemberCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="userId">被封禁的用户ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> BanMemberAsync(
        Guid id,
        Guid userId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new BanGroupMemberCommand(id, userId), ct);
            return Results.Ok(ApiResponse.Ok("成员已封禁"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 解除群组成员封禁。
    /// 命令侧（UnbanGroupMemberCommand）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="userId">被解除封禁的用户ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> UnbanMemberAsync(
        Guid id,
        Guid userId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new UnbanGroupMemberCommand(id, userId), ct);
            return Results.Ok(ApiResponse.Ok("成员已解除封禁"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    /// <summary>
    /// 获取公开群组列表（查询侧）。
    /// </summary>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>公开群组 DTO 列表</returns>
    private static async Task<IResult> GetPublicGroupsAsync(
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var groups = await mediator.SendAsync(new GetPublicGroupsQuery(), ct);
            return Results.Ok(ApiResponse<IEnumerable<GroupDto>>.Ok(groups.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<IEnumerable<GroupDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 搜索群组（分页）（查询侧）。
    /// </summary>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="searchTerm">搜索关键词（查询参数）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页搜索结果</returns>
    private static async Task<IResult> SearchGroupsAsync(
        [FromServices] INotMediator mediator,
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var groups = await mediator.SendAsync(new SearchGroupsQuery(searchTerm, page, pageSize), ct);
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
    }

    /// <summary>
    /// 获取群组成员数量（查询侧）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>成员数量</returns>
    private static async Task<IResult> GetMemberCountAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var count = await mediator.SendAsync(new GetGroupMemberCountQuery(id), ct);
            return Results.Ok(ApiResponse<int>.Ok(count));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<int>.Error(ex.Message));
        }
    }

    /// <summary>
    /// 检查用户是否为群组成员（查询侧）。
    /// </summary>
    /// <param name="id">群组ID（路由参数）</param>
    /// <param name="userId">用户ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否为成员</returns>
    private static async Task<IResult> IsMemberAsync(
        Guid id,
        Guid userId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var isMember = await mediator.SendAsync(new IsGroupMemberQuery(id, userId), ct);
            return Results.Ok(ApiResponse<bool>.Ok(isMember));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
    }

    /// <summary>群组实体 → DTO 映射</summary>
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

    /// <summary>群组成员实体 → DTO 映射</summary>
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
