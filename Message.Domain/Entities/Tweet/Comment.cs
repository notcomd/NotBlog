
namespace Message.Domain.Entities.Tweet;

public class Comment : Entity<Guid>, IAggregateRoot
{
    public Guid CommentGuid { get; init; }
    public Guid TweetGuid { get; private set; }
    public Guid UserGuid { get; private set; }
    public Guid? ParentGuid { get; private set; }
    public Guid? ReplyToGuid { get; private set; }
    public string Content { get; private set; }
    public int LikeCount { get; private set; }
    public int ReplyCount { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreateTime { get; private set; }

    private Comment()
    {
        CommentGuid = Guid.CreateVersion7();
        Content = string.Empty;
    }

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

    public void SoftDelete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("评论已被删除");
        IsDeleted = true;
    }

    public void IncrementReplyCount()
    {
        if (IsDeleted)
            throw new InvalidOperationException("评论已被删除，无法新增回复");
        ReplyCount++;
    }

    public void AddLike()
    {
        if (IsDeleted)
            throw new InvalidOperationException("评论已被删除，无法点赞");
        LikeCount++;
    }

    public void RemoveLike()
    {
        if (IsDeleted)
            throw new InvalidOperationException("评论已被删除，无法取消点赞");
        if (LikeCount <= 0)
            throw new InvalidOperationException("点赞数不能小于0");
        LikeCount--;
    }
}
