namespace Message.Web.API.Dto.Response;
public class AuditLogDto
{
    public Guid AuditGuid { get; init; }
    public Guid TweetGuid { get; init; }
    public Guid AuditorGuid { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset AuditTime { get; init; }
}
