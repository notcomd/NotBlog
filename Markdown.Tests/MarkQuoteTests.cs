using Markdown.Domain.Entities;

namespace Markdown.Tests;

/// <summary>
///     MarkQuote 评论互动计数值对象测试：增减、下限钳制、总互动、非法参数
/// </summary>
[TestFixture]
public class MarkQuoteTests
{
    [Test]
    public void 默认计数全部为零()
    {
        var q = new MarkQuote();
        Assert.That(q.LoveSome, Is.Zero);
        Assert.That(q.ReviewSome, Is.Zero);
        Assert.That(q.CommentSome, Is.Zero);
        Assert.That(q.ShareSome, Is.Zero);
        Assert.That(q.ViewSome, Is.Zero);
    }

    [Test]
    public void 增加计数累加正确()
    {
        var q = new MarkQuote();
        Assert.That(q.AddLove(), Is.EqualTo(1));
        Assert.That(q.AddLove(3), Is.EqualTo(4));
        Assert.That(q.AddReview(2), Is.EqualTo(2));
        Assert.That(q.AddComment(), Is.EqualTo(1));
        Assert.That(q.AddShare(), Is.EqualTo(1));
        Assert.That(q.AddView(5), Is.EqualTo(5));
    }

    [Test]
    public void 减少计数不低于零()
    {
        var q = new MarkQuote(loveSome: 2);
        Assert.That(q.RemoveLove(), Is.EqualTo(1));
        Assert.That(q.RemoveLove(), Is.EqualTo(0));
        Assert.That(q.RemoveLove(), Is.EqualTo(0), "下限钳制为 0，不出现负数");
    }

    [Test]
    public void 总互动数为点赞加回复加评论加分享()
    {
        var q = new MarkQuote(loveSome: 1, reviewSome: 2, commentSome: 3, shareSome: 4, viewSome: 100);
        Assert.That(q.GetTotalInteractions(), Is.EqualTo(10), "浏览数不计入互动总数");
    }

    [Test]
    public void 负数参数抛异常()
    {
        var q = new MarkQuote();
        Assert.Throws<ArgumentOutOfRangeException>(() => q.AddLove(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.RemoveLove(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.AddView(-5));
    }

    [Test]
    public void 构造时负数抛异常()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarkQuote(loveSome: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarkQuote(viewSome: -1));
    }

    [Test]
    public void 相等性与字符串表示()
    {
        var a = new MarkQuote(1, 2, 3, 4, 5);
        var b = new MarkQuote(1, 2, 3, 4, 5);
        var c = new MarkQuote(1, 2, 3, 4, 6);
        Assert.That(a.Equals(b), Is.True);
        Assert.That(a.Equals(c), Is.False);
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        Assert.That(a.ToString(), Does.Contain("Love:1"));
    }
}
