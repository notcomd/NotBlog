namespace Markdown.Domain.Entities;

/// <summary>
///     MarkDown 文档交互统计（值对象）
///     保存文档级浏览、点赞、收藏、分享、硬币与热度数据，用于计算文档热点。
///     使用线程安全的方式管理计数。
/// </summary>
public class MarkQuote
{
    private long _loveSome;
    private long _favoriteSome;
    private long _shareSome;
    private long _coinSome;
    private long _viewSome;
    private double _heatScore;

    /// <summary>
    ///     私有构造函数，供 EF Core 使用
    /// </summary>
    private MarkQuote()
    {
    }

    /// <summary>
    ///     创建 MarkQuote 实例
    /// </summary>
    public MarkQuote(long loveSome = 0, long favoriteSome = 0, long shareSome = 0,
        long coinSome = 0, long viewSome = 0, double heatScore = 0)
    {
        if (loveSome < 0) throw new ArgumentOutOfRangeException(nameof(loveSome), "点赞数不能为负数");
        if (favoriteSome < 0) throw new ArgumentOutOfRangeException(nameof(favoriteSome), "收藏数不能为负数");
        if (shareSome < 0) throw new ArgumentOutOfRangeException(nameof(shareSome), "分享数不能为负数");
        if (coinSome < 0) throw new ArgumentOutOfRangeException(nameof(coinSome), "硬币数不能为负数");
        if (viewSome < 0) throw new ArgumentOutOfRangeException(nameof(viewSome), "浏览数不能为负数");
        if (heatScore < 0) throw new ArgumentOutOfRangeException(nameof(heatScore), "热度分不能为负数");

        _loveSome = loveSome;
        _favoriteSome = favoriteSome;
        _shareSome = shareSome;
        _coinSome = coinSome;
        _viewSome = viewSome;
        _heatScore = heatScore;
    }

    /// <summary>
    ///     点赞数量
    /// </summary>
    public long LoveSome => Interlocked.Read(ref _loveSome);

    /// <summary>
    ///     收藏数量
    /// </summary>
    public long FavoriteSome => Interlocked.Read(ref _favoriteSome);

    /// <summary>
    ///     分享数量
    /// </summary>
    public long ShareSome => Interlocked.Read(ref _shareSome);

    /// <summary>
    ///     硬币（打赏）数量
    /// </summary>
    public long CoinSome => Interlocked.Read(ref _coinSome);

    /// <summary>
    ///     浏览数量
    /// </summary>
    public long ViewSome => Interlocked.Read(ref _viewSome);

    /// <summary>
    ///     热度分（由热点算法计算，定时任务/写侧维护）
    /// </summary>
    public double HeatScore => Volatile.Read(ref _heatScore);

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
    ///     增加收藏数（线程安全）
    /// </summary>
    public long AddFavorite(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _favoriteSome, count);
    }

    /// <summary>
    ///     减少收藏数（线程安全，下限钳制为 0）
    /// </summary>
    public long RemoveFavorite(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "减少的数量不能为负数");
        var newValue = Interlocked.Add(ref _favoriteSome, -count);
        if (newValue < 0)
        {
            Interlocked.Exchange(ref _favoriteSome, 0);
            return 0;
        }

        return newValue;
    }

    /// <summary>
    ///     增加分享数（线程安全）
    /// </summary>
    public long AddShare(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _shareSome, count);
    }

    /// <summary>
    ///     增加硬币数（线程安全）
    /// </summary>
    public long AddCoin(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _coinSome, count);
    }

    /// <summary>
    ///     增加浏览数（线程安全）
    /// </summary>
    public long AddView(long count = 1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "增加的数量不能为负数");
        return Interlocked.Add(ref _viewSome, count);
    }

    /// <summary>
    ///     设置热度分（由热点计算服务调用）
    /// </summary>
    public void SetHeatScore(double score)
    {
        if (score < 0) throw new ArgumentOutOfRangeException(nameof(score), "热度分不能为负数");
        Volatile.Write(ref _heatScore, score);
    }

    /// <summary>
    ///     获取总互动数（点赞 + 收藏 + 分享 + 硬币，浏览不计入互动）
    /// </summary>
    public long GetTotalInteractions()
    {
        return LoveSome + FavoriteSome + ShareSome + CoinSome;
    }

    /// <summary>
    ///     与另一个 MarkQuote 比较是否相等
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not MarkQuote)
            return false;

        var other = (MarkQuote)obj;
        return LoveSome == other.LoveSome &&
               FavoriteSome == other.FavoriteSome &&
               ShareSome == other.ShareSome &&
               CoinSome == other.CoinSome &&
               ViewSome == other.ViewSome &&
               HeatScore.Equals(other.HeatScore);
    }

    /// <summary>
    ///     获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(LoveSome, FavoriteSome, ShareSome, CoinSome, ViewSome, HeatScore);
    }

    /// <summary>
    ///     转换为字符串表示
    /// </summary>
    public override string ToString()
    {
        return
            $"MarkQuote(Love:{LoveSome}, Favorite:{FavoriteSome}, Share:{ShareSome}, Coin:{CoinSome}, View:{ViewSome}, Heat:{HeatScore})";
    }
}
