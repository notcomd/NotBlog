namespace Markdown.Domain.Entities;

/// <summary>
///     文档打赏（硬币）记录：用户对文档的硬币打赏流水，用于文档硬币计数与审计
/// </summary>
public class MarkCoin
{
    public int Id { get; private set; }

    public Guid MarkDownGuid { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>
    ///     打赏硬币数量（正数）
    /// </summary>
    public long Amount { get; private set; }

    public DateTimeOffset CreateAt { get; private set; }

    private MarkCoin()
    {
    }

    public MarkCoin(Guid markDownGuid, Guid userId, long amount)
    {
        if (markDownGuid == Guid.Empty)
            throw new ArgumentException("文档标识不能为空", nameof(markDownGuid));
        if (userId == Guid.Empty)
            throw new ArgumentException("用户标识不能为空", nameof(userId));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "打赏数量必须为正数");

        MarkDownGuid = markDownGuid;
        UserId = userId;
        Amount = amount;
        CreateAt = DateTimeOffset.UtcNow;
    }
}
