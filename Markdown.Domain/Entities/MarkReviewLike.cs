namespace Markdown.Domain.Entities;

/// <summary>
///     评论点赞记录（用于点赞去重：同一用户对同一评论仅能点赞一次，
///     通过 (MarkReviewGuid, UserId) 唯一约束保证并发安全）
/// </summary>
public class MarkReviewLike
{
    public int Id { get; private set; }

    public Guid MarkReviewGuid { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreateAt { get; private set; }

    private MarkReviewLike()
    {
    }

    public MarkReviewLike(Guid markReviewGuid, Guid userId)
    {
        MarkReviewGuid = markReviewGuid;
        UserId = userId;
        CreateAt = DateTimeOffset.UtcNow;
    }
}
