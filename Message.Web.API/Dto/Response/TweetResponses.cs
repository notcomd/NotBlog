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

public class UserBriefDto
{
    public Guid UserGuid { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? Avatar { get; init; }
}

public class LinkMetadataDto
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Image { get; init; }
}

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

public class ReportDto
{
    public Guid ReportGuid { get; init; }
    public Guid ReporterGuid { get; init; }
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetGuid { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public List<string>? EvidenceUrls { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? ReviewerGuid { get; init; }
    public string? ReviewNote { get; init; }
    public DateTimeOffset? ReviewTime { get; init; }
    public DateTimeOffset CreateTime { get; init; }
}

public class AuditLogDto
{
    public Guid AuditGuid { get; init; }
    public Guid TweetGuid { get; init; }
    public Guid AuditorGuid { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset AuditTime { get; init; }
}
