
namespace Message.Domain.Entities.Tweet;

/// <summary>
/// 推文审核日志（普通实体）。
/// </summary>
public class TweetAuditLog:Entity<Guid>
{
    /// <summary>审核记录ID</summary>
    public Guid AuditGuid { get; init; }
    /// <summary>推文ID</summary>
    public Guid TweetGuid { get; private set; }
    /// <summary>审核员用户ID</summary>
    public Guid AuditorGuid { get; private set; }
    /// <summary>审核动作</summary>
    public AuditAction Action { get; private set; }
    /// <summary>审核原因</summary>
    public string Reason { get; private set; } = null!;
    /// <summary>审核时间</summary>
    public DateTimeOffset AuditTime { get; private set; }

    private TweetAuditLog() => AuditGuid = Guid.CreateVersion7();

    /// <summary>创建审核日志</summary>
    public static TweetAuditLog Create(Guid tweetGuid, Guid auditorGuid, AuditAction action, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("审核原因不能为空", nameof(reason));

        return new TweetAuditLog
        {
            TweetGuid = tweetGuid,
            AuditorGuid = auditorGuid,
            Action = action,
            Reason = reason,
            AuditTime = DateTimeOffset.UtcNow
        };
    }
}
