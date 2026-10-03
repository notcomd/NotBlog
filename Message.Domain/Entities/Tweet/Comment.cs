
namespace Message.Domain.Entities.Tweet;

/// <summary>
/// 评论聚合根。
/// </summary>
public class Comment : Entity<Guid>, IAggregateRoot
{
    /// <summary>评论ID</summary>
    public Guid CommentGuid { get; init; }
    /// <summary>所属推文ID</summary>
    public Guid TweetGuid { get; private set; }
    /// <summary>评论者用户ID</summary>
    public Guid UserGuid { get; private set; }
    /// <summary>父评论ID（顶级评论为 null）</summary>
    public Guid? ParentGuid { get; private set; }
    /// <summary>回复的目标评论ID</summary>
    public Guid? ReplyToGuid { get; private set; }
    /// <summary>评论内容</summary>
    public string Content { get; private set; }
    /// <summary>点赞数</summary>
    public int LikeCount { get; private set; }
    /// <summary>回复数</summary>
    public int ReplyCount { get; private set; }
    /// <summary>是否已删除</summary>
    public bool IsDeleted { get; private set; }
    /// <summary>创建时间</summary>
    public DateTimeOffset CreateTime { get; private set; }

    private Comment()
    {
        CommentGuid = Guid.CreateVersion7();
        Content = string.Empty;
    }

    /// <summary>创建评论</summary>
    public static Comment Create(Guid tweetGuid, Guid userGuid, string content, Guid? parentGuid = null, Guid? replyToGuid = null)
    {
        if (tweetGuid == Guid.Empty)
            throw new ArgumentException("推文ID不能为空", nameof(tweetGuid));
        if (userGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userGuid));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("评论内容不能为空", nameof(content));
        if (content.Length > 500)
            throw new ArgumentException("评论内容不能超过500个字符", nameof(content));

        var comment = new Comment
        {
            TweetGuid = tweetGuid,
            UserGuid = userGuid,
            Content = content,
            ParentGuid = parentGuid,
            ReplyToGuid = replyToGuid,
            LikeCount = 0,
            ReplyCount = 0,
            IsDeleted = false,
            CreateTime = DateTimeOffset.UtcNow
        };

        comment.AddDomainEvent(new CommentAddedEvent(comment.CommentGuid, tweetGuid, userGuid, parentGuid));

        return comment;
    }

    /// <summary>软删除评论</summary>
    public void SoftDelete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("评论已被删除");
        IsDeleted = true;
    }

    /// <summary>回复数 +1</summary>
    public void IncrementReplyCount()
    {
        if (IsDeleted)
            throw new InvalidOperationException("评论已被删除，无法新增回复");
        ReplyCount++;
    }

    /// <summary>点赞数 +1</summary>
    public void AddLike()
    {
        if (IsDeleted)
            throw new InvalidOperationException("评论已被删除，无法点赞");
        LikeCount++;
    }

    /// <summary>点赞数 -1</summary>
    public void RemoveLike()
    {
        if (IsDeleted)
            throw new InvalidOperationException("评论已被删除，无法取消点赞");
        if (LikeCount <= 0)
            throw new InvalidOperationException("点赞数不能小于0");
        LikeCount--;
    }
}
