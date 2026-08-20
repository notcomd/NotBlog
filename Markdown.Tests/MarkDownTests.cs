using Markdown.Domain.Entities;

namespace Markdown.Tests;

/// <summary>
///     MarkDown 聚合根领域测试：审核状态机 / 评论树软删除 / 历史快照去重 / 子评论 / 权限 / 文件元数据 / 交互计数
/// </summary>
[TestFixture]
public class MarkDownTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    private static MarkDown CreateDefault(string name = "测试文章", string fileId = "f1.md", string hash = "h1")
        => new(Owner, name, fileId, fileId, 1024, ".md", hash);

    private static void UpdateFile(MarkDown md, string fileId, string hash)
        => md.UpdateByMarkDownAsync(md.MarkDownName, fileId, fileId, 2048, ".md", hash);

    // ==================== 审核状态机 ====================

    [Test]
    public void 初始状态为草稿()
    {
        var md = CreateDefault();
        Assert.That(md.Status, Is.EqualTo(MarkStatus.MarkDraft));
        Assert.That(md.IsApproved, Is.False);
    }

    [Test]
    public void 草稿提交后进入待审核()
    {
        var md = CreateDefault();
        md.SubmitForReview();
        Assert.That(md.Status, Is.EqualTo(MarkStatus.MarkPendingReview));
    }

    [Test]
    public void 待审核状态重复提交抛异常()
    {
        var md = CreateDefault();
        md.SubmitForReview();
        Assert.Throws<InvalidOperationException>(() => md.SubmitForReview());
    }

    [Test]
    public void 待审核通过后对外可见()
    {
        var md = CreateDefault();
        md.SubmitForReview();
        md.Approve();
        Assert.That(md.Status, Is.EqualTo(MarkStatus.MarkApproved));
        Assert.That(md.IsApproved, Is.True);
    }

    [Test]
    public void 已通过文章不可重复通过或驳回()
    {
        var md = CreateDefault();
        md.SubmitForReview();
        md.Approve();
        Assert.Throws<InvalidOperationException>(() => md.Approve());
        Assert.Throws<InvalidOperationException>(() => md.Reject());
    }

    [Test]
    public void 驳回后可重新提交并再次通过()
    {
        var md = CreateDefault();
        md.SubmitForReview();
        md.Reject();
        Assert.That(md.Status, Is.EqualTo(MarkStatus.MarkRejected));

        md.SubmitForReview();   // 驳回 -> 待审核
        md.Approve();           // 待审核 -> 通过
        Assert.That(md.IsApproved, Is.True);
    }

    [Test]
    public void 草稿状态不可直接通过()
    {
        var md = CreateDefault();
        Assert.Throws<InvalidOperationException>(() => md.Approve());
    }

    // ==================== 评论树软删除（P1-1） ====================

    [Test]
    public void 软删除评论会连带整棵子树且保留记录()
    {
        var md = CreateDefault();
        var top = new MarkReview(md.MarkDownGuid, Owner, "顶级评论", null);
        md.AddByMarkReviewAsync(top);

        var child = new MarkReview(md.MarkDownGuid, Owner, "子评论", null);
        md.AddChildReview(top.MarkReviewGuid, child);

        var grandchild = new MarkReview(md.MarkDownGuid, Owner, "孙评论", null);
        md.AddChildReview(child.MarkReviewGuid, grandchild);

        md.SoftDeleteReview(top.MarkReviewGuid);

        Assert.That(top.IsDelete, Is.True, "顶级评论应被软删除");
        Assert.That(child.IsDelete, Is.True, "子评论应被连带软删除");
        Assert.That(grandchild.IsDelete, Is.True, "孙评论应被连带软删除");
        // 记录保留在聚合内（软删除不是物理删除）
        Assert.That(md.FindReview(top.MarkReviewGuid), Is.Not.Null);
        Assert.That(md.FindReview(child.MarkReviewGuid), Is.Not.Null);
        Assert.That(md.FindReview(grandchild.MarkReviewGuid), Is.Not.Null);
    }

    [Test]
    public void 删除不存在的评论抛异常()
    {
        var md = CreateDefault();
        Assert.Throws<InvalidOperationException>(() => md.SoftDeleteReview(Guid.NewGuid()));
    }

    [Test]
    public void 软删除只影响目标子树不影响其他评论()
    {
        var md = CreateDefault();
        var a = new MarkReview(md.MarkDownGuid, Owner, "A", null);
        var b = new MarkReview(md.MarkDownGuid, Owner, "B", null);
        md.AddByMarkReviewAsync(a);
        md.AddByMarkReviewAsync(b);

        md.SoftDeleteReview(a.MarkReviewGuid);

        Assert.That(a.IsDelete, Is.True);
        Assert.That(b.IsDelete, Is.False, "无关评论不应受影响");
    }

    // ==================== 子评论（聚合一致性） ====================

    [Test]
    public void 添加子评论维护父评论归属与回复计数()
    {
        var md = CreateDefault();
        var top = new MarkReview(md.MarkDownGuid, Owner, "顶级", null);
        md.AddByMarkReviewAsync(top);

        var child = new MarkReview(md.MarkDownGuid, Owner, "子", null);
        md.AddChildReview(top.MarkReviewGuid, child);

        Assert.That(child.MarkAggregateRootGuid, Is.EqualTo(top.MarkReviewGuid));
        Assert.That(top.ReviewQuote.ReplySome, Is.EqualTo(1), "父评论回复计数应 +1");
        Assert.That(md.FindReview(child.MarkReviewGuid), Is.Not.Null, "子评论应纳入聚合扁平集合");
    }

    [Test]
    public void 向不存在的父评论添加子评论抛异常()
    {
        var md = CreateDefault();
        var child = new MarkReview(md.MarkDownGuid, Owner, "子", null);
        Assert.Throws<InvalidOperationException>(() => md.AddChildReview(Guid.NewGuid(), child));
    }

    // ==================== 历史快照去重（P1-8，正文来自文件流参数） ====================

    [Test]
    public void 相同内容不重复快照()
    {
        var md = CreateDefault(hash: "hash-1");
        Assert.That(md.CreateHistorySnapshot("旧内容"), Is.Not.Null, "首次快照应创建");
        Assert.That(md.CreateHistorySnapshot("旧内容"), Is.Null, "内容未变时不应重复快照");
        Assert.That(md.OldMarkDowns.Count, Is.EqualTo(1));
    }

    [Test]
    public void 内容变化后再快照会新增版本()
    {
        var md = CreateDefault(hash: "hash-1");
        md.CreateHistorySnapshot("旧内容");
        UpdateFile(md, "f2.md", "hash-2");
        md.CreateHistorySnapshot("内容B");

        Assert.That(md.OldMarkDowns.Count, Is.EqualTo(2));
        Assert.That(md.OldMarkDowns.Select(o => o.OldMarkDownHash), Does.Contain("hash-1"));
        Assert.That(md.OldMarkDowns.Select(o => o.OldMarkDownHash), Does.Contain("hash-2"));
    }

    [Test]
    public void 内容改回历史版本不会重复快照()
    {
        var md = CreateDefault(hash: "hash-1");
        md.CreateHistorySnapshot("旧内容");              // 历史: [h1]
        UpdateFile(md, "f2.md", "hash-2");
        md.CreateHistorySnapshot("内容B");              // 历史: [h1, h2]
        UpdateFile(md, "f3.md", "hash-1");  // 改回 h1
        md.CreateHistorySnapshot("内容A");              // h1 已存在 -> 不重复

        Assert.That(md.OldMarkDowns.Count, Is.EqualTo(2));
    }

    // ==================== 文件元数据与更新 ====================

    [Test]
    public void 更新内容刷新文件元数据与更新时间()
    {
        var md = CreateDefault();
        var before = md.UpdateAt;
        md.UpdateByMarkDownAsync("新名称", "f2.md", "f2.md", 2048, ".md", "hash-2");

        Assert.That(md.MarkDownName, Is.EqualTo("新名称"));
        Assert.That(md.FileId, Is.EqualTo("f2.md"));
        Assert.That(md.FileUri, Is.EqualTo("f2.md"));
        Assert.That(md.FileSize, Is.EqualTo(2048));
        Assert.That(md.FileExt, Is.EqualTo(".md"));
        Assert.That(md.MarkDownHash, Is.EqualTo("hash-2"));
        Assert.That(md.UpdateAt >= before, Is.True);
    }

    [Test]
    public void 空参数更新抛异常()
    {
        var md = CreateDefault();
        Assert.Throws<ArgumentNullException>(() => md.UpdateByMarkDownAsync("", "f2.md", "f2.md", 1, ".md", "h"));
        Assert.Throws<ArgumentNullException>(() => md.UpdateByMarkDownAsync("名称", "", "f2.md", 1, ".md", "h"));
        Assert.Throws<ArgumentNullException>(() => md.UpdateByMarkDownAsync("名称", "f2.md", "f2.md", 1, ".md", ""));
    }

    [Test]
    public void 从历史版本还原更新文件元数据()
    {
        var md = CreateDefault(hash: "hash-1");
        md.CreateHistorySnapshot("旧内容");
        UpdateFile(md, "f2.md", "hash-2");

        var old = md.OldMarkDowns.Single(o => o.OldMarkDownHash == "hash-1");
        md.RestoreFromHistory(old, "f3.md", "f3.md", 4096, ".md");

        Assert.That(md.MarkDownHash, Is.EqualTo("hash-1"), "还原后哈希应为历史版本哈希");
        Assert.That(md.FileId, Is.EqualTo("f3.md"));
        Assert.That(md.FileSize, Is.EqualTo(4096));
    }

    // ==================== 文档交互计数（MarkQuote 委托） ====================

    [Test]
    public void 文档计数方法委托MarkQuote()
    {
        var md = CreateDefault();
        Assert.That(md.AddLove(), Is.EqualTo(1));
        Assert.That(md.AddFavorite(), Is.EqualTo(1));
        Assert.That(md.AddShare(), Is.EqualTo(1));
        Assert.That(md.AddCoin(), Is.EqualTo(1));
        Assert.That(md.AddView(5), Is.EqualTo(5));
        Assert.That(md.RemoveLove(), Is.EqualTo(0));

        Assert.That(md.MarkQuote.LoveSome, Is.EqualTo(0));
        Assert.That(md.MarkQuote.FavoriteSome, Is.EqualTo(1));
        Assert.That(md.MarkQuote.ShareSome, Is.EqualTo(1));
        Assert.That(md.MarkQuote.CoinSome, Is.EqualTo(1));
        Assert.That(md.MarkQuote.ViewSome, Is.EqualTo(5));
    }

    // ==================== 热点分重算 ====================

    [Test]
    public void 重算热点分写回MarkQuote()
    {
        var md = CreateDefault();
        md.AddLove(10);
        md.AddView(100);

        var now = DateTimeOffset.UtcNow;
        var score = md.RecalculateHotScore(now);

        Assert.That(score, Is.EqualTo(md.MarkQuote.HeatScore), "重算返回值应与 MarkQuote.HeatScore 一致");
        Assert.That(score, Is.GreaterThan(0));
    }

    [Test]
    public void 热度分随计数增加而提高()
    {
        var md = CreateDefault();
        var now = DateTimeOffset.UtcNow;
        var baseScore = md.RecalculateHotScore(now);

        md.AddLove(5);
        md.AddCoin(2);
        var higherScore = md.RecalculateHotScore(now);

        Assert.That(higherScore, Is.GreaterThan(baseScore));
    }

    // ==================== 权限 ====================

    [Test]
    public void 公开文档所有人可访问()
    {
        var md = CreateDefault();
        Assert.That(md.HasPermission(Owner), Is.True);
        Assert.That(md.HasPermission(Other), Is.True);
    }

    [Test]
    public void 私有文档仅所有者可访问()
    {
        var md = new MarkDown.MarkDownBuilder(Owner, "私有", "f1.md", "f1.md", 1024, ".md", "h")
            .WithMarkDownAuth(MarkDownAuth.PrivateMark)
            .Build();
        Assert.That(md.HasPermission(Owner), Is.True);
        Assert.That(md.HasPermission(Other), Is.False);
    }

    [Test]
    public void 软删除与恢复()
    {
        var md = CreateDefault();
        md.SoftDelete();
        Assert.That(md.IsDelete, Is.True);
        md.Restore();
        Assert.That(md.IsDelete, Is.False);
    }
}
