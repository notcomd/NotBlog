namespace Video.Tests.ValueObjects;

/// <summary>
/// 评论互动计数（ReviewQuote）单元测试。
/// 覆盖：点赞/点踩增减、取消操作下限 0、并发原子安全。
/// </summary>
[TestFixture]
public class ReviewQuoteTests
{
    [Test]
    public void 点赞_计数递增()
    {
        var quote = ReviewQuote.ReviewQuoteBuilder();

        quote.UpLike();
        quote.UpLike();

        Assert.That(quote.Like, Is.EqualTo(2));
        Assert.That(quote.Dislike, Is.EqualTo(0));
    }

    [Test]
    public void 点踩_计数递增()
    {
        var quote = ReviewQuote.ReviewQuoteBuilder();

        quote.UpDislike();
        quote.UpDislike();
        quote.UpDislike();

        Assert.That(quote.Dislike, Is.EqualTo(3));
        Assert.That(quote.Like, Is.EqualTo(0));
    }

    [Test]
    public void 取消点赞_递减()
    {
        var quote = ReviewQuote.ReviewQuoteBuilder();
        quote.UpLike();
        quote.UpLike();

        quote.DownLike();

        Assert.That(quote.Like, Is.EqualTo(1));
    }

    [Test]
    public void 取消点赞_下限为零_不会减为负数()
    {
        var quote = ReviewQuote.ReviewQuoteBuilder();

        quote.DownLike();
        quote.DownLike();

        Assert.That(quote.Like, Is.EqualTo(0));
    }

    [Test]
    public void 取消点踩_下限为零_不会减为负数()
    {
        var quote = ReviewQuote.ReviewQuoteBuilder();

        quote.DownDislike();
        quote.DownDislike();

        Assert.That(quote.Dislike, Is.EqualTo(0));
    }

    [Test]
    public void 并发点赞_计数不丢失()
    {
        const int concurrency = 64;
        const int perThread = 50;
        var quote = ReviewQuote.ReviewQuoteBuilder();

        Parallel.For(0, concurrency, _ =>
        {
            for (var i = 0; i < perThread; i++) quote.UpLike();
        });

        Assert.That(quote.Like, Is.EqualTo((long)concurrency * perThread));
    }
}