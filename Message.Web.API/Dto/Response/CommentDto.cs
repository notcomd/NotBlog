namespace Message.Web.API.Dto.Response;
public class CommentDto
{
    public Guid CommentGuid { get; init; }
    public Guid TweetGuid { get; init; }
    public UserBriefDto User { get; init; } = null!;
    public Guid? ParentGuid { get; init; }
    public Guid? ReplyToGuid { get; init; }
    public string Content { get; init; } = string.Empty;
    public int LikeCount { get; init; }
    public int ReplyCount { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset CreateTime { get; init; }
}

