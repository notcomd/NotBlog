namespace Video.Domain.ValueObjects;

/// <summary>
/// 评论互动计数（点赞/点踩）— 专用于 VideoReview，语义与视频 VideoQuote 分离。
/// 设计为可变值对象，使用原子操作保证线程安全，计数不会减为负数。
/// </summary>
public record ReviewQuote
{
    private long _like;
    private long _dislike;

    /// <summary>点赞数</summary>
    public long Like => _like;

    /// <summary>点踩数</summary>
    public long Dislike => _dislike;

    private ReviewQuote()
    {
    }

    public static ReviewQuote ReviewQuoteBuilder()
    {
        return new ReviewQuote();
    }

    /// <summary>点赞 +1（原子）</summary>
    public void UpLike() => Interlocked.Increment(ref _like);

    /// <summary>点踩 +1（原子）</summary>
    public void UpDislike() => Interlocked.Increment(ref _dislike);

    /// <summary>取消点赞 −1（确保不会减为负数）</summary>
    public void DownLike() => AtomicDecrement(ref _like);

    /// <summary>取消点踩 −1（确保不会减为负数）</summary>
    public void DownDislike() => AtomicDecrement(ref _dislike);

    private static void AtomicDecrement(ref long field)
    {
        long current = field;
        while (current > 0 && Interlocked.CompareExchange(ref field, current - 1, current) != current)
        {
            current = field;
        }
    }
}