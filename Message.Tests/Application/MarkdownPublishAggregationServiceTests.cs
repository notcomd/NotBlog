using Message.Web.API.Background;

namespace Message.Tests.Application;

/// <summary>
///     Markdown 发布通知聚合文案（BuildAggregatedCopy 纯函数）测试：
///     单篇 / 多篇聚合 / 昵称去重 / 溢出省略
/// </summary>
[TestFixture]
public class MarkdownPublishAggregationServiceTests
{
    private static Dictionary<string, string> Entry(string fileName, string nick)
        => new()
        {
            { $"{Guid.NewGuid():N}:{Guid.NewGuid():N}", $"{fileName}|{nick}" }
        };

    [Test]
    public void 窗口内仅一篇_保持单条文案()
    {
        var aggregated = MarkdownPublishAggregationService.BuildAggregatedCopy(Entry("文章A", "小明"));

        Assert.That(aggregated.Title, Is.EqualTo("好友/关注者发布了新文章"));
        Assert.That(aggregated.Content, Does.Contain("小明"));
        Assert.That(aggregated.Content, Does.Contain("文章A"));
        Assert.That(aggregated.RefGuid, Is.Not.EqualTo(Guid.Empty), "refGuid 应指向某篇文章便于前端跳转");
    }

    [Test]
    public void 多篇聚合为一条且昵称去重()
    {
        var entries = Entry("文章A", "小明");
        foreach (var (fileName, nick) in new[] { ("文章B", "小红"), ("文章C", "小明") })
        {
            foreach (var (key, value) in Entry(fileName, nick))
            {
                entries[key] = value;
            }
        }

        var aggregated = MarkdownPublishAggregationService.BuildAggregatedCopy(entries);

        Assert.That(aggregated.Title, Is.EqualTo("好友/关注者动态"));
        Assert.That(aggregated.Content, Does.Contain("3 位好友/关注者发布了新文章"));
        Assert.That(aggregated.Content, Does.Contain("小明"));
        Assert.That(aggregated.Content, Does.Contain("小红"));
        Assert.That(aggregated.Content, Does.Not.Contain("等"), "昵称去重后 2 个不溢出，不应追加省略号");
    }

    [Test]
    public void 昵称数量超上限时追加省略号()
    {
        var entries = Entry("文章0", "用户0");
        for (var i = 1; i < 5; i++)
        {
            foreach (var (key, value) in Entry($"文章{i}", $"用户{i}"))
            {
                entries[key] = value;
            }
        }

        var aggregated = MarkdownPublishAggregationService.BuildAggregatedCopy(entries);

        Assert.That(aggregated.Content, Does.Contain("5 位好友/关注者发布了新文章"));
        Assert.That(aggregated.Content, Does.EndWith("等"));
    }
}