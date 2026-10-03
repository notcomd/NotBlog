
namespace Message.Domain.Entities.Community;

/// <summary>
/// 全局关注关系（关注者 → 被关注者）。
/// <para>独立于圈子成员关系；关注 Feed 聚合"我 + 我关注的人"的全局帖。</para>
/// </summary>
public class UserFollow : Entity<Guid>, IAggregateRoot
{
    private UserFollow()
    {
        Id = Guid.CreateVersion7();
        CreateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建关注关系</summary>
    public static UserFollow Create(Guid followerGuid, Guid followeeGuid)
    {
        if (followerGuid == Guid.Empty)
            throw new ArgumentException("关注者ID不能为空", nameof(followerGuid));
        if (followeeGuid == Guid.Empty)
            throw new ArgumentException("被关注者ID不能为空", nameof(followeeGuid));
        if (followerGuid == followeeGuid)
            throw new InvalidOperationException("不能关注自己");

        return new UserFollow
        {
            FollowerGuid = followerGuid,
            FolloweeGuid = followeeGuid
        };
    }

    /// <summary>关注者用户ID</summary>
    public Guid FollowerGuid { get; private set; }
    /// <summary>被关注者用户ID</summary>
    public Guid FolloweeGuid { get; private set; }
    /// <summary>创建时间</summary>
    public DateTimeOffset CreateTime { get; private set; }
}
