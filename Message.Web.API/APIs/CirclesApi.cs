
namespace Message.Web.API.APIs;

/// <summary>
/// 兴趣圈子接口（静态函数模式 + CQRS）。
/// <para>
/// 约定与 TweetsApi 一致：写操作经 INotMediator 分发命令（仅返回 ID/结果），读操作分发查询；
/// 圈子为邀请制社区：成员列表/帖子流仅成员可见，邀请管理仅圈主/管理员可见。
/// </para>
/// </summary>
public static class CirclesApi
{
    /// <summary>映射圈子相关端点组</summary>
    public static RouteGroupBuilder MapCirclesApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/circles")
            .WithTags("Circles")
            .RequireAuthorization();

        // ── 字面量路由优先（避免与 {circleGuid} 冲突）──
        group.MapPost("/join", JoinCircleAsync)
            .WithSummary("凭邀请码/链接加入圈子")
            .Accepts<JoinCircleRequest>("application/json")
            .Produces<ApiResponse<Guid>>();

        group.MapGet("/invitations/my", GetMyInvitationsAsync)
            .WithSummary("我收到的直邀列表")
            .Produces<ApiResponse<PagedResult<CircleInvitationDto>>>();

        group.MapPost("/invitations/{inviteGuid}/accept", AcceptInvitationAsync)
            .WithSummary("接受直邀")
            .Produces<ApiResponse<Guid>>();

        group.MapPost("/invitations/{inviteGuid}/reject", RejectInvitationAsync)
            .WithSummary("拒绝直邀")
            .Produces<ApiResponse>();

        group.MapGet("/my", GetMyCirclesAsync)
            .WithSummary("我加入的圈子列表")
            .Produces<ApiResponse<List<CircleDto>>>();

        // ── 圈子 CRUD ──
        group.MapPost("/", CreateCircleAsync)
            .WithSummary("创建圈子")
            .Accepts<CreateCircleRequest>("application/json")
            .Produces<ApiResponse<Guid>>();

        group.MapGet("/{circleGuid}", GetCircleAsync)
            .WithSummary("圈子详情")
            .Produces<ApiResponse<CircleDto>>();

        group.MapPut("/{circleGuid}", UpdateCircleAsync)
            .WithSummary("更新圈子信息")
            .Accepts<UpdateCircleRequest>("application/json")
            .Produces<ApiResponse>();

        group.MapDelete("/{circleGuid}", DissolveCircleAsync)
            .WithSummary("解散圈子")
            .Produces<ApiResponse>();

        // ── 邀请管理 ──
        group.MapPost("/{circleGuid}/invitations", GenerateInvitationAsync)
            .WithSummary("生成邀请（码/链接/直邀）")
            .Accepts<GenerateInvitationRequest>("application/json")
            .Produces<ApiResponse<CircleInvitationResult>>();

        group.MapGet("/{circleGuid}/invitations", GetInvitationsAsync)
            .WithSummary("圈子的邀请列表（圈主/管理员）")
            .Produces<ApiResponse<PagedResult<CircleInvitationDto>>>();

        group.MapDelete("/{circleGuid}/invitations/{inviteGuid}", RevokeInvitationAsync)
            .WithSummary("撤销邀请")
            .Produces<ApiResponse>();

        // ── 成员管理 ──
        group.MapGet("/{circleGuid}/members", GetMembersAsync)
            .WithSummary("圈子成员列表")
            .Produces<ApiResponse<PagedResult<CircleMemberDto>>>();

        group.MapPost("/{circleGuid}/members/{userGuid}/role", SetMemberRoleAsync)
            .WithSummary("设置/取消管理员")
            .Accepts<SetCircleMemberRoleRequest>("application/json")
            .Produces<ApiResponse>();

        group.MapDelete("/{circleGuid}/members/{userGuid}", RemoveMemberAsync)
            .WithSummary("移出成员")
            .Produces<ApiResponse>();

        group.MapPost("/{circleGuid}/transfer", TransferOwnershipAsync)
            .WithSummary("转移圈主")
            .Produces<ApiResponse>();

        // ── 圈子内容 ──
        group.MapGet("/{circleGuid}/posts", GetCirclePostsAsync)
            .WithSummary("圈子帖子流")
            .Produces<ApiResponse<PagedResult<CommunityPostDto>>>();

        return group;
    }

    private static async Task<IResult> CreateCircleAsync(
        [FromBody] CreateCircleRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var circleId = await mediator.SendAsync(new CreateCircleCommand(
                currentUser.GetUserId(), request.Name, request.Description,
                request.AvatarUrl, request.MaxMembers), ct);
            return Results.Ok(ApiResponse<Guid>.Created(circleId, "圈子创建成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"创建圈子失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetMyCirclesAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var circles = await mediator.SendAsync(new GetUserCirclesQuery(currentUser.GetUserId()), ct);
            var dtos = circles.Select(c => c.ToDto(currentUser.GetUserId())).ToList();
            return Results.Ok(ApiResponse<List<CircleDto>>.Ok(dtos));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<List<CircleDto>>.Error($"获取圈子列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetCircleAsync(
        Guid circleGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var result = await mediator.SendAsync(new GetCircleQuery(circleGuid, userId), ct);
            if (result.Circle is null)
                return Results.Ok(ApiResponse<CircleDto>.NotFound("圈子不存在"));

            var dto = result.Circle.ToDto(userId);
            return Results.Ok(ApiResponse<CircleDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<CircleDto>.Error($"获取圈子详情失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateCircleAsync(
        Guid circleGuid,
        [FromBody] UpdateCircleRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new UpdateCircleCommand(
                currentUser.GetUserId(), circleGuid, request.Name, request.Description, request.AvatarUrl), ct);
            return Results.Ok(ApiResponse.Ok("圈子信息已更新"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"更新圈子失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> DissolveCircleAsync(
        Guid circleGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new DissolveCircleCommand(currentUser.GetUserId(), circleGuid), ct);
            return Results.Ok(ApiResponse.Ok("圈子已解散"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"解散圈子失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GenerateInvitationAsync(
        Guid circleGuid,
        [FromBody] GenerateInvitationRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.SendAsync(new GenerateCircleInvitationCommand(
                currentUser.GetUserId(), circleGuid, request.Type, request.InviteeGuid, request.TtlHours), ct);
            return Results.Ok(ApiResponse<CircleInvitationResult>.Created(result, "邀请生成成功"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse<CircleInvitationResult>.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse<CircleInvitationResult>.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<CircleInvitationResult>.Error($"生成邀请失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetInvitationsAsync(
        Guid circleGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetCircleInvitationsQuery(circleGuid, currentUser.GetUserId(), page, pageSize), ct);
            var dto = new PagedResult<CircleInvitationDto>
            {
                Items = paged.Items.Select(i => i.ToDto(string.Empty)).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
            return Results.Ok(ApiResponse<PagedResult<CircleInvitationDto>>.Ok(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse<PagedResult<CircleInvitationDto>>.Forbidden(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CircleInvitationDto>>.Error($"获取邀请列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid circleGuid,
        Guid inviteGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new RevokeCircleInvitationCommand(currentUser.GetUserId(), inviteGuid), ct);
            return Results.Ok(ApiResponse.Ok("邀请已撤销"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"撤销邀请失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> JoinCircleAsync(
        [FromBody] JoinCircleRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var circleId = await mediator.SendAsync(new JoinCircleCommand(currentUser.GetUserId(), request.Code, request.Token), ct);
            return Results.Ok(ApiResponse<Guid>.Ok(circleId, "加入圈子成功"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse<Guid>.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse<Guid>.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"加入圈子失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetMyInvitationsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetMyInvitationsQuery(currentUser.GetUserId(), page, pageSize), ct);
            var dto = new PagedResult<CircleInvitationDto>
            {
                Items = paged.Items.Select(i => i.ToDto(string.Empty)).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
            return Results.Ok(ApiResponse<PagedResult<CircleInvitationDto>>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CircleInvitationDto>>.Error($"获取我的邀请失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> AcceptInvitationAsync(
        Guid inviteGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var circleId = await mediator.SendAsync(new AcceptCircleInvitationCommand(currentUser.GetUserId(), inviteGuid), ct);
            return Results.Ok(ApiResponse<Guid>.Ok(circleId, "已加入圈子"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse<Guid>.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse<Guid>.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"接受邀请失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> RejectInvitationAsync(
        Guid inviteGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new RejectCircleInvitationCommand(currentUser.GetUserId(), inviteGuid), ct);
            return Results.Ok(ApiResponse.Ok("已拒绝邀请"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"拒绝邀请失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetMembersAsync(
        Guid circleGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetCircleMembersQuery(circleGuid, currentUser.GetUserId(), page, pageSize), ct);
            var dto = new PagedResult<CircleMemberDto>
            {
                Items = paged.Items.Select(m => m.ToDto()).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
            return Results.Ok(ApiResponse<PagedResult<CircleMemberDto>>.Ok(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse<PagedResult<CircleMemberDto>>.Forbidden(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CircleMemberDto>>.Error($"获取成员列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> SetMemberRoleAsync(
        Guid circleGuid,
        Guid userGuid,
        [FromBody] SetCircleMemberRoleRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new SetCircleMemberRoleCommand(currentUser.GetUserId(), circleGuid, userGuid, request.Role), ct);
            return Results.Ok(ApiResponse.Ok("成员角色已更新"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"设置成员角色失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid circleGuid,
        Guid userGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new RemoveCircleMemberCommand(currentUser.GetUserId(), circleGuid, userGuid), ct);
            return Results.Ok(ApiResponse.Ok("成员已移出"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"移出成员失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> TransferOwnershipAsync(
        Guid circleGuid,
        [FromBody] CircleTransferOwnershipRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new TransferCircleOwnershipCommand(currentUser.GetUserId(), circleGuid, request.NewOwnerGuid), ct);
            return Results.Ok(ApiResponse.Ok("圈主已转移"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"转移圈主失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetCirclePostsAsync(
        Guid circleGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetCirclePostsQuery(circleGuid, currentUser.GetUserId(), page, pageSize), ct);
            var dto = new PagedResult<CommunityPostDto>
            {
                Items = paged.Items.Select(t => t.ToCommunityDto()).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
            return Results.Ok(ApiResponse<PagedResult<CommunityPostDto>>.Ok(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse<PagedResult<CommunityPostDto>>.Forbidden(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CommunityPostDto>>.Error($"获取圈子帖子失败: {ex.Message}"), statusCode: 500);
        }
    }
}
