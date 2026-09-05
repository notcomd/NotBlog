using Markdown.Domain.Entities;
using Markdown.Domain.Heat;

namespace Markdown.Tests;

/// <summary>
///     热点分公式测试：权重占比 / log 压缩 / 时间衰减单调性 / 零计数基准
/// </summary>
[TestFixture]
public class MarkdownHeatFormulaTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void 零计数文档热度为时间衰减项()
    {
        var q = new MarkQuote();
        var created = Now;
        var score = MarkdownHeatFormula.Calculate(q, created, Now);
        // 零互动零浏览：score = 0.2 × exp(0) = 0.2
        Assert.That(score, Is.EqualTo(0.2).Within(1e-9));
    }

    [Test]
    public void 互动权重占比正确()
    {
        // 纯互动文档（10 点赞）：interaction = log10(11) ≈ 1.0414，view=0，freshness=1
        var q = new MarkQuote(loveSome: 10);
        var score = MarkdownHeatFormula.Calculate(q, Now, Now);
        var expected = 0.5 * Math.Log10(11) + 0.2;
        Assert.That(score, Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void 浏览权重占比正确()
    {
        // 纯浏览文档（1000 浏览）：view = log10(1001) ≈ 3.0004
        var q = new MarkQuote(viewSome: 1000);
        var score = MarkdownHeatFormula.Calculate(q, Now, Now);
        var expected = 0.3 * Math.Log10(1001) + 0.2;
        Assert.That(score, Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void 互动子项加权正确()
    {
        // Love×1 + Favorite×2 + Share×3 + Coin×5
        var q = new MarkQuote(loveSome: 1, favoriteSome: 1, shareSome: 1, coinSome: 1);
        var score = MarkdownHeatFormula.Calculate(q, Now, Now);
        var expected = 0.5 * Math.Log10(1 + 1 + 2 + 3 + 5) + 0.2;
        Assert.That(score, Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void 时间衰减随年龄单调下降()
    {
        var q = new MarkQuote(loveSome: 5, viewSome: 10);
        var fresh = MarkdownHeatFormula.Calculate(q, Now, Now.AddDays(1));
        var old = MarkdownHeatFormula.Calculate(q, Now, Now.AddDays(14));
        Assert.That(fresh, Is.GreaterThan(old), "新文档热度应高于老文档");
    }

    [Test]
    public void 七天半衰期衰减值正确()
    {
        var q = new MarkQuote(loveSome: 5);
        var created = Now.AddDays(-7);
        // freshness = exp(-1) ≈ 0.3679
        var score = MarkdownHeatFormula.Calculate(q, created, Now);
        var expected = 0.5 * Math.Log10(6) + 0.2 * Math.Exp(-1);
        Assert.That(score, Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void 未来时间不产生负衰减()
    {
        var q = new MarkQuote(loveSome: 1);
        var score = MarkdownHeatFormula.Calculate(q, Now, Now.AddDays(-3));
        // ageDays 被钳制为 0 → freshness = 1
        var expected = 0.5 * Math.Log10(2) + 0.2;
        Assert.That(score, Is.EqualTo(expected).Within(1e-9));
    }
}
