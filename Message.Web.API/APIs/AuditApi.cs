using Message.Web.API.Application.Commands.Audit;
using Message.Web.API.Application.Queries.Audit;

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
            .WithTags("Audit");

        // GET /tweets/pending — 获取待审核推文列表
        group.MapGet("/tweets/pending", GetPendingTweetsAsync)
            .WithSummary("获取待审核推文列表")
            .WithDescription("管理员获取所有待审核的推文列表，支持分页")
            .Produces<ApiResponse<PagedResult<object>>>();

        // POST /tweets/{tweetGuid}/approve — 通过推文审核
        group.MapPost("/tweets/{tweetGuid}/approve", ApproveTweetAsync)
            .WithSummary("通过推文审核")
            .WithDescription("管理员通过指定推文的审核")
            .Produces<ApiResponse>();

        // POST /tweets/{tweetGuid}/reject — 驳回推文
        group.MapPost("/tweets/{tweetGuid}/reject", RejectTweetAsync)
            .WithSummary("驳回推文")
            .WithDescription("管理员驳回指定推文，需提供驳回原因")
            .Accepts<AuditActionRequest>("application/json")
            .Produces<ApiResponse>();

        // GET /reports/pending — 获取待处理举报列表
        group.MapGet("/reports/pending", GetPendingReportsAsync)
            .WithSummary("获取待处理举报列表")
            .WithDescription("管理员获取所有待处理的举报列表，支持分页")
            .Produces<ApiResponse<PagedResult<object>>>();

        // POST /reports/{reportGuid}/resolve — 处理举报
        group.MapPost("/reports/{reportGuid}/resolve", ResolveReportAsync)
            .WithSummary("处理举报")
            .WithDescription("管理员处理指定举报")
            .Accepts<ResolveReportRequest>("application/json")
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

            var tweets = await mediator.SendAsync(new GetPendingTweetsQuery(page, pageSize), ct);

            var result = new PagedResult<object>
            {
                Items = [.. tweets.Select(t => new
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
                TotalCount = tweets.Count(),
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<object>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<PagedResult<object>>.Error($"获取待审核推文失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse.Error($"审核推文通过失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse.Error($"驳回推文失败: {ex.Message}"));
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

            var reports = await mediator.SendAsync(new GetPendingReportsQuery(page, pageSize), ct);

            var result = new PagedResult<object>
            {
                Items = [.. reports.Select(report => new
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
                TotalCount = reports.Count(),
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<object>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<PagedResult<object>>.Error($"获取待处理举报失败: {ex.Message}"));
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
            return Results.Ok(ApiResponse.Error($"处理举报失败: {ex.Message}"));
        }
    }
}
