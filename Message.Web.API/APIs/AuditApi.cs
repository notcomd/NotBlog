using Message.Web.API.Application.Commands.Audit;

namespace Message.Web.API.APIs;

/// <summary>
/// 审核接口（静态函数模式 + CQRS，仅管理员可调用）。
/// <para>
/// 设计约定：
/// - 所有端点处理程序均为<b>静态函数</b>（不使用 Action/Lambda 创建接口）；
/// - 依赖服务通过 <c>[FromServices]</c> 特性注入，生命周期由服务注册文件统一管理；
/// - 路由参数（如 {tweetGuid}、{reportGuid}）由框架按名称绑定，请求体使用 <c>[FromBody]</c>；
/// - 数据写操作通过 <see cref="INotMediator"/> 分发到命令处理程序（Commands），
///   命令仅返回操作结果（bool），不返回业务实体/DTO；
/// - 数据读操作通过 <see cref="INotMediator"/> 分发到查询处理程序（Queries），
///   查询不修改任何数据状态，仅返回只读结果。
/// </para>
/// </summary>
public static class AuditApi
{
    /// <summary>映射审核相关端点组</summary>
    public static RouteGroupBuilder MapAuditApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit")
            .WithTags("Audit")
            .RequireAuthorization();

        group.MapGet("/tweets/pending", GetPendingTweetsAsync)
            .WithSummary("获取待审核推文列表")
            .WithDescription("管理员获取所有待审核的推文列表，支持分页")
            .Produces<ApiResponse<PagedResult<object>>>();


        group.MapPost("/tweets/{tweetGuid}/approve", ApproveTweetAsync)
            .WithSummary("通过推文审核")
            .WithDescription("管理员通过指定推文的审核")
            .Produces<ApiResponse>();


        group.MapPost("/tweets/{tweetGuid}/reject", RejectTweetAsync)
            .WithSummary("驳回推文")
            .WithDescription("管理员驳回指定推文，需提供驳回原因")
            .Accepts<AuditActionRequest>("application/json")
            .Produces<ApiResponse>();


        group.MapGet("/reports/pending", GetPendingReportsAsync)
            .WithSummary("获取待处理举报列表")
            .WithDescription("管理员获取所有待处理的举报列表，支持分页")
            .Produces<ApiResponse<PagedResult<object>>>();


        group.MapPost("/reports/{reportGuid}/resolve", ResolveReportAsync)
            .WithSummary("处理举报")
            .WithDescription("管理员处理指定举报")
            .Accepts<ResolveReportRequest>("application/json")
            .Produces<ApiResponse>();

        // ── 运营管理补充端点（2026-08 补齐） ──

        // GET /stats — 运营统计
        group.MapGet("/stats", GetStatsAsync)
            .WithSummary("运营统计")
            .WithDescription("总推文数/待审核数/待处理举报数/圈子总数/在线用户数")
            .Produces<ApiResponse<object>>();

        // GET /online-users — 在线用户列表
        group.MapGet("/online-users", GetOnlineUsersAsync)
            .WithSummary("在线用户列表")
            .WithDescription("读取 Redis 在线集合，返回在线用户 ID 数组")
            .Produces<ApiResponse<IEnumerable<Guid>>>();

        // GET /activity-logs — 操作日志
        group.MapGet("/activity-logs", GetActivityLogsAsync)
            .WithSummary("操作日志")
            .WithDescription("管理员内容审核/处理记录（分页）")
            .Produces<ApiResponse<PagedResult<object>>>();

        // POST /tweets/{tweetGuid}/block — 屏蔽内容
        group.MapPost("/tweets/{tweetGuid}/block", BlockTweetAsync)
            .WithSummary("屏蔽内容")
            .WithDescription("管理员屏蔽推文（置为驳回状态并写入原因“运营屏蔽”）")
            .Produces<ApiResponse>();

        // POST /tweets/{tweetGuid}/delete — 删除内容（管理员）
        group.MapPost("/tweets/{tweetGuid}/delete", AdminDeleteTweetAsync)
            .WithSummary("删除内容（管理员）")
            .WithDescription("管理员删除指定推文（不限于作者本人）")
            .Produces<ApiResponse>();

        // GET /circles — 全量社区列表（含已解散）
        group.MapGet("/circles", GetCirclesAdminAsync)
            .WithSummary("全量社区列表（管理员）")
            .WithDescription("分页查询全部社区，支持名称关键字过滤，含已解散/封禁状态")
            .Produces<ApiResponse<PagedResult<object>>>();

        // POST /circles/{circleGuid}/ban — 封禁（解散）社区
        group.MapPost("/circles/{circleGuid:guid}/ban", BanCircleAdminAsync)
            .WithSummary("封禁社区（管理员）")
            .WithDescription("管理员解散指定社区（不限圈主），成员会话同步失效")
            .Produces<ApiResponse>();

        return group;
    }

    /// <summary>
    /// 获取待审核推文列表（查询侧，仅管理员）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页待审核推文列表</returns>
    private static async Task<IResult> GetPendingTweetsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

            var paged = await mediator.SendAsync(new GetPendingTweetsQuery(page, pageSize), ct);

            var result = new PagedResult<object>
            {
                Items = [.. paged.Items.Select(t => new
                {
                    t.TweetGuid,
                    t.AuthorGuid,
                    t.Content,
                    TweetStatus = t.TweetStatus.ToString(),
                    t.ViewCount,
                    t.LikeCount,
                    t.CommentCount,
                    t.CreateTime,
                    t.PublishTime
                })],
                TotalCount = paged.TotalCount,
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<object>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<object>>.Error($"获取待审核推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 通过推文审核（命令侧，仅管理员）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> ApproveTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

            var auditorGuid = currentUser.GetUserId();
            await mediator.SendAsync(new ApproveTweetCommand(tweetGuid, auditorGuid), ct);

            return Results.Ok(ApiResponse.Ok("推文已通过审核"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"审核推文通过失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 驳回推文（命令侧，仅管理员）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="request">驳回请求体（驳回原因）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> RejectTweetAsync(
        Guid tweetGuid,
        [FromBody] AuditActionRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

            var auditorGuid = currentUser.GetUserId();
            await mediator.SendAsync(new RejectTweetCommand(tweetGuid, auditorGuid, request.Reason), ct);

            return Results.Ok(ApiResponse.Ok("推文已驳回"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"驳回推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取待处理举报列表（查询侧，仅管理员）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页待处理举报列表</returns>
    private static async Task<IResult> GetPendingReportsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

            var paged = await mediator.SendAsync(new GetPendingReportsQuery(page, pageSize), ct);

            var result = new PagedResult<object>
            {
                Items = [.. paged.Items.Select(report => new
                {
                    report.ReportGuid,
                    report.ReporterGuid,
                    TargetType = report.TargetType.ToString(),
                    report.TargetGuid,
                    report.ReportReason,
                    Category = report.Category.ToString(),
                    report.EvidenceUrls,
                    Status = report.Status.ToString(),
                    report.ReviewerGuid,
                    report.ReviewNote,
                    report.ReviewTime,
                    report.CreateTime
                })],
                TotalCount = paged.TotalCount,
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<object>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<object>>.Error($"获取待处理举报失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 处理举报（命令侧，仅管理员）。
    /// </summary>
    /// <param name="reportGuid">举报ID（路由参数）</param>
    /// <param name="request">处理举报请求体（处理备注与是否删除内容）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> ResolveReportAsync(
        Guid reportGuid,
        [FromBody] ResolveReportRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

            var reviewerGuid = currentUser.GetUserId();
            var isContentRemoved = request.Action?.ToLower() == "removed";

            await mediator.SendAsync(
                new ResolveReportCommand(reportGuid, reviewerGuid, request.Note, isContentRemoved),
                ct);

            return Results.Ok(ApiResponse.Ok("举报已处理"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"处理举报失败: {ex.Message}"), statusCode: 500);
        }
    }

    // ──────────── 运营管理补充端点实现 ────────────

    /// <summary>运营统计：总推文/待审核/待处理举报/圈子总数/在线用户数（仅管理员）</summary>
    private static async Task<IResult> GetStatsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] ITweetRepository tweetRepository,
        [FromServices] ITweetReportRepository reportRepository,
        [FromServices] ICircleRepository circleRepository,
        [FromServices] UserStatusCacheService userStatusCache,
        CancellationToken ct = default)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可查看运营统计"), statusCode: 403);

            var totalTweets = await tweetRepository.GetCountAllAsync();
            var pendingTweets = await tweetRepository.GetPendingAuditCountAsync();
            var pendingReports = await reportRepository.GetPendingCountAsync();
            var totalCircles = await circleRepository.GetTotalCountAsync(null);
            var onlineUsers = await userStatusCache.GetOnlineUserCountAsync(ct);

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                totalTweets,
                pendingTweets,
                pendingReports,
                totalCircles,
                onlineUsers
            }));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<object>.Error($"获取运营统计失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>在线用户列表（仅管理员）：Redis 在线集合 → 用户 ID 数组</summary>
    private static async Task<IResult> GetOnlineUsersAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] UserStatusCacheService userStatusCache,
        CancellationToken ct = default)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可查看在线用户"), statusCode: 403);

            var ids = await userStatusCache.GetOnlineUserIdsAsync(ct);
            var userGuids = ids
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse);

            return Results.Ok(ApiResponse<IEnumerable<Guid>>.Ok(userGuids));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<Guid>>.Error($"获取在线用户失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>操作日志分页（仅管理员）：来自 TweetAuditLog 审核记录</summary>
    private static async Task<IResult> GetActivityLogsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] ITweetAuditRepository auditRepository,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可查看操作日志"), statusCode: 403);

            var logs = await auditRepository.GetPagedAsync(page, pageSize);
            var total = await auditRepository.GetCountAsync();

            var items = logs.Select(l => new
            {
                l.AuditGuid,
                l.TweetGuid,
                l.AuditorGuid,
                Action = l.Action.ToString(),
                l.Reason,
                AuditTime = l.AuditTime
            });

            return Results.Ok(ApiResponse<PagedResult<object>>.Ok(new PagedResult<object>
            {
                Items = [.. items.Cast<object>()],
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            }));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<object>>.Error($"获取操作日志失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>屏蔽内容（仅管理员）：复用 RejectTweetCommand（置为驳回 + 审计日志，原因=运营屏蔽）</summary>
    private static async Task<IResult> BlockTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可执行屏蔽操作"), statusCode: 403);

            await mediator.SendAsync(new RejectTweetCommand(tweetGuid, currentUser.GetUserId(), "运营屏蔽（管理员屏蔽内容）"), ct);
            return Results.Ok(ApiResponse.Ok("内容已屏蔽"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"屏蔽内容失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>管理员删除内容：复用 DeleteTweetCommand（作者本人 / 全局管理员均可删除）</summary>
    private static async Task<IResult> AdminDeleteTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可执行删除操作"), statusCode: 403);

            await mediator.SendAsync(new DeleteTweetCommand(tweetGuid, currentUser.GetUserId()), ct);
            return Results.Ok(ApiResponse.Ok("内容已删除"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"删除内容失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>全量社区列表（仅管理员）：分页返回全部圈子（含已解散/封禁状态）</summary>
    private static async Task<IResult> GetCirclesAdminAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] ICircleRepository circleRepository,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可查看社区列表"), statusCode: 403);

            var circles = await circleRepository.GetPagedAsync(keyword, page, pageSize);
            var total = await circleRepository.GetTotalCountAsync(keyword);

            var dtos = circles.Select(c => c.ToDto()).ToList();
            return Results.Ok(ApiResponse<PagedResult<object>>.Ok(new PagedResult<object>
            {
                Items = [.. dtos.Cast<object>()],
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            }));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<object>>.Error($"获取社区列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>封禁（解散）社区（仅管理员）：不限圈主，复用 Circle.Dissolve() 同步会话失效</summary>
    private static async Task<IResult> BanCircleAdminAsync(
        Guid circleGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] ICircleRepository circleRepository,
        CancellationToken ct)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponse.Forbidden("仅管理员可封禁社区"), statusCode: 403);

            var circle = await circleRepository.GetByIdWithMembersAsync(circleGuid);
            if (circle is null)
                return Results.NotFound(ApiResponse.NotFound("社区不存在"));

            circle.Dissolve();
            await circleRepository.UpdateAsync(circle);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(ct);

            return Results.Ok(ApiResponse.Ok("社区已封禁（解散）"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"封禁社区失败: {ex.Message}"), statusCode: 500);
        }
    }
}
