using Markdown.Domain.Entities;

namespace Markdown.Tests;

/// <summary>
///     ReviewQuote 评论交互计数值对象测试：增减、下限钳制、总互动、非法参数
/// </summary>
[TestFixture]
public class ReviewQuoteTests
{
    [Test]
    public void 默认计数全部为零()
    {
        var q = new ReviewQuote();
        Assert.That(q.LoveSome, Is.Zero);
        Assert.That(q.ViewSome, Is.Zero);
        Assert.That(q.ReplySome, Is.Zero);
        Assert.That(q.DislikeSome, Is.Zero);
    }

    [Test]
    public void 增加计数累加正确()
    {
        var q = new ReviewQuote();
        Assert.That(q.AddLove(), Is.EqualTo(1));
        Assert.That(q.AddLove(3), Is.EqualTo(4));
        Assert.That(q.AddView(5), Is.EqualTo(5));
        Assert.That(q.AddReply(), Is.EqualTo(1));
        Assert.That(q.AddDislike(), Is.EqualTo(1));
        Assert.That(q.AddDislike(2), Is.EqualTo(3));
    }

    [Test]
    public void 减少计数不低于零()
    {
        var q = new ReviewQuote(loveSome: 2, dislikeSome: 1);
        Assert.That(q.RemoveLove(), Is.EqualTo(1));
        Assert.That(q.RemoveLove(), Is.EqualTo(0));
        Assert.That(q.RemoveLove(), Is.EqualTo(0), "下限钳制为 0，不出现负数");
        Assert.That(q.RemoveDislike(), Is.EqualTo(0));
        Assert.That(q.RemoveDislike(), Is.EqualTo(0), "下限钳制为 0，不出现负数");
    }

    [Test]
    public void 总互动数为点赞加回复加踩()
    {
        var q = new ReviewQuote(loveSome: 1, viewSome: 100, replySome: 2, dislikeSome: 3);
        Assert.That(q.GetTotalInteractions(), Is.EqualTo(6), "查看数不计入互动总数");
    }

    [Test]
    public void 负数参数抛异常()
    {
        var q = new ReviewQuote();
        Assert.Throws<ArgumentOutOfRangeException>(() => q.AddLove(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.RemoveLove(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.AddView(-5));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.AddReply(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.AddDislike(-1));
    }

    [Test]
    public void 构造时负数抛异常()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReviewQuote(loveSome: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReviewQuote(viewSome: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReviewQuote(dislikeSome: -1));
    }

    [Test]
    public void 相等性与字符串表示()
    {
        var a = new ReviewQuote(1, 2, 3, 4);
        var b = new ReviewQuote(1, 2, 3, 4);
        var c = new ReviewQuote(1, 2, 3, 5);
        Assert.That(a.Equals(b), Is.True);
        Assert.That(a.Equals(c), Is.False);
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        Assert.That(a.ToString(), Does.Contain("Love:1"));
    }
}
