
namespace Message.Domain.Events;

/// <summary>内容被举报事件。</summary>
public record TweetReportedEvent(
    Guid ReportGuid,
    Guid ReporterGuid,
    ReportTargetType TargetType,
    Guid TargetGuid,
    string Reason) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
