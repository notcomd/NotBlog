namespace Markdown.Domain.Entities;

/// <summary>
///     评论踩记录（踩去重：同一用户对同一评论仅能踩一次，
///     通过 (MarkReviewGuid, UserId) 唯一约束保证并发安全）
/// </summary>
public class MarkReviewDislike
{
    public int Id { get; private set; }

    public Guid MarkReviewGuid { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreateAt { get; private set; }

    private MarkReviewDislike()
    {
    }

    public MarkReviewDislike(Guid markReviewGuid, Guid userId)
    {
        MarkReviewGuid = markReviewGuid;
        UserId = userId;
        CreateAt = DateTimeOffset.UtcNow;
    }
}
