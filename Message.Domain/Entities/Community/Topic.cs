
namespace Message.Domain.Entities.Community;

/// <summary>
/// 话题（聚合根）：全局轻量标签，任何用户可创建，帖子可关联多个话题。
/// </summary>
public class Topic : Entity<Guid>, IAggregateRoot
{
    private Topic()
    {
        TopicGuid = Guid.CreateVersion7();
        CreateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建话题（名称唯一性由仓储层保证）</summary>
    public static Topic Create(string name, string? description, Guid creatorGuid)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("话题名称不能为空", nameof(name));
        if (name.Length > 30)
            throw new ArgumentException("话题名称不能超过30个字符", nameof(name));
        if (creatorGuid == Guid.Empty)
            throw new ArgumentException("创建者ID不能为空", nameof(creatorGuid));

        var topic = new Topic
        {
            Name = name.Trim(),
            Description = description,
            CreatorGuid = creatorGuid,
            PostCount = 0,
            IsActive = true
        };
        topic.AddDomainEvent(new TopicCreatedEvent(topic.TopicGuid, topic.Name, creatorGuid));
        return topic;
    }

    public Guid TopicGuid { get; init; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid CreatorGuid { get; private set; }
    /// <summary>帖子数（冗余计数）</summary>
    public int PostCount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreateTime { get; init; }

    public void IncrementPostCount()
    {
        PostCount++;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
