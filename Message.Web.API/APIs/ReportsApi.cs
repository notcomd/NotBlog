namespace Message.Web.API.APIs;

public static class ReportsApi
{
    public static RouteGroupBuilder MapReportsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports");

        group.MapPost("/", async (
            [FromBody] SubmitReportRequest request,
            IReportProvider reportProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ReportsApi");
            try
            {
                var userId = currentUserService.GetUserId();

                await reportProvider.SubmitReportAsync(
                    userId, request.TargetType, request.TargetGuid,
                    request.Reason, request.Category, request.EvidenceUrls);

                return Results.Ok(ApiResponse.Ok("举报提交成功"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "提交举报失败");
                return Results.Ok(ApiResponse.Error("提交举报失败"));
            }
        })
        .WithSummary("提交举报")
        .WithDescription("用户对推文或评论提交举报")
        .Accepts<SubmitReportRequest>("application/json")
        .Produces<ApiResponse>();

        group.MapGet("/my", async (
            IReportProvider reportProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var logger = loggerFactory.CreateLogger("ReportsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var reports = await reportProvider.GetMyReportsAsync(userId, page, pageSize);

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
                    Total = reports.Count(),
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<object>>.Ok(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取我的举报列表失败");
                return Results.Ok(ApiResponse<PagedResult<object>>.Error("获取我的举报列表失败"));
            }
        })
        .WithSummary("获取我的举报列表")
        .WithDescription("获取当前用户提交的举报列表，支持分页")
        .Produces<ApiResponse<PagedResult<object>>>();

        return group;
    }
}
