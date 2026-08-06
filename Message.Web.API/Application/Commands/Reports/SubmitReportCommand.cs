namespace Message.Web.API.Application.Commands.Reports;
/// <summary>
/// 提交举报命令。
/// </summary>
/// <param name="UserId">举报者用户 ID</param>
/// <param name="TargetType">举报对象类型（Tweet/Comment）</param>
/// <param name="TargetGuid">举报对象 ID</param>
/// <param name="Reason">举报原因</param>
/// <param name="Category">举报分类</param>
/// <param name="EvidenceUrls">证据 URL 集合</param>
public record SubmitReportCommand(
    Guid UserId,
    string TargetType,
    Guid TargetGuid,
    string Reason,
    string Category,
    IEnumerable<string>? EvidenceUrls) : IRequest<bool>;

