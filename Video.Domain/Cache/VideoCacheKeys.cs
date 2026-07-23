namespace Video.Domain.Cache;

/// <summary>
/// Video cache key naming convention.
/// All keys follow the pattern: {namespace}:{resource}:{identifier}
/// </summary>
public static class VideoCacheKeys
{
    private const string Prefix = "video";

    // ── Video Metadata (Hash) ──
    /// <summary>Redis Hash: video metadata fields. Key format: video:meta:{videoGuid}</summary>
    public static string VideoMeta(Guid videoGuid) => $"{Prefix}:meta:{videoGuid}";

    // ── Video Quote / Interaction Counts (Hash) ──
    /// <summary>Redis Hash: interaction counts (upvote, stars, watch, etc.). Key format: video:quote:{videoGuid}</summary>
    public static string VideoQuote(Guid videoGuid) => $"{Prefix}:quote:{videoGuid}";

    /// <summary>Hash field names for VideoQuote counters.</summary>
    public static class QuoteFields
    {
        public const string Upvote = "upvote";
        public const string Stars = "stars";
        public const string Watch = "watch";
        public const string Down = "down";
        public const string Ballot = "ballot";
        public const string Share = "share";
    }

    // ── Video Lists (String / JSON) ──
    /// <summary>Key format: video:list:all</summary>
    public const string VideoListAll = $"{Prefix}:list:all";

    /// <summary>Key format: video:list:page:{page}:{pageSize}</summary>
    public static string VideoListPage(int page, int pageSize) => $"{Prefix}:list:page:{page}:{pageSize}";

    /// <summary>Key format: video:list:name:{name}</summary>
    public static string VideoListByName(string name) => $"{Prefix}:list:name:{name}";

    /// <summary>Key format: video:list:blurred:{name}</summary>
    public static string VideoListBlurred(string name) => $"{Prefix}:list:blurred:{name}";

    // ── Cache TTL Constants ──
    public static readonly TimeSpan VideoMetaTtl = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan VideoListTtl = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan VideoSearchTtl = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan VideoQuoteTtl = TimeSpan.FromHours(2);

    // ── Invalidation Patterns ──
    /// <summary>Pattern to invalidate all video list caches.</summary>
    public const string AllVideoListsPattern = $"{Prefix}:list:*";

    /// <summary>Pattern to invalidate all caches for a specific video.</summary>
    public static string VideoAllPattern(Guid videoGuid) => $"{Prefix}:*:{videoGuid}";

    // ── Video Reviews (Comments) ──
    /// <summary>Redis String (JSON): cached review list for a video. Key format: video:reviews:{videoGuid}</summary>
    public static string VideoReviews(Guid videoGuid) => $"{Prefix}:reviews:{videoGuid}";

    /// <summary>Redis String (JSON): cached review replies. Key format: video:replies:{reviewGuid}</summary>
    public static string VideoReviewReplies(Guid reviewGuid) => $"{Prefix}:replies:{reviewGuid}";

    /// <summary>Redis Hash: interaction counters for a single review. Key format: video:review-quote:{reviewGuid}</summary>
    public static string ReviewQuote(Guid reviewGuid) => $"{Prefix}:review-quote:{reviewGuid}";

    /// <summary>Pattern to invalidate all review caches for a video.</summary>
    public static string VideoReviewAllPattern(Guid videoGuid) => $"{Prefix}:reviews:*:{videoGuid}";

    /// <summary>TTL for review list caches.</summary>
    public static readonly TimeSpan ReviewListTtl = TimeSpan.FromMinutes(10);

    /// <summary>TTL for individual review quote cache.</summary>
    public static readonly TimeSpan ReviewQuoteTtl = TimeSpan.FromHours(2);
}
