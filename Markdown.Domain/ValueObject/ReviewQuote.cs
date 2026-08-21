namespace Markdown.Domain.Entities;

/// <summary>
///     MarkReview 评论交互统计（值对象）
///     保存评论级点赞、查看、回复数与踩数据。
///     使用线程安全的方式管理计数。
/// </summary>
public class ReviewQuote
{
    private long _loveSome;
    private long _viewSome;
    private long _replySome;
    private long _dislikeSome;

    /// <summary>
    ///     私有构造函数，供 EF Core 使用
    /// </summary>
    private ReviewQuote()
    {
    }

    /// <summary>
    ///     创建 ReviewQuote 实例
    /// </summary>
    public ReviewQuote(long loveSome = 0, long viewSome = 0, long replySome = 0, long dislikeSome = 0)
    {
        if (loveSome < 0) throw new ArgumentOutOfRangeException(nameof(loveSome), "点赞数不能为负数");
        if (viewSome < 0) throw new ArgumentOutOfRangeException(nameof(viewSome), "查看数不能为负数");
        if (replySome < 0) throw new ArgumentOutOfRangeException(nameof(replySome), "回复数不能为负数");
        if (dislikeSome < 0) throw new ArgumentOutOfRangeException(nameof(dislikeSome), "踩数不能为负数");

        _loveSome = loveSome;
        _viewSome = viewSome;
        _replySome = replySome;
        _dislikeSome = dislikeSome;
    }

    /// <summary>
    ///     点赞数量
    /// </summary>
    public long LoveSome => Interlocked.Read(ref _loveSome);

    /// <summary>
    ///     查看数量
    /// </summary>
    public long ViewSome => Interlocked.Read(ref _viewSome);

    /// <summary>
    ///     回复数量
    /// </summary>
    public long ReplySome => Interlocked.Read(ref _replySome);

    /// <summary>
    ///     踩数量
    /// </summary>
    public long DislikeSome => Interlocked.Read(ref _dislikeSome);

    /// <summary>
    ///     增加点赞数（线程安全）
    /// </summary>
    public long AddLove(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _loveSome, count);
    }

    /// <summary>
    ///     减少点赞数（线程安全，下限钳制为 0）
    /// </summary>
    public long RemoveLove(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "减少的数量不能为负数");
        var newValue = Interlocked.Add(ref _loveSome, -count);
        if (newValue < 0)
        {
            Interlocked.Exchange(ref _loveSome, 0);
            return 0;
        }

        return newValue;
    }

    /// <summary>
    ///     增加查看数（线程安全）
    /// </summary>
    public long AddView(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _viewSome, count);
    }

    /// <summary>
    ///     增加回复数（线程安全）
    /// </summary>
    public long AddReply(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _replySome, count);
    }

    /// <summary>
    ///     增加踩数（线程安全）
    /// </summary>
    public long AddDislike(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _dislikeSome, count);
    }

    /// <summary>
    ///     减少踩数（线程安全，下限钳制为 0）
    /// </summary>
    public long RemoveDislike(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "减少的数量不能为负数");
        var newValue = Interlocked.Add(ref _dislikeSome, -count);
        if (newValue < 0)
        {
            Interlocked.Exchange(ref _dislikeSome, 0);
            return 0;
        }

        return newValue;
    }

    /// <summary>
    ///     获取总互动数（点赞 + 回复 + 踩，查看不计入互动）
    /// </summary>
    public long GetTotalInteractions()
    {
        return LoveSome + ReplySome + DislikeSome;
    }

    /// <summary>
    ///     与另一个 ReviewQuote 比较是否相等
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not ReviewQuote)
            return false;

        var other = (ReviewQuote)obj;
        return LoveSome == other.LoveSome &&
               ViewSome == other.ViewSome &&
               ReplySome == other.ReplySome &&
               DislikeSome == other.DislikeSome;
    }

    /// <summary>
    ///     获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(LoveSome, ViewSome, ReplySome, DislikeSome);
    }

    /// <summary>
    ///     转换为字符串表示
    /// </summary>
    public override string ToString()
    {
        return
            $"ReviewQuote(Love:{LoveSome}, View:{ViewSome}, Reply:{ReplySome}, Dislike:{DislikeSome})";
    }
}
