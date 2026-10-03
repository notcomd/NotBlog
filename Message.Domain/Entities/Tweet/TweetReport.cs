
namespace Message.Domain.Entities.Tweet;

/// <summary>
/// 推文举报聚合根。
/// </summary>
public class TweetReport : Entity<Guid>, IAggregateRoot
{


    /// <summary>举报ID</summary>
    public Guid ReportGuid { get; init; }
    /// <summary>举报人用户ID</summary>
    public Guid ReporterGuid { get; private set; }
    /// <summary>举报目标类型</summary>
    public ReportTargetType TargetType { get; private set; }
    /// <summary>举报目标ID</summary>
    public Guid TargetGuid { get; private set; }
    /// <summary>被举报用户ID</summary>
    public Guid ReportedUserGuid { get; private set; }
    /// <summary>举报原因</summary>
    public string ReportReason { get; private set; } = null!;
    /// <summary>举报分类</summary>
    public ReportCategory Category { get; private set; }
    /// <summary>证据URL列表</summary>
    public IReadOnlyList<string> EvidenceUrls => _evidenceUrls.AsReadOnly();
    /// <summary>举报状态</summary>
    public ReportStatus Status { get; private set; }
    /// <summary>审核人用户ID</summary>
    public Guid? ReviewerGuid { get; private set; }
    /// <summary>审核备注</summary>
    public string? ReviewNote { get; private set; }
    /// <summary>审核时间</summary>
    public DateTimeOffset? ReviewTime { get; private set; }
    /// <summary>创建时间</summary>
    public DateTimeOffset CreateTime { get; private set; }

    private readonly List<string> _evidenceUrls = [];



    private TweetReport() => ReportGuid = Guid.CreateVersion7();

    /// <summary>创建举报</summary>
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

    /// <summary>开始审核</summary>
    public void StartReview(Guid reviewerGuid)
    {
        if (Status != ReportStatus.Pending)
            throw new InvalidOperationException("只有待处理的举报可以开始审核");

        Status = ReportStatus.Reviewing;
        ReviewerGuid = reviewerGuid;
    }

    /// <summary>审核处置为已移除</summary>
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

    /// <summary>审核处置为已拒绝</summary>
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
