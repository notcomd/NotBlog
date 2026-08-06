namespace Message.Web.API.Dto.Response;
public class TweetDto
{
    public Guid TweetGuid { get; init; }
    public UserBriefDto Author { get; init; } = null!;
    public string Content { get; init; } = string.Empty;
    public List<string>? MediaUrls { get; init; }
    public LinkMetadataDto? LinkMetadata { get; init; }
    public List<string>? Hashtags { get; init; }
    public string TweetStatus { get; init; } = string.Empty;
    public string Visibility { get; init; } = string.Empty;
    public long ViewCount { get; init; }
    public int LikeCount { get; init; }
    public int CommentCount { get; init; }
    public int ShareCount { get; init; }
    public int CoinCount { get; init; }
    public int FavoriteCount { get; init; }
    public double HotScore { get; init; }
    public DateTimeOffset? PublishTime { get; init; }
    public DateTimeOffset CreateTime { get; init; }
    public bool IsLiked { get; init; }
    public bool IsFavorited { get; init; }
    public bool IsCoined { get; init; }
}

