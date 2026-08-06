using Markdown.Domain.Entities;

namespace Markdown.Tests;

/// <summary>
///     MarkDown 聚合根领域测试：审核状态机 / 评论树软删除 / 历史快照去重 / 子评论 / 权限
/// </summary>
[TestFixture]
public class MarkDownTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    private static MarkDown CreateDefault(string name = "测试文章", string content = "内容", string hash = "h1")
        => new(Owner, name, content, hash);

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
        Assert.That(top.MarkQuote.ReviewSome, Is.EqualTo(1), "父评论回复计数应 +1");
        Assert.That(md.FindReview(child.MarkReviewGuid), Is.Not.Null, "子评论应纳入聚合扁平集合");
    }

    [Test]
    public void 向不存在的父评论添加子评论抛异常()
    {
        var md = CreateDefault();
        var child = new MarkReview(md.MarkDownGuid, Owner, "子", null);
        Assert.Throws<InvalidOperationException>(() => md.AddChildReview(Guid.NewGuid(), child));
    }

    // ==================== 历史快照去重（P1-8） ====================

    [Test]
    public void 相同内容不重复快照()
    {
        var md = CreateDefault(hash: "hash-1");
        Assert.That(md.CreateHistorySnapshot(), Is.Not.Null, "首次快照应创建");
        Assert.That(md.CreateHistorySnapshot(), Is.Null, "内容未变时不应重复快照");
        Assert.That(md.OldMarkDowns.Count, Is.EqualTo(1));
    }

    [Test]
    public void 内容变化后再快照会新增版本()
    {
        var md = CreateDefault(hash: "hash-1");
        md.CreateHistorySnapshot();
        md.UpdateByMarkDownAsync("测试文章", "新内容", "hash-2");
        md.CreateHistorySnapshot();

        Assert.That(md.OldMarkDowns.Count, Is.EqualTo(2));
        Assert.That(md.OldMarkDowns.Select(o => o.OldMarkDownHash), Does.Contain("hash-1"));
        Assert.That(md.OldMarkDowns.Select(o => o.OldMarkDownHash), Does.Contain("hash-2"));
    }

    [Test]
    public void 内容改回历史版本不会重复快照()
    {
        var md = CreateDefault(hash: "hash-1");
        md.CreateHistorySnapshot();              // 历史: [h1]
        md.UpdateByMarkDownAsync("测试文章", "内容B", "hash-2");
        md.CreateHistorySnapshot();              // 历史: [h1, h2]
        md.UpdateByMarkDownAsync("测试文章", "内容A", "hash-1");  // 改回 h1
        md.CreateHistorySnapshot();              // h1 已存在 -> 不重复

        Assert.That(md.OldMarkDowns.Count, Is.EqualTo(2));
    }

    // ==================== 更新与权限 ====================

    [Test]
    public void 更新内容刷新内容与更新时间()
    {
        var md = CreateDefault();
        var before = md.UpdateAt;
        md.UpdateByMarkDownAsync("新名称", "新内容", "hash-2");

        Assert.That(md.MarkDownName, Is.EqualTo("新名称"));
        Assert.That(md.MarkDownContent, Is.EqualTo("新内容"));
        Assert.That(md.MarkDownHash, Is.EqualTo("hash-2"));
        Assert.That(md.UpdateAt >= before, Is.True);
    }

    [Test]
    public void 空参数更新抛异常()
    {
        var md = CreateDefault();
        Assert.Throws<ArgumentNullException>(() => md.UpdateByMarkDownAsync("", "内容", "h"));
        Assert.Throws<ArgumentNullException>(() => md.UpdateByMarkDownAsync("名称", "", "h"));
    }

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
        var md = new MarkDown.MarkDownBuilder(Owner, "私有", "内容", "h")
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
