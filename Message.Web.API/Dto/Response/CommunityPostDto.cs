namespace Message.Web.API.Dto.Response;

/// <summary>
/// 社区帖子 DTO（圈子帖 + 话题关联；媒体/互动字段与 TweetDto 一致）
/// </summary>
public class CommunityPostDto
{
    public Guid TweetGuid { get; init; }
    public Guid AuthorGuid { get; init; }
    public string Content { get; init; } = string.Empty;
    public List<string>? MediaUrls { get; init; }
    public List<Guid>? TopicGuids { get; init; }
    public Guid? CircleGuid { get; init; }
    public long ViewCount { get; init; }
    public int LikeCount { get; init; }
    public int CommentCount { get; init; }
    public int FavoriteCount { get; init; }
    public DateTimeOffset PublishTime { get; init; }
    public DateTimeOffset CreateTime { get; init; }
    public bool IsLiked { get; init; }
    public bool IsFavorited { get; init; }
}

