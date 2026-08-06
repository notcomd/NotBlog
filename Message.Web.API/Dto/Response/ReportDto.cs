namespace Message.Web.API.Dto.Response;
public class ReportDto
{
    public Guid ReportGuid { get; init; }
    public Guid ReporterGuid { get; init; }
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetGuid { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public List<string>? EvidenceUrls { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? ReviewerGuid { get; init; }
    public string? ReviewNote { get; init; }
    public DateTimeOffset? ReviewTime { get; init; }
    public DateTimeOffset CreateTime { get; init; }
}

