
namespace Message.Domain.Events;

/// <summary>举报处理完成事件。</summary>
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
