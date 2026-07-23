namespace Message.Web.API.APIs;

public static class AuditApi
{
    public static RouteGroupBuilder MapAuditApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit")
            .WithTags("Audit");

        group.MapGet("/tweets/pending", async (
            IAuditProvider auditProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var logger = loggerFactory.CreateLogger("AuditApi");
            try
            {
                if (!currentUserService.IsAdmin())
                    return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

                var tweets = await auditProvider.GetPendingTweetsAsync(page, pageSize);

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
                    Total = tweets.Count(),
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<object>>.Ok(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取待审核推文失败");
                return Results.Ok(ApiResponse<PagedResult<object>>.Error("获取待审核推文失败"));
            }
        })
        .WithSummary("获取待审核推文列表")
        .WithDescription("管理员获取所有待审核的推文列表，支持分页")
        .Produces<ApiResponse<PagedResult<object>>>();

        group.MapPost("/tweets/{tweetGuid}/approve", async (
            Guid tweetGuid,
            IAuditProvider auditProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuditApi");
            try
            {
                if (!currentUserService.IsAdmin())
                    return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

                var auditorGuid = currentUserService.GetUserId();
                await auditProvider.ApproveTweetAsync(tweetGuid, auditorGuid);

                return Results.Ok(ApiResponse.Ok("推文已通过审核"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "审核推文通过失败");
                return Results.Ok(ApiResponse.Error("审核推文通过失败"));
            }
        })
        .WithSummary("通过推文审核")
        .WithDescription("管理员通过指定推文的审核")
        .Produces<ApiResponse>();

        group.MapPost("/tweets/{tweetGuid}/reject", async (
            Guid tweetGuid,
            [FromBody] AuditActionRequest request,
            IAuditProvider auditProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuditApi");
            try
            {
                if (!currentUserService.IsAdmin())
                    return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

                var auditorGuid = currentUserService.GetUserId();
                await auditProvider.RejectTweetAsync(tweetGuid, auditorGuid, request.Reason);

                return Results.Ok(ApiResponse.Ok("推文已驳回"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "驳回推文失败");
                return Results.Ok(ApiResponse.Error("驳回推文失败"));
            }
        })
        .WithSummary("驳回推文")
        .WithDescription("管理员驳回指定推文，需提供驳回原因")
        .Accepts<AuditActionRequest>("application/json")
        .Produces<ApiResponse>();

        group.MapGet("/reports/pending", async (
            IAuditProvider auditProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var logger = loggerFactory.CreateLogger("AuditApi");
            try
            {
                if (!currentUserService.IsAdmin())
                    return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

                var reports = await auditProvider.GetPendingReportsAsync(page, pageSize);

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
                logger.LogError(ex, "获取待处理举报失败");
                return Results.Ok(ApiResponse<PagedResult<object>>.Error("获取待处理举报失败"));
            }
        })
        .WithSummary("获取待处理举报列表")
        .WithDescription("管理员获取所有待处理的举报列表，支持分页")
        .Produces<ApiResponse<PagedResult<object>>>();

        group.MapPost("/reports/{reportGuid}/resolve", async (
            Guid reportGuid,
            [FromBody] ResolveReportRequest request,
            IAuditProvider auditProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuditApi");
            try
            {
                if (!currentUserService.IsAdmin())
                    return Results.Json(ApiResponse.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

                var reviewerGuid = currentUserService.GetUserId();
                var isContentRemoved = request.Action?.ToLower() == "removed";

                await auditProvider.ResolveReportAsync(reportGuid, reviewerGuid, request.Note, isContentRemoved);

                return Results.Ok(ApiResponse.Ok("举报已处理"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理举报失败");
                return Results.Ok(ApiResponse.Error("处理举报失败"));
            }
        })
        .WithSummary("处理举报")
        .WithDescription("管理员处理指定举报")
        .Accepts<ResolveReportRequest>("application/json")
        .Produces<ApiResponse>();

        return group;
    }
}
