namespace Markdown.Domain.Entities;

/// <summary>
///     MarkReview 引用统计（值对象）
///     使用线程安全的方式管理点赞、回复、评论、分享、浏览计数
/// </summary>
public class MarkQuote
{
    private long _commentSome;
    private long _loveSome;
    private long _reviewSome;
    private long _shareSome;
    private long _viewSome;

    /// <summary>
    ///     私有构造函数，供 EF Core 使用
    /// </summary>
    private MarkQuote()
    {
    }

    /// <summary>
    ///     创建 MarkQuote 实例
    /// </summary>
    public MarkQuote(long loveSome = 0, long reviewSome = 0, long commentSome = 0,
        long shareSome = 0, long viewSome = 0)
    {
        if (loveSome < 0) throw new ArgumentOutOfRangeException(nameof(loveSome), "点赞数不能为负数");
        if (reviewSome < 0) throw new ArgumentOutOfRangeException(nameof(reviewSome), "回复数不能为负数");
        if (commentSome < 0) throw new ArgumentOutOfRangeException(nameof(commentSome), "评论数不能为负数");
        if (shareSome < 0) throw new ArgumentOutOfRangeException(nameof(shareSome), "分享数不能为负数");
        if (viewSome < 0) throw new ArgumentOutOfRangeException(nameof(viewSome), "浏览数不能为负数");

        _loveSome = loveSome;
        _reviewSome = reviewSome;
        _commentSome = commentSome;
        _shareSome = shareSome;
        _viewSome = viewSome;
    }

    /// <summary>
    ///     点赞数量
    /// </summary>
    public long LoveSome => Interlocked.Read(ref _loveSome);

    /// <summary>
    ///     回复数量
    /// </summary>
    public long ReviewSome => Interlocked.Read(ref _reviewSome);

    /// <summary>
    ///     评论数量
    /// </summary>
    public long CommentSome => Interlocked.Read(ref _commentSome);

    /// <summary>
    ///     分享数量
    /// </summary>
    public long ShareSome => Interlocked.Read(ref _shareSome);

    /// <summary>
    ///     浏览数量
    /// </summary>
    public long ViewSome => Interlocked.Read(ref _viewSome);

    /// <summary>
    ///     增加点赞数（线程安全）
    /// </summary>
    /// <param name="count">增加的数量，默认为 1</param>
    /// <returns>新的点赞总数</returns>
    public long AddLove(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _loveSome, count);
    }

    /// <summary>
    ///     减少点赞数（线程安全）
    /// </summary>
    /// <param name="count">减少的数量，默认为 1</param>
    /// <returns>新的点赞总数</returns>
    public long RemoveLove(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "减少的数量不能为负数");
        var newValue = Interlocked.Add(ref _loveSome, -count);
        if (newValue < 0)
        {
            // 如果结果为负数，重置为 0
            Interlocked.Exchange(ref _loveSome, 0);
            return 0;
        }

        return newValue;
    }

    /// <summary>
    ///   增加回复数（线程安全）
    /// </summary>
    /// <param name="count">增加的数量，默认为 1</param>
    /// <returns>新的回复总数</returns>
    public long AddReview(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _reviewSome, count);
    }

    /// <summary>
    /// 增加评论数（线程安全）
    /// </summary>
    /// <param name="count">增加的数量，默认为 1</param>
    /// <returns>新的评论总数</returns>
    public long AddComment(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _commentSome, count);
    }

    /// <summary>
    /// 增加分享数（线程安全）
    /// </summary>
    /// <param name="count">增加的数量，默认为 1</param>
    /// <returns>新的分享总数</returns>
    public long AddShare(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _shareSome, count);
    }

    /// <summary>
    ///     增加浏览数（线程安全）
    /// </summary>
    /// <param name="count">增加的数量，默认为 1</param>
    /// <returns>新的浏览总数</returns>
    public long AddView(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _viewSome, count);
    }

    /// <summary>
    ///  获取总互动数（点赞 + 回复 + 评论 + 分享）
    /// </summary>
    /// <returns>总互动数</returns>
    public long GetTotalInteractions()
    {
        return LoveSome + ReviewSome + CommentSome + ShareSome;
    }

    /// <summary>
    ///     与另一个 MarkQuote 比较是否相等
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || !(obj is MarkQuote))
            return false;

        var other = (MarkQuote)obj;
        return LoveSome == other.LoveSome &&
               ReviewSome == other.ReviewSome &&
               CommentSome == other.CommentSome &&
               ShareSome == other.ShareSome &&
               ViewSome == other.ViewSome;
    }

    /// <summary>
    ///     获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(LoveSome, ReviewSome, CommentSome, ShareSome, ViewSome);
    }

    /// <summary>
    ///     转换为字符串表示
    /// </summary>
    public override string ToString()
    {
        return
            $"MarkQuote(Love:{LoveSome}, Review:{ReviewSome}, Comment:{CommentSome}, Share:{ShareSome}, View:{ViewSome})";
    }
}