namespace Video.Domain.ValueObjects;

/// <summary>
/// 视频互动计数（点赞、收藏、播放等）
/// 设计为可变值对象，使用原子操作保证线程安全
/// </summary>
public record VideoQuote
{
    private long _upvote;
    private long _stars;
    private long _watch;
    private long _down;
    private long _ballot;
    private long _share;

    public long Upvote   => _upvote;
    public long Stars    => _stars;
    public long Watch    => _watch;
    public long Down     => _down;
    public long Ballot   => _ballot;
    public long Share    => _share;
    
    private VideoQuote()
    {
    }
    
    public static VideoQuote VideoQuoteBuilder()
    {
        return new VideoQuote();
    }

    // 原子增加
    public void UpUpvote()   => Interlocked.Increment(ref _upvote);
    public void UpStars()    => Interlocked.Increment(ref _stars);
    public void UpWatch()    => Interlocked.Increment(ref _watch);
    public void UpDown()     => Interlocked.Increment(ref _down);
    public void UpBallot()   => Interlocked.Increment(ref _ballot);
    public void UpShare()    => Interlocked.Increment(ref _share);

    // 原子减少（确保不会减为负数）
    public void DownUpvote()   => AtomicDecrement(ref _upvote);
    public void DownStars()    => AtomicDecrement(ref _stars);
    //public void DownWatch()    => AtomicDecrement(ref _watch);
    public void DownDown()     => AtomicDecrement(ref _down);
    public void DownBallot()   => AtomicDecrement(ref _ballot);
    public void DownShare()    => AtomicDecrement(ref _share);

    private static void AtomicDecrement(ref long field)
    {
        long current = field;
        while (current > 0 && Interlocked.CompareExchange(ref field, current - 1, current) != current)
        {
            current = field;
        }
    }
}