namespace Video.Tests.ValueObjects;

/// <summary>
/// 视频互动计数（VideoQuote）单元测试 — 重点覆盖互动实施文档新增的 Stars（收藏）字段。
/// </summary>
[TestFixture]
public class VideoQuoteTests
{
    [Test]
    public void 收藏_计数递增()
    {
        var quote = VideoQuote.VideoQuoteBuilder();

        quote.UpStars();
        quote.UpStars();
        quote.UpStars();

        Assert.That(quote.Stars, Is.EqualTo(3));
    }

    [Test]
    public void 取消收藏_计数递减()
    {
        var quote = VideoQuote.VideoQuoteBuilder();
        quote.UpStars();
        quote.UpStars();

        quote.DownStars();

        Assert.That(quote.Stars, Is.EqualTo(1));
    }

    [Test]
    public void 取消收藏_下限为零_不会减为负数()
    {
        var quote = VideoQuote.VideoQuoteBuilder();

        quote.DownStars();
        quote.DownStars();

        Assert.That(quote.Stars, Is.EqualTo(0));
    }

    [Test]
    public void 并发收藏_计数不丢失()
    {
        const int concurrency = 64;
        const int perThread = 50;
        var quote = VideoQuote.VideoQuoteBuilder();

        Parallel.For(0, concurrency, _ =>
        {
            for (var i = 0; i < perThread; i++) quote.UpStars();
        });

        Assert.That(quote.Stars, Is.EqualTo((long)concurrency * perThread));
    }

    [Test]
    public void 收藏与其它维度互不影响()
    {
        var quote = VideoQuote.VideoQuoteBuilder();

        quote.UpStars();
        quote.UpUpvote();
        quote.UpWatch();

        Assert.That(quote.Stars, Is.EqualTo(1));
        Assert.That(quote.Upvote, Is.EqualTo(1));
        Assert.That(quote.Watch, Is.EqualTo(1));
        Assert.That(quote.Ballot, Is.EqualTo(0));
    }
}