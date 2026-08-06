namespace Message.Web.API.Dto.Request;
public class SubmitReportRequest
{
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetGuid { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public List<string>? EvidenceUrls { get; init; }
}

