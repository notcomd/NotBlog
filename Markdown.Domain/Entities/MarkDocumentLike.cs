namespace Markdown.Domain.Entities;

/// <summary>
///     文档点赞记录（点赞去重：同一用户对同一文档仅能点赞一次，
///     通过 (MarkDownGuid, UserId) 唯一约束保证并发安全）
/// </summary>
public class MarkDocumentLike
{
    public int Id { get; private set; }

    public Guid MarkDownGuid { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreateAt { get; private set; }

    private MarkDocumentLike()
    {
    }

    public MarkDocumentLike(Guid markDownGuid, Guid userId)
    {
        MarkDownGuid = markDownGuid;
        UserId = userId;
        CreateAt = DateTimeOffset.UtcNow;
    }
}
