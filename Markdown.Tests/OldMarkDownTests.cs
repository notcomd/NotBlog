using Markdown.Domain.Entities;

namespace Markdown.Tests;

/// <summary>
///     OldMarkDown 历史版本测试：创建快照、软删除、哈希比对
/// </summary>
[TestFixture]
public class OldMarkDownTests
{
    [Test]
    public void 历史版本记录内容哈希与权限()
    {
        var guid = Guid.NewGuid();
        var old = new OldMarkDown(guid, Guid.NewGuid(), "旧内容", "old-hash", MarkDownAuth.PrivateMark);

        Assert.That(old.MarkDownGuid, Is.EqualTo(guid));
        Assert.That(old.OldMarkDownContent, Is.EqualTo("旧内容"));
        Assert.That(old.OldMarkDownHash, Is.EqualTo("old-hash"));
        Assert.That(old.AuthType, Is.EqualTo(MarkDownAuth.PrivateMark), "快照权限应与文档一致");
        Assert.That(old.IsDelete, Is.False);
    }

    [Test]
    public void 空内容构造抛异常()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OldMarkDown(Guid.NewGuid(), Guid.NewGuid(), null!, "hash", MarkDownAuth.PublicMark));
    }

    [Test]
    public void 软删除历史版本()
    {
        var old = new OldMarkDown(Guid.NewGuid(), Guid.NewGuid(), "内容", "hash", MarkDownAuth.PublicMark);
        old.SoftDelete();
        Assert.That(old.IsDelete, Is.True);
    }
}
