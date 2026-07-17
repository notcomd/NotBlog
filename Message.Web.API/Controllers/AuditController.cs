using Message.Domain.Dto;
using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IServices;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Request;
using Microsoft.AspNetCore.Mvc;

namespace Message.Web.API.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditController(
    IAuditProvider auditProvider,
    ICurrentUserService currentUserService,
    ILogger<AuditController> logger) : ControllerBase
{
    private readonly IAuditProvider _auditProvider = auditProvider;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<AuditController> _logger = logger;

    [HttpGet("tweets/pending")]
    public async Task<ActionResult<ApiResponse<PagedResult<object>>>> GetPendingTweetsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!_currentUserService.IsAdmin())
            return StatusCode(403, ApiResponse.Forbidden("仅管理员可执行审核操作"));

        var tweets = await _auditProvider.GetPendingTweetsAsync(page, pageSize);

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

        return Ok(ApiResponse<PagedResult<object>>.Ok(result));
    }

    [HttpPost("tweets/{tweetGuid}/approve")]
    public async Task<ActionResult<ApiResponse>> ApproveTweetAsync(Guid tweetGuid)
    {
        if (!_currentUserService.IsAdmin())
            return StatusCode(403, ApiResponse.Forbidden("仅管理员可执行审核操作"));

        var auditorGuid = _currentUserService.GetUserId();
        await _auditProvider.ApproveTweetAsync(tweetGuid, auditorGuid);

        return Ok(ApiResponse.Ok("推文已通过审核"));
    }

    [HttpPost("tweets/{tweetGuid}/reject")]
    public async Task<ActionResult<ApiResponse>> RejectTweetAsync(
        Guid tweetGuid, [FromBody] AuditActionRequest request)
    {
        if (!_currentUserService.IsAdmin())
            return StatusCode(403, ApiResponse.Forbidden("仅管理员可执行审核操作"));

        var auditorGuid = _currentUserService.GetUserId();
        await _auditProvider.RejectTweetAsync(tweetGuid, auditorGuid, request.Reason);

        return Ok(ApiResponse.Ok("推文已驳回"));
    }

    [HttpGet("reports/pending")]
    public async Task<ActionResult<ApiResponse<PagedResult<object>>>> GetPendingReportsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!_currentUserService.IsAdmin())
            return StatusCode(403, ApiResponse.Forbidden("仅管理员可执行审核操作"));

        var reports = await _auditProvider.GetPendingReportsAsync(page, pageSize);

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

        return Ok(ApiResponse<PagedResult<object>>.Ok(result));
    }

    [HttpPost("reports/{reportGuid}/resolve")]
    public async Task<ActionResult<ApiResponse>> ResolveReportAsync(
        Guid reportGuid, [FromBody] ResolveReportRequest request)
    {
        if (!_currentUserService.IsAdmin())
            return StatusCode(403, ApiResponse.Forbidden("仅管理员可执行审核操作"));

        var reviewerGuid = _currentUserService.GetUserId();
        var isContentRemoved = request.Action?.ToLower() == "removed";

        await _auditProvider.ResolveReportAsync(reportGuid, reviewerGuid, request.Note, isContentRemoved);

        return Ok(ApiResponse.Ok("举报已处理"));
    }
}
