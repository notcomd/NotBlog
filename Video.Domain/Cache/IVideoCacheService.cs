using Video.Domain.Entities;
using Video.Domain.ValueObjects;

namespace Video.Domain.Cache;

/// <summary>
/// Cache service contract for video data.
/// Defined in the Domain layer, implemented in Infrastructure.
/// </summary>
public interface IVideoCacheService
{
    

    ///用于获取视频元数据，返回null if cached.
    Task<Videos?> GetVideoMetaAsync(Guid videoGuid, CancellationToken ct = default);

    /// <summary>Cache video metadata.</summary>
    Task SetVideoMetaAsync(Videos video, CancellationToken ct = default);

    /// <summary>Remove cached video metadata.</summary>
    Task RemoveVideoMetaAsync(Guid videoGuid, CancellationToken ct = default);

    // ── Video Quote (Interaction Counts) ──

    /// <summary>Get cached video quote (interaction counters). Returns null if not cached.</summary>
    Task<VideoQuote?> GetVideoQuoteAsync(Guid videoGuid, CancellationToken ct = default);

    /// <summary>Cache video quote data.</summary>
    Task SetVideoQuoteAsync(Guid videoGuid, VideoQuote quote, CancellationToken ct = default);

    /// <summary>Atomically increment a quote counter in Redis. Returns the new value.</summary>
    Task<long> IncrementQuoteFieldAsync(Guid videoGuid, string field, long delta = 1, CancellationToken ct = default);

    /// <summary>Get all quote fields as a dictionary.</summary>
    Task<Dictionary<string, long>> GetQuoteFieldsAsync(Guid videoGuid, CancellationToken ct = default);

    // ── Video Lists ──

    /// <summary>Get a cached video list by key.</summary>
    Task<List<Videos>?> GetVideoListAsync(string listKey, CancellationToken ct = default);

    /// <summary>Cache a video list.</summary>
    Task SetVideoListAsync(string listKey, List<Videos> videos, CancellationToken ct = default);

    /// <summary>Invalidate all video list caches (e.g., after a new video is created).</summary>
    Task InvalidateVideoListsAsync(CancellationToken ct = default);

    /// <summary>Invalidate all caches related to a specific video.</summary>
    Task InvalidateVideoAsync(Guid videoGuid, CancellationToken ct = default);

    // ── Video Reviews (Comments) ──

    /// <summary>Get cached review list for a video.</summary>
    Task<List<VideoReview>?> GetVideoReviewsAsync(Guid videoGuid, CancellationToken ct = default);

    /// <summary>Cache review list for a video.</summary>
    Task SetVideoReviewsAsync(Guid videoGuid, List<VideoReview> reviews, CancellationToken ct = default);

    /// <summary>Get cached review replies.</summary>
    Task<List<VideoReview>?> GetVideoReviewRepliesAsync(Guid reviewGuid, CancellationToken ct = default);

    /// <summary>Cache review replies.</summary>
    Task SetVideoReviewRepliesAsync(Guid reviewGuid, List<VideoReview> replies, CancellationToken ct = default);

    /// <summary>Atomically increment a review quote counter in Redis. Returns the new value.</summary>
    Task<long> IncrementReviewQuoteFieldAsync(Guid reviewGuid, string field, long delta = 1, CancellationToken ct = default);

    /// <summary>Get cached review quote hash fields.</summary>
    Task<Dictionary<string, long>> GetReviewQuoteFieldsAsync(Guid reviewGuid, CancellationToken ct = default);

    /// <summary>Cache review quote data.</summary>
    Task SetReviewQuoteAsync(Guid reviewGuid, VideoQuote quote, CancellationToken ct = default);

    /// <summary>Invalidate all review caches for a video (after a new review is added).</summary>
    Task InvalidateVideoReviewCachesAsync(Guid videoGuid, CancellationToken ct = default);
}
