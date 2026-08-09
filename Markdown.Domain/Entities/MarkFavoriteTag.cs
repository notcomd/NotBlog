namespace Markdown.Domain.Entities;

/// <summary>
///     用户收藏标签库（标签复用）：记录用户打标签时使用过的标签，
///     下次打标签可直接选择复用，无需再次手打。
///     同一用户同一标签仅一条记录（(UserGuid, Tag) 唯一约束），
///     UseCount / LastUsedAt 用于"常用标签"排序建议。
///     取消收藏或移除标签不影响标签库（历史标签保留，保证可复用）。
/// </summary>
public class MarkFavoriteTag : Entity<int>
{
    /// <summary>单标签最大长度（与收藏标签规则一致）</summary>
    public const int MaxTagLength = 50;

    private MarkFavoriteTag()
    {
        MarkFavoriteTagGuid = Guid.CreateVersion7();
        CreateAt = DateTimeOffset.UtcNow;
        UseCount = 0;
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    private MarkFavoriteTag(Guid userGuid, string tag) : this()
    {
        UserGuid = userGuid;
        Tag = tag;
        UseCount = 1;
    }

    /// <summary>
    ///     创建标签库记录（首次使用即计数 1）
    /// </summary>
    /// <exception cref="ArgumentException">用户标识为空或标签非法时抛出</exception>
    public static MarkFavoriteTag Create(Guid userGuid, string tag)
    {
        if (userGuid == Guid.Empty)
            throw new ArgumentException("用户标识不能为空", nameof(userGuid));
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("标签不能为空", nameof(tag));

        var trimmed = tag.Trim();
        if (trimmed.Length > MaxTagLength)
            throw new ArgumentException($"标签长度不能超过 {MaxTagLength} 个字符");

        return new MarkFavoriteTag(userGuid, trimmed);
    }

    public Guid MarkFavoriteTagGuid { get; init; }

    /// <summary>
    ///     标签所属用户 GUID
    /// </summary>
    public Guid UserGuid { get; init; }

    /// <summary>
    ///     标签文本
    /// </summary>
    public string Tag { get; private set; } = null!;

    /// <summary>
    ///     使用次数（打标签命中次数，用于常用标签排序）
    /// </summary>
    public int UseCount { get; private set; }

    /// <summary>
    ///     最近使用时间（用于常用标签排序）
    /// </summary>
    public DateTimeOffset LastUsedAt { get; private set; }

    /// <summary>
    ///     首次使用时间
    /// </summary>
    public DateTimeOffset CreateAt { get; init; }

    /// <summary>
    ///     记录一次使用（计数 +1 并刷新最近使用时间）
    /// </summary>
    public void RecordUsage()
    {
        UseCount++;
        LastUsedAt = DateTimeOffset.UtcNow;
    }
}
