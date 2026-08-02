using Message.Domain.Enums;
using Message.Domain.Events;
using Message.Domain.SeedWork;
using NotMediator;

namespace Message.Domain.Entities.Tweet;

public class TweetReport : Entity, IAggregateRoot
{


    public Guid ReportGuid { get; init; }
    public Guid ReporterGuid { get; private set; }
    public ReportTargetType TargetType { get; private set; }
    public Guid TargetGuid { get; private set; }
    public Guid ReportedUserGuid { get; private set; }
    public string ReportReason { get; private set; } = null!;
    public ReportCategory Category { get; private set; }
    public IReadOnlyList<string> EvidenceUrls => _evidenceUrls.AsReadOnly();
    public ReportStatus Status { get; private set; }
    public Guid? ReviewerGuid { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTimeOffset? ReviewTime { get; private set; }
    public DateTimeOffset CreateTime { get; private set; }

    private readonly List<string> _evidenceUrls = [];



    private TweetReport() => ReportGuid = Guid.CreateVersion7();

    public static TweetReport Create(
        Guid reporterGuid, ReportTargetType targetType, Guid targetGuid,
        Guid reportedUserGuid, string reason, ReportCategory category,
        IEnumerable<string>? evidenceUrls = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("举报原因不能为空", nameof(reason));

        var report = new TweetReport
        {
            ReporterGuid = reporterGuid,
            TargetType = targetType,
            TargetGuid = targetGuid,
            ReportedUserGuid = reportedUserGuid,
            ReportReason = reason,
            Category = category,
            Status = ReportStatus.Pending,
            CreateTime = DateTimeOffset.UtcNow
        };

        if (evidenceUrls != null)
        {
            foreach (var url in evidenceUrls)
            {
                if (report._evidenceUrls.Count >= 5)
                    throw new ArgumentException("证据URL数量不能超过5个", nameof(evidenceUrls));
                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentException("证据URL不能为空", nameof(evidenceUrls));
                report._evidenceUrls.Add(url);
            }
        }

        report.AddDomainEvent(new TweetReportedEvent(
            report.ReportGuid, report.ReporterGuid, report.TargetType,
            report.TargetGuid, report.ReportReason));

        return report;
    }

    public void StartReview(Guid reviewerGuid)
    {
        if (Status != ReportStatus.Pending)
            throw new InvalidOperationException("只有待处理的举报可以开始审核");

        Status = ReportStatus.Reviewing;
        ReviewerGuid = reviewerGuid;
    }

    public void ResolveRemoved(Guid reviewerGuid, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("审核备注不能为空", nameof(note));

        if (Status != ReportStatus.Reviewing)
            throw new InvalidOperationException("只有审核中的举报可以标记为已移除");

        Status = ReportStatus.Resolved_Removed;
        ReviewNote = note;
        ReviewTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new ReportResolvedEvent(
            ReportGuid, reviewerGuid, ReportStatus.Resolved_Removed,
            note, ReporterGuid, ReportedUserGuid));
    }

    public void ResolveRejected(Guid reviewerGuid, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("审核备注不能为空", nameof(note));

        if (Status != ReportStatus.Reviewing)
            throw new InvalidOperationException("只有审核中的举报可以标记为已拒绝");

        Status = ReportStatus.Resolved_Rejected;
        ReviewNote = note;
        ReviewTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new ReportResolvedEvent(
            ReportGuid, reviewerGuid, ReportStatus.Resolved_Rejected,
            note, ReporterGuid, ReportedUserGuid));
    }


}
