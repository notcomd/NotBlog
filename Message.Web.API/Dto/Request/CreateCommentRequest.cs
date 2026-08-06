namespace Message.Web.API.Dto.Request;
public class CreateCommentRequest
{
    public Guid TweetGuid { get; init; }
    public string Content { get; init; } = string.Empty;
    public Guid? ParentGuid { get; init; }
    public Guid? ReplyToGuid { get; init; }
}

