using Message.Web.API.Application.Commands.Reports;
using Message.Web.API.Application.Queries.Reports;

namespace Message.Web.API.APIs;

/// <summary>
/// 举报接口（静态函数模式 + CQRS）。
/// <para>
/// 设计约定：
/// - 所有端点处理程序均为<b>静态函数</b>（不使用 Action/Lambda 创建接口）；
/// - 依赖服务通过 <c>[FromServices]</c> 特性注入，生命周期由服务注册文件统一管理；
/// - 请求体使用 <c>[FromBody]</c>，分页参数使用 <c>[FromQuery]</c>；
/// - 数据写操作通过 <see cref="INotMediator"/> 分发到命令处理程序（Commands），
///   命令仅返回操作结果（bool / 新实体 ID），不返回业务实体/DTO；
/// - 数据读操作通过 <see cref="INotMediator"/> 分发到查询处理程序（Queries），
///   查询不修改任何数据状态，仅返回只读结果。
/// </para>
/// </summary>
public static class ReportsApi
{
    /// <summary>映射举报相关端点组</summary>
    public static RouteGroupBuilder MapReportsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        // POST / — 提交举报
        group.MapPost("/", SubmitReportAsync)
            .WithSummary("提交举报")
            .WithDescription("用户对推文或评论提交举报")
            .Accepts<SubmitReportRequest>("application/json")
            .Produces<ApiResponse>();

        // GET /my — 获取我的举报列表
        group.MapGet("/my", GetMyReportsAsync)
            .WithSummary("获取我的举报列表")
            .WithDescription("获取当前用户提交的举报列表，支持分页")
            .Produces<ApiResponse<PagedResult<object>>>();

        return group;
    }

    /// <summary>
    /// 提交举报（命令侧）。
    /// </summary>
    /// <param name="request">举报请求体</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> SubmitReportAsync(
        [FromBody] SubmitReportRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new SubmitReportCommand(
                userId, request.TargetType, request.TargetGuid,
                request.Reason, request.Category, request.EvidenceUrls), ct);

            return Results.Ok(ApiResponse.Ok("举报提交成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"提交举报失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取当前用户提交的举报列表（查询侧，分页）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页举报列表</returns>
    private static async Task<IResult> GetMyReportsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var paged = await mediator.SendAsync(new GetMyReportsQuery(userId, page, pageSize), ct);

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
            return Results.Json(ApiResponse<PagedResult<object>>.Error($"获取我的举报列表失败: {ex.Message}"), statusCode: 500);
        }
    }
}
