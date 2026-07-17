using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.Entities.Tweet;

public class TweetAuditLog:Entity
{
    public Guid AuditGuid { get; init; }
    public Guid TweetGuid { get; private set; }
    public Guid AuditorGuid { get; private set; }
    public AuditAction Action { get; private set; }
    public string Reason { get; private set; }
    public DateTimeOffset AuditTime { get; private set; }

    private TweetAuditLog() => AuditGuid = Guid.CreateVersion7();

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
