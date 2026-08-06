
namespace Message.Domain.Events;

public record TweetReportedEvent(
    Guid ReportGuid,
    Guid ReporterGuid,
    ReportTargetType TargetType,
    Guid TargetGuid,
    string Reason) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
