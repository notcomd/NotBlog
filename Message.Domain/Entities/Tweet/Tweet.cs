
namespace Message.Domain.Entities.Tweet;

public class Tweet : Entity<Guid>, IAggregateRoot
{
    private readonly List<TweetMedia> _media = [];
    private readonly HashSet<string> _hashtags = [];

    private Tweet()
    {
        TweetGuid = Guid.CreateVersion7();
        TweetStatus = TweetStatus.Draft;
        CreateTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    public Guid TweetGuid { get; init; }
    public Guid AuthorGuid { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public IReadOnlyList<TweetMedia> Media => _media.AsReadOnly();
    public LinkMetadata? LinkMetadata { get; private set; }
    public IReadOnlySet<string> Hashtags => _hashtags;
    /// <summary>
    /// 所属圈子（圈子帖非空；仅圈子成员可见、可互动）
    /// </summary>
    public Guid? CircleGuid { get; private set; }
    /// <summary>
    /// 话题GUID存储列（逗号分隔的 Guid N 格式，供 LIKE 查询）
    /// </summary>
    public string TopicGuidsJson { get; private set; } = string.Empty;
    /// <summary>
    /// 关联话题列表（逻辑视图）
    /// </summary>
    public IReadOnlyList<Guid> TopicGuids =>
        string.IsNullOrWhiteSpace(TopicGuidsJson)
            ? []
            : TopicGuidsJson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();
    public TweetStatus TweetStatus { get; private set; }
    public Visibility Visibility { get; private set; }
    public bool IsPinned { get; private set; }
    public long ViewCount { get; private set; }
    public int LikeCount { get; private set; }
    public int CommentCount { get; private set; }
    public int ShareCount { get; private set; }
    public int CoinCount { get; private set; }
    public int FavoriteCount { get; private set; }
    public long HotScore { get; private set; }
    public string? AuditReason { get; private set; }
    public DateTimeOffset? PublishTime { get; private set; }
    public DateTimeOffset CreateTime { get; private set; }
    public DateTimeOffset UpdateTime { get; private set; }

    public static Tweet Create(
        Guid authorGuid, string content,
        IEnumerable<TweetMedia>? media = null,
        LinkMetadata? linkMetadata = null,
        IEnumerable<string>? hashtags = null,
        Visibility visibility = Visibility.Public,
        Guid? circleGuid = null,
        IEnumerable<Guid>? topicGuids = null)
    {
        if (authorGuid == Guid.Empty)
            throw new ArgumentException("作者ID不能为空", nameof(authorGuid));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("推文内容不能为空", nameof(content));

        if (content.Length > 2000)
            throw new ArgumentException("推文内容不能超过2000个字符", nameof(content));

        var tweet = new Tweet
        {
            AuthorGuid = authorGuid,
            Content = content,
            LinkMetadata = linkMetadata,
            Visibility = visibility,
            TweetStatus = TweetStatus.Draft,
            CircleGuid = circleGuid
        };

        if (topicGuids is not null)
            tweet.SetTopics(topicGuids);

        if (media is not null)
        {
            var mediaList = media.ToList();
            if (mediaList.Count > 9)
                throw new ArgumentException("推文最多只能包含9个媒体文件", nameof(media));
            tweet._media.AddRange(mediaList);
        }

        if (hashtags is not null)
        {
            foreach (var tag in hashtags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                    tweet._hashtags.Add(tag);
            }
        }

        tweet.AddDomainEvent(new TweetCreatedEvent(tweet.TweetGuid, authorGuid));
        return tweet;
    }

    public void Publish()
    {
        if (TweetStatus != TweetStatus.Draft)
            throw new InvalidOperationException("只有草稿状态的推文才能发布");

        TweetStatus = TweetStatus.Pending;
        PublishTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new TweetPublishedEvent(TweetGuid, AuthorGuid));
    }

    public void Approve(Guid auditorGuid)
    {
        if (auditorGuid == Guid.Empty)
            throw new ArgumentException("审核员ID不能为空", nameof(auditorGuid));

        if (TweetStatus != TweetStatus.Pending)
            throw new InvalidOperationException("只有待审核状态的推文才能通过审核");

        TweetStatus = TweetStatus.Approved;
        UpdateTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new TweetApprovedEvent(TweetGuid, AuthorGuid, auditorGuid));
    }

    public void Reject(Guid auditorGuid, string reason)
    {
        if (auditorGuid == Guid.Empty)
            throw new ArgumentException("审核员ID不能为空", nameof(auditorGuid));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("驳回原因不能为空", nameof(reason));

        if (TweetStatus != TweetStatus.Pending)
            throw new InvalidOperationException("只有待审核状态的推文才能驳回");

        TweetStatus = TweetStatus.Rejected;
        AuditReason = reason;
        UpdateTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new TweetRejectedEvent(TweetGuid, AuthorGuid, auditorGuid, reason));
    }

    public void UpdateContent(string content)
    {
        if (TweetStatus != TweetStatus.Draft)
            throw new InvalidOperationException("只有草稿状态的推文才能修改内容");

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("推文内容不能为空", nameof(content));

        if (content.Length > 2000)
            throw new ArgumentException("推文内容不能超过2000个字符", nameof(content));

        Content = content;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    public void AddLike()
    {
        LikeCount++;
    }

    public void RemoveLike()
    {
        if (LikeCount > 0)
            LikeCount--;
    }

    public void AddFavorite()
    {
        FavoriteCount++;
    }

    public void RemoveFavorite()
    {
        if (FavoriteCount > 0)
            FavoriteCount--;
    }

    public void AddShare()
    {
        ShareCount++;
    }

    public void AddCoin()
    {
        CoinCount++;
    }

    public void AddComment()
    {
        CommentCount++;
    }

    public void RemoveComment()
    {
        if (CommentCount > 0)
            CommentCount--;
    }

    public void RecalculateHotScore()
    {
        HotScore = (long)(LikeCount * 0.2
                   + FavoriteCount * 0.2
                   + CoinCount * 0.2
                   + ShareCount * 0.2
                   + ViewCount * 0.2);
    }

    public void Pin()
    {
        IsPinned = true;
    }

    public void Unpin()
    {
        IsPinned = false;
    }

    public void SetUpdateTime()
    {
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 设置关联话题（最多 10 个）
    /// </summary>
    public void SetTopics(IEnumerable<Guid> topicGuids)
    {
        var list = (topicGuids ?? []).Distinct().ToList();
        if (list.Count > 10)
            throw new ArgumentException("帖子最多关联10个话题", nameof(topicGuids));
        TopicGuidsJson = string.Join(",", list.Select(g => g.ToString("N")));
    }

    /// <summary>
    /// 发布到圈子（圈子帖免审核，发布即 Approved）。
    /// <para>仅在圈子帖草稿上调用；触发 <see cref="CirclePostPublishedEvent"/> 供实时推送/通知/事件总线消费。</para>
    /// </summary>
    public void PublishInCircle()
    {
        if (TweetStatus != TweetStatus.Draft)
            throw new InvalidOperationException("只有草稿状态的推文才能发布");

        if (CircleGuid is null || CircleGuid == Guid.Empty)
            throw new InvalidOperationException("圈子帖必须指定所属圈子");

        TweetStatus = TweetStatus.Approved;
        PublishTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new CirclePostPublishedEvent(TweetGuid, AuthorGuid, CircleGuid.Value));
    }
}
