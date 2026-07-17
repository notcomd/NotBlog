using Message.Domain.Enums;

namespace Message.Domain.Entities.Tweet;

public class TweetInteraction
{
    public Guid InteractGuid { get; init; }
    public Guid TweetGuid { get; private set; }
    public Guid UserGuid { get; private set; }
    public InteractionType Type { get; private set; }
    public DateTimeOffset CreateTime { get; private set; }

    private TweetInteraction()
    {
        InteractGuid = Guid.CreateVersion7();
    }

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
