using Message.Domain.Entities.User;

namespace Message.Tests.Domain;

/// <summary>
/// 用户资料聚合（UserInfo）单元测试。
/// 覆盖：创建校验与默认值、背景封面更新、硬币增加/扣除边界、等级设置。
/// </summary>
[TestFixture]
public class UserInfoAggregateTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Test]
    public void Create_空UserId_应抛出ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => UserInfo.Create(Guid.Empty));
    }

    [Test]
    public void Create_有效UserId_应返回默认等级1与硬币0()
    {
        var info = UserInfo.Create(UserId);

        Assert.Multiple(() =>
        {
            Assert.That(info.UserId, Is.EqualTo(UserId));
            Assert.That(info.Level, Is.EqualTo(1));
            Assert.That(info.Coins, Is.EqualTo(0));
            Assert.That(info.BackgroundCoverUrl, Is.Null);
        });
    }

    [Test]
    public void UpdateBackgroundCover_有效URL_应更新()
    {
        var info = UserInfo.Create(UserId);
        const string url = "https://example.com/cover.png";

        info.UpdateBackgroundCover(new Uri(url));

        Assert.That(info.BackgroundCoverUrl, Is.EqualTo(new Uri(url)));
    }

    [Test]
    public void UpdateBackgroundCover_空白值_应清除()
    {
        var info = UserInfo.Create(UserId);
        info.UpdateBackgroundCover(new Uri("https://example.com/cover.png"));

        info.UpdateBackgroundCover(null);

        Assert.That(info.BackgroundCoverUrl, Is.Null);
    }

    [Test]
    public void UpdateBackgroundCover_超长URL_应抛出ArgumentException()
    {
        var info = UserInfo.Create(UserId);

        Assert.Throws<ArgumentException>(() => info.UpdateBackgroundCover(new Uri("https://example.com/" + new string('a', 2049))));
    }

    [Test]
    public void AddCoins_正数_应累加余额()
    {
        var info = UserInfo.Create(UserId);

        info.AddCoins(100);
        info.AddCoins(50);

        Assert.That(info.Coins, Is.EqualTo(150));
    }

    [Test]
    public void AddCoins_零或负数_应抛出ArgumentException()
    {
        var info = UserInfo.Create(UserId);

        Assert.Throws<ArgumentException>(() => info.AddCoins(0));
        Assert.Throws<ArgumentException>(() => info.AddCoins(-10));
    }

    [Test]
    public void ConsumeCoins_余额充足_应扣减()
    {
        var info = UserInfo.Create(UserId);
        info.AddCoins(100);

        info.ConsumeCoins(40);

        Assert.That(info.Coins, Is.EqualTo(60));
    }

    [Test]
    public void ConsumeCoins_余额不足_应抛出InvalidOperationException()
    {
        var info = UserInfo.Create(UserId);
        info.AddCoins(10);

        Assert.Throws<InvalidOperationException>(() => info.ConsumeCoins(11));
    }

    [Test]
    public void ConsumeCoins_零或负数_应抛出ArgumentException()
    {
        var info = UserInfo.Create(UserId);
        info.AddCoins(100);

        Assert.Throws<ArgumentException>(() => info.ConsumeCoins(0));
        Assert.Throws<ArgumentException>(() => info.ConsumeCoins(-5));
    }

    [Test]
    public void SetLevel_合法等级_应更新()
    {
        var info = UserInfo.Create(UserId);

        info.SetLevel(5);

        Assert.That(info.Level, Is.EqualTo(5));
    }

    [Test]
    public void SetLevel_小于1_应抛出ArgumentException()
    {
        var info = UserInfo.Create(UserId);

        Assert.Throws<ArgumentException>(() => info.SetLevel(0));
    }
}
