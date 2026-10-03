
namespace Message.Domain.Entities.Tweet;

/// <summary>
/// 推文互动记录（点赞/收藏/投币等，普通实体）。
/// </summary>
public class TweetInteraction : Entity<Guid>
{
    /// <summary>推文ID</summary>
    public Guid TweetGuid { get; private set; }
    /// <summary>互动用户ID</summary>
    public Guid UserGuid { get; private set; }
    /// <summary>互动类型</summary>
    public InteractionType Type { get; private set; }
    /// <summary>创建时间</summary>
    public DateTimeOffset CreateTime { get; private set; }

    private TweetInteraction()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>创建推文互动记录</summary>
    public static TweetInteraction Create(Guid tweetGuid, Guid userGuid, InteractionType type)
    {
        if (tweetGuid == Guid.Empty)
            throw new ArgumentException("推文ID不能为空", nameof(tweetGuid));
        if (userGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userGuid));

        return new TweetInteraction
        {
            TweetGuid = tweetGuid,
            UserGuid = userGuid,
            Type = type,
            CreateTime = DateTimeOffset.UtcNow
        };
    }
}
