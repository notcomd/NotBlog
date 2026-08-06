
namespace Message.Domain.Events;

public record ReportResolvedEvent(
    Guid ReportGuid,
    Guid ReviewerGuid,
    ReportStatus ResultStatus,
    string Note,
    Guid ReporterGuid,
    Guid ReportedUserGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
