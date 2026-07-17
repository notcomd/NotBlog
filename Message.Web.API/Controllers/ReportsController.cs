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
[Route("api/reports")]
public class ReportsController(
    IReportProvider reportProvider,
    ICurrentUserService currentUserService,
    ILogger<ReportsController> logger) : ControllerBase
{
    private readonly IReportProvider _reportProvider = reportProvider;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<ReportsController> _logger = logger;

    [HttpPost]
    public async Task<ActionResult<ApiResponse>> SubmitReportAsync([FromBody] SubmitReportRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var targetType = Enum.Parse<ReportTargetType>(request.TargetType);
        var category = Enum.Parse<ReportCategory>(request.Category);

        await _reportProvider.SubmitReportAsync(
            userId, targetType, request.TargetGuid,
            request.Reason, category, request.EvidenceUrls);

        return Ok(ApiResponse.Ok("举报提交成功"));
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse>> GetMyReportsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = _currentUserService.GetUserId();
        var reports = await _reportProvider.GetMyReportsAsync(userId, page, pageSize);

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
}
