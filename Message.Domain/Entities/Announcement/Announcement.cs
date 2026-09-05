namespace Message.Domain.Entities.Announcement;

/// <summary>
/// 系统公报（站点公告）：由管理员创建，可撤回。
/// <para>
/// 事实约束：同一公报仅创建者/管理员可撤回；撤回后不再出现在有效列表。
/// </para>
/// </summary>
public class Announcement : Entity<Guid>, IAggregateRoot
{
    protected Announcement()
    {
        AnnouncementGuid = Guid.CreateVersion7();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>公报 ID</summary>
    public Guid AnnouncementGuid { get; init; }

    /// <summary>标题</summary>
    public string Title { get; private set; } = null!;

    /// <summary>正文</summary>
    public string Content { get; private set; } = null!;

    /// <summary>创建者（管理员）用户 ID</summary>
    public Guid CreatorUserId { get; init; }

    /// <summary>是否已撤回</summary>
    public bool IsRecalled { get; private set; }

    /// <summary>撤回时间</summary>
    public DateTimeOffset? RecalledAt { get; private set; }

    /// <summary>创建时间</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 创建公报
    /// </summary>
    /// <param name="creatorUserId">创建者用户 ID</param>
    /// <param name="title">标题（非空）</param>
    /// <param name="content">正文（非空）</param>
    public static Announcement Create(Guid creatorUserId, string title, string content)
    {
        if (creatorUserId == Guid.Empty)
            throw new ArgumentException("创建者不能为空", nameof(creatorUserId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("公报标题不能为空", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("公报内容不能为空", nameof(content));

        return new Announcement
        {
            CreatorUserId = creatorUserId,
            Title = title.Trim(),
            Content = content.Trim()
        };
    }

    /// <summary>
    /// 撤回公报（幂等：已撤回重复撤回直接返回）
    /// </summary>
    public void Recall()
    {
        if (IsRecalled)
            return;
        IsRecalled = true;
        RecalledAt = DateTimeOffset.UtcNow;
    }
}