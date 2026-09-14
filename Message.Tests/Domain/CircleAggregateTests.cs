using Message.Domain.Entities.Community;
using Message.Domain.Enums;

namespace Message.Tests.Domain;

/// <summary>
/// 兴趣圈子聚合根（<see cref="Circle"/>）单元测试。
/// 覆盖成员上限判定：上限按「有效成员」（Active）计算，历史退出（Left）/被移出（Banned）成员不占名额；
/// 以及重新加入复用同一成员行、不重复计数。
/// </summary>
[TestFixture]
public class CircleAggregateTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Test]
    public void 成员上限_历史退出成员不占名额_可继续加入()
    {
        var circle = Circle.Create(OwnerId, "测试圈子", maxMembers: 2);
        var leaving = Guid.NewGuid();
        circle.AddMember(leaving);
        circle.RemoveMember(leaving);

        var joiner = Guid.NewGuid();
        Assert.DoesNotThrow(() => circle.AddMember(joiner));
        Assert.That(circle.IsMember(joiner), Is.True);
        Assert.That(circle.IsMember(leaving), Is.False);
        Assert.That(circle.MemberCount, Is.EqualTo(2));
    }

    [Test]
    public void 成员上限_被移出成员不占名额_可继续加入()
    {
        var circle = Circle.Create(OwnerId, "测试圈子", maxMembers: 2);
        var banned = Guid.NewGuid();
        circle.AddMember(banned);
        circle.BanMember(banned, OwnerId);

        var joiner = Guid.NewGuid();
        Assert.DoesNotThrow(() => circle.AddMember(joiner));
        Assert.That(circle.IsMember(joiner), Is.True);
        Assert.That(circle.MemberCount, Is.EqualTo(2));
    }

    [Test]
    public void 成员上限_有效成员达上限_加入抛异常()
    {
        var circle = Circle.Create(OwnerId, "测试圈子", maxMembers: 2);
        circle.AddMember(Guid.NewGuid());

        var ex = Assert.Throws<InvalidOperationException>(() => circle.AddMember(Guid.NewGuid()));
        Assert.That(ex!.Message, Does.Contain("上限"));
        Assert.That(circle.MemberCount, Is.EqualTo(2));
    }

    [Test]
    public void 重新加入_恢复为有效成员且不重复占用名额()
    {
        var circle = Circle.Create(OwnerId, "测试圈子", maxMembers: 2);
        var userId = Guid.NewGuid();
        circle.AddMember(userId);
        circle.RemoveMember(userId);

        circle.AddMember(userId);

        Assert.That(circle.IsMember(userId), Is.True);
        Assert.That(circle.GetActiveMember(userId)!.Status, Is.EqualTo(CircleMemberStatus.Active));
        Assert.That(circle.Members.Count, Is.EqualTo(2), "重新加入应复用同一成员行");
        Assert.That(circle.MemberCount, Is.EqualTo(2));
    }

    [Test]
    public void 被移出成员腾出的名额_可被新成员使用_超限时仍拦截()
    {
        var circle = Circle.Create(OwnerId, "测试圈子", maxMembers: 3);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        circle.AddMember(first);
        circle.AddMember(second);      // 有效成员 3：圈主 + first + second
        circle.BanMember(second, OwnerId);

        Assert.DoesNotThrow(() => circle.AddMember(Guid.NewGuid()));  // 用掉被移出者腾出的名额，有效成员 3
        Assert.Throws<InvalidOperationException>(() => circle.AddMember(Guid.NewGuid()));
        Assert.That(circle.MemberCount, Is.EqualTo(3));
    }
}
