using System.Text.Json;
using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Video.Domain.Cache;
using Video.Domain.Entities;
using Video.Domain.ValueObjects;

namespace Video.Infrastructure.Cache;

/// <summary>
/// Video cache service implementation using Redis via IRedisCacheService.
/// Uses Redis Hash for structured data (metadata, quotes) and
/// Redis String (JSON) for serialized list results.
/// </summary>
public class VideoCacheService : IVideoCacheService
{
    private readonly IRedisCacheService _redis;
    private readonly ILogger<VideoCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public VideoCacheService(IRedisCacheService redis, ILogger<VideoCacheService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }


    /// <summary>
    /// 获取视频元数据
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>视频元数据</returns>
    public async Task<Videos?> GetVideoMetaAsync(Guid videoGuid, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoMeta(videoGuid);
        var json = await _redis.StringGetAsync(key, ct);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Videos>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize video meta from cache: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// 设置视频元数据
    /// </summary>
    /// <param name="video">视频元数据</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetVideoMetaAsync(Videos video, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoMeta(video.VideoGuid);
        var json = JsonSerializer.Serialize(video, _jsonOptions);
        await _redis.StringSetAsync(key, json, VideoCacheKeys.VideoMetaTtl, ct);
        _logger.LogDebug("Cached video meta: {Key}", key);
    }

    /// <summary>
    /// 无效视频缓存
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="ct">取消令牌</param>
    public async Task RemoveVideoMetaAsync(Guid videoGuid, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoMeta(videoGuid);
        await _redis.KeyDeleteAsync(key, ct);
        _logger.LogDebug("Removed video meta cache: {Key}", key);
    }


    /// <summary>
    /// 获取视频互动次数
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>互动次数</returns>
    public async Task<VideoQuote?> GetVideoQuoteAsync(Guid videoGuid, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoQuote(videoGuid);
        var fields = await _redis.HashGetAllAsync(key, ct);
        if (fields is null || fields.Count == 0)
            return null;

        var quote = VideoQuote.VideoQuoteBuilder();

        // Use reflection-free assignment via direct field increment (hack: set via increment from 0)
        // Since VideoQuote fields are private, we build it by deserializing from hash values
        if (fields.TryGetValue(VideoCacheKeys.QuoteFields.Upvote, out var uv) && long.TryParse(uv, out var uvVal))
            while (quote.Upvote < uvVal) quote.UpUpvote();
        if (fields.TryGetValue(VideoCacheKeys.QuoteFields.Stars, out var st) && long.TryParse(st, out var stVal))
            while (quote.Stars < stVal) quote.UpStars();
        if (fields.TryGetValue(VideoCacheKeys.QuoteFields.Watch, out var w) && long.TryParse(w, out var wVal))
            while (quote.Watch < wVal) quote.UpWatch();
        if (fields.TryGetValue(VideoCacheKeys.QuoteFields.Down, out var d) && long.TryParse(d, out var dVal))
            while (quote.Down < dVal) quote.UpDown();
        if (fields.TryGetValue(VideoCacheKeys.QuoteFields.Ballot, out var b) && long.TryParse(b, out var bVal))
            while (quote.Ballot < bVal) quote.UpBallot();
        if (fields.TryGetValue(VideoCacheKeys.QuoteFields.Share, out var sh) && long.TryParse(sh, out var shVal))
            while (quote.Share < shVal) quote.UpShare();

        return quote;
    }

    /// <summary>
    /// 设置视频互动次数
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="quote">互动次数</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetVideoQuoteAsync(Guid videoGuid, VideoQuote quote, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoQuote(videoGuid);
        var entries = new Dictionary<string, string>
        {
            [VideoCacheKeys.QuoteFields.Upvote] = quote.Upvote.ToString(),
            [VideoCacheKeys.QuoteFields.Stars] = quote.Stars.ToString(),
            [VideoCacheKeys.QuoteFields.Watch] = quote.Watch.ToString(),
            [VideoCacheKeys.QuoteFields.Down] = quote.Down.ToString(),
            [VideoCacheKeys.QuoteFields.Ballot] = quote.Ballot.ToString(),
            [VideoCacheKeys.QuoteFields.Share] = quote.Share.ToString()
        };

        await _redis.HashSetManyAsync(key, entries, ct);
        await _redis.KeyExpireAsync(key, VideoCacheKeys.VideoQuoteTtl, ct);
        _logger.LogDebug("Cached video quote: {Key}", key);
    }

    /// <summary>
    /// 增加视频互动次数
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="field">互动字段</param>
    /// <param name="delta">增加量</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新的互动次数</returns>
    public async Task<long> IncrementQuoteFieldAsync(Guid videoGuid, string field, long delta = 1,
        CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoQuote(videoGuid);
        var newValue = await _redis.HashIncrementAsync(key, field, delta, ct);

        // Refresh TTL on interaction
        await _redis.KeyExpireAsync(key, VideoCacheKeys.VideoQuoteTtl, ct);

        _logger.LogDebug("Incremented quote field {Field} by {Delta} for video {VideoGuid}: new={Value}",
            field, delta, videoGuid, newValue);

        return newValue;
    }

    /// <summary>
    /// 获取视频互动次数
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>互动次数字典</returns>
    public async Task<Dictionary<string, long>> GetQuoteFieldsAsync(Guid videoGuid, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoQuote(videoGuid);
        var fields = await _redis.HashGetAllAsync(key, ct);
        if (fields is null || fields.Count == 0)
            return new Dictionary<string, long>();

        var result = new Dictionary<string, long>();
        foreach (var (field, value) in fields)
        {
            if (long.TryParse(value, out var parsed))
                result[field] = parsed;
        }

        return result;
    }

    /// <summary>
    /// 获取视频列表
    /// </summary>
    /// <param name="listKey">列表键</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>视频列表</returns>
    public async Task<List<Videos>?> GetVideoListAsync(string listKey, CancellationToken ct = default)
    {
        var json = await _redis.StringGetAsync(listKey, ct);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<Videos>>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize video list from cache: {Key}", listKey);
            return null;
        }
    }


    /// <summary>
    /// 设置视频列表
    /// </summary>
    /// <param name="listKey">列表键</param>
    /// <param name="videos">视频列表</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetVideoListAsync(string listKey, List<Videos> videos, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(videos, _jsonOptions);
        await _redis.StringSetAsync(listKey, json, VideoCacheKeys.VideoListTtl, ct);
        _logger.LogDebug("Cached video list: {Key}", listKey);
    }

    /// <summary>
    /// 无效所有视频列表缓存
    /// </summary>
    /// <param name="ct">取消令牌</param>
    public async Task InvalidateVideoListsAsync(CancellationToken ct = default)
    {
        var keys = await _redis.KeysByPatternAsync(VideoCacheKeys.AllVideoListsPattern, ct: ct);
        if (keys.Any())
        {
            await _redis.KeyDeleteManyAsync(keys, ct);
            _logger.LogInformation("Invalidated {Count} video list caches", keys.Count());
        }
    }


    /// <summary>
    /// 无效视频缓存
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="ct">取消令牌</param>
    public async Task InvalidateVideoAsync(Guid videoGuid, CancellationToken ct = default)
    {
        await RemoveVideoMetaAsync(videoGuid, ct);
        await _redis.KeyDeleteAsync(VideoCacheKeys.VideoQuote(videoGuid), ct);
        await InvalidateVideoListsAsync(ct);
        await InvalidateVideoReviewCachesAsync(videoGuid, ct);
        _logger.LogInformation("Invalidated all caches for video: {VideoGuid}", videoGuid);
    }



    /// <summary>
    /// 获取视频评论列表缓存
    /// </summary>
    public async Task<List<VideoReview>?> GetVideoReviewsAsync(Guid videoGuid, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoReviews(videoGuid);
        var json = await _redis.StringGetAsync(key, ct);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<VideoReview>>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize review list from cache: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// 设置视频评论列表缓存
    /// </summary>
    public async Task SetVideoReviewsAsync(Guid videoGuid, List<VideoReview> reviews, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoReviews(videoGuid);
        var json = JsonSerializer.Serialize(reviews, _jsonOptions);
        await _redis.StringSetAsync(key, json, VideoCacheKeys.ReviewListTtl, ct);
        _logger.LogDebug("Cached {Count} reviews for video: {VideoGuid}", reviews.Count, videoGuid);
    }

    /// <summary>
    /// 获取评论回复缓存
    /// </summary>
    public async Task<List<VideoReview>?> GetVideoReviewRepliesAsync(Guid reviewGuid, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoReviewReplies(reviewGuid);
        var json = await _redis.StringGetAsync(key, ct);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<VideoReview>>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize review replies from cache: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// 设置评论回复缓存
    /// </summary>
    public async Task SetVideoReviewRepliesAsync(Guid reviewGuid, List<VideoReview> replies,
        CancellationToken ct = default)
    {
        var key = VideoCacheKeys.VideoReviewReplies(reviewGuid);
        var json = JsonSerializer.Serialize(replies, _jsonOptions);
        await _redis.StringSetAsync(key, json, VideoCacheKeys.ReviewListTtl, ct);
        _logger.LogDebug("Cached {Count} replies for review: {ReviewGuid}", replies.Count, reviewGuid);
    }

    /// <summary>
    /// 原子递增评论互动计数
    /// </summary>
    public async Task<long> IncrementReviewQuoteFieldAsync(Guid reviewGuid, string field, long delta = 1,
        CancellationToken ct = default)
    {
        var key = VideoCacheKeys.ReviewQuote(reviewGuid);
        var newValue = await _redis.HashIncrementAsync(key, field, delta, ct);

        // Refresh TTL on interaction
        await _redis.KeyExpireAsync(key, VideoCacheKeys.ReviewQuoteTtl, ct);

        _logger.LogDebug("Incremented review quote field {Field} by {Delta} for review {ReviewGuid}: new={Value}",
            field, delta, reviewGuid, newValue);

        return newValue;
    }

    /// <summary>
    /// 获取评论互动计数字段
    /// </summary>
    public async Task<Dictionary<string, long>> GetReviewQuoteFieldsAsync(Guid reviewGuid,
        CancellationToken ct = default)
    {
        var key = VideoCacheKeys.ReviewQuote(reviewGuid);
        var fields = await _redis.HashGetAllAsync(key, ct);
        if (fields is null || fields.Count == 0)
            return new Dictionary<string, long>();

        var result = new Dictionary<string, long>();
        foreach (var (field, value) in fields)
        {
            if (long.TryParse(value, out var parsed))
                result[field] = parsed;
        }

        return result;
    }

    /// <summary>
    /// 设置评论互动计数缓存
    /// </summary>
    public async Task SetReviewQuoteAsync(Guid reviewGuid, VideoQuote quote, CancellationToken ct = default)
    {
        var key = VideoCacheKeys.ReviewQuote(reviewGuid);
        var entries = new Dictionary<string, string>
        {
            [VideoCacheKeys.QuoteFields.Upvote] = quote.Upvote.ToString(),
            [VideoCacheKeys.QuoteFields.Stars] = quote.Stars.ToString(),
            [VideoCacheKeys.QuoteFields.Watch] = quote.Watch.ToString(),
            [VideoCacheKeys.QuoteFields.Down] = quote.Down.ToString(),
            [VideoCacheKeys.QuoteFields.Ballot] = quote.Ballot.ToString(),
            [VideoCacheKeys.QuoteFields.Share] = quote.Share.ToString()
        };

        await _redis.HashSetManyAsync(key, entries, ct);
        await _redis.KeyExpireAsync(key, VideoCacheKeys.ReviewQuoteTtl, ct);
        _logger.LogDebug("Cached review quote: {Key}", key);
    }

    /// <summary>
    /// 无效视频的所有评论缓存
    /// </summary>
    public async Task InvalidateVideoReviewCachesAsync(Guid videoGuid, CancellationToken ct = default)
    {
        var reviewsKey = VideoCacheKeys.VideoReviews(videoGuid);
        await _redis.KeyDeleteAsync(reviewsKey, ct);
        _logger.LogDebug("Invalidated review caches for video: {VideoGuid}", videoGuid);
    }

}
