namespace Markdown.Domain.Entities;

/// <summary>
///     文档
/// </summary>
public class MarkDown : Entity, IAggregateRoot
{
    private MarkDown()
    {
        MarkDownGuid = Guid.CreateVersion7();
        MarkDownTagboard = [];
        MarkReviews = [];
        OldMarkDowns = [];
        CreateAt = DateTimeOffset.UtcNow;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    // 私有全参数构造函数，供 Builder 调用
    private MarkDown(Guid markUserGuid, string markDownName, string markDownContent, string markDownHash,
        Guid markReviewGuid, HashSet<string> markDownTagboard, MarkDownAuth markDownAuth,
        MarkOption markOption) : this()
    {
        MarkUserGuid = markUserGuid;
        MarkDownName = markDownName;
        MarkDownContent = markDownContent;
        MarkDownHash = markDownHash;
        MarkReviewGuid = markReviewGuid;
        MarkDownTagboard = markDownTagboard;
        MarkDownAuth = markDownAuth;
        MarkOption = markOption;
        IsDelete = false;
    }

    // 公有简化构造函数，使用默认值调用私有构造函数
    public MarkDown(Guid markUserGuid, string markDownName, string markDownContent, string markDownHash)
        : this(markUserGuid, markDownName, markDownContent, markDownHash, Guid.Empty, [],
            MarkDownAuth.PublicMark, MarkOption.Default)
    {
    }

    public Guid MarkDownGuid { get; init; }

    public Guid MarkReviewGuid { get; init; }

    public Guid MarkUserGuid { get; init; }

    public string MarkDownName { get; private set; } = null!;

    public HashSet<string> MarkDownTagboard { get; private set; }

    public MarkDownAuth MarkDownAuth { get; private set; } = MarkDownAuth.PublicMark;

    public MarkOption MarkOption { get; private set; } = MarkOption.Default;

    public string MarkDownHash { get; private set; } = null!;

    public string MarkDownContent { get; private set; } = null!;

    public bool IsDelete { get; private set; }

    /// <summary>
    ///     审核状态（草稿/待审核/通过/驳回），默认草稿
    /// </summary>
    public MarkStatus Status { get; private set; } = MarkStatus.MarkDraft;

    public DateTimeOffset CreateAt { get; init; }

    public DateTimeOffset UpdateAt { get; private set; }

    public ICollection<MarkReview> MarkReviews { get; private set; }

    public ICollection<OldMarkDown> OldMarkDowns { get; private set; }

    /// <summary>
    ///     添加评论到文档（聚合根统一入口）
    /// </summary>
    /// <param name="markReview">要添加的评论</param>
    /// <returns>当前文档实例（支持链式调用）</returns>
    public Task<MarkDown> AddByMarkReviewAsync(MarkReview markReview)
    {
        ArgumentNullException.ThrowIfNull(markReview);
        MarkReviews.Add(markReview);
        return Task.FromResult(this);
    }

    /// <summary>
    ///     从聚合中移除评论及其所有子评论（聚合根统一入口）
    /// </summary>
    /// <param name="reviewGuid">要移除的评论 GUID</param>
    public void RemoveReview(Guid reviewGuid)
    {
        var review = FindReview(reviewGuid);
        if (review is null)
            throw new InvalidOperationException($"评论 {reviewGuid} 不存在于当前文档聚合中");

        // 递归移除子评论
        var childReviews = MarkReviews
            .Where(r => r.MarkAggregateRootGuid == reviewGuid)
            .ToList();

        foreach (var child in childReviews)
        {
            MarkReviews.Remove(child);
        }

        MarkReviews.Remove(review);
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     添加子评论到父评论（聚合根统一入口，维护聚合内一致性）
    /// </summary>
    /// <param name="parentReviewGuid">父评论 GUID</param>
    /// <param name="childReview">子评论</param>
    /// <returns>子评论实例</returns>
    public MarkReview AddChildReview(Guid parentReviewGuid, MarkReview childReview)
    {
        ArgumentNullException.ThrowIfNull(childReview);

        var parentReview = FindReview(parentReviewGuid)
            ?? throw new InvalidOperationException($"父评论 {parentReviewGuid} 不存在于当前文档聚合中");

        childReview.SetParentReviewGuid(parentReviewGuid);
        parentReview.MarkReviews.Add(childReview);
        parentReview.MarkQuote.AddReview();
        MarkReviews.Add(childReview);

        UpdateAt = DateTimeOffset.UtcNow;
        return childReview;
    }

    /// <summary>
    ///     在聚合内查找指定评论
    /// </summary>
    /// <param name="reviewGuid">评论 GUID</param>
    /// <returns>找到的评论，如果不存在返回 null</returns>
    public MarkReview? FindReview(Guid reviewGuid)
    {
        return MarkReviews.FirstOrDefault(r => r.MarkReviewGuid == reviewGuid);
    }

    /// <summary>
    ///     更新文档内容（同时创建历史版本）
    /// </summary>
    /// <param name="markDownName">新名称</param>
    /// <param name="markDownContent">新内容</param>
    /// <param name="markDownHash">新哈希值</param>
    /// <returns>当前文档实例（支持链式调用）</returns>
    public Task<MarkDown> UpDataByMarkDownAsync(string markDownName, string markDownContent, string markDownHash)
    {
        if (string.IsNullOrWhiteSpace(markDownName))
            throw new ArgumentNullException(nameof(markDownName));
        if (string.IsNullOrWhiteSpace(markDownContent))
            throw new ArgumentNullException(nameof(markDownContent));
        if (string.IsNullOrWhiteSpace(markDownHash))
            throw new ArgumentNullException(nameof(markDownHash));

        // 如果内容发生变化，应该先创建历史版本记录（由应用层负责）
        MarkDownName = markDownName;
        MarkDownContent = markDownContent;
        MarkDownHash = markDownHash;
        UpdateAt = DateTimeOffset.UtcNow;

        return Task.FromResult(this);
    }

    /// <summary>
    ///     验证文档哈希值是否匹配
    /// </summary>
    /// <param name="markMd5">要比较的 MD5 哈希值</param>
    /// <returns>如果匹配返回 true</returns>
    public bool IsMarkDownEques(string markMd5) => MarkDownHash == markMd5;

    /// <summary>
    ///     提交审核：草稿/驳回 -> 待审核
    /// </summary>
    public void SubmitForReview()
    {
        if (Status is not (MarkStatus.MarkDraft or MarkStatus.MarkRejected))
            throw new InvalidOperationException($"当前状态 {Status} 无法提交审核，仅草稿或驳回状态可提交");

        Status = MarkStatus.MarkPendingReview;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     审核通过：待审核 -> 通过
    /// </summary>
    public void Approve()
    {
        if (Status != MarkStatus.MarkPendingReview)
            throw new InvalidOperationException($"当前状态 {Status} 无法通过审核，仅待审核状态可通过");

        Status = MarkStatus.MarkApproved;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     审核驳回：待审核 -> 驳回
    /// </summary>
    public void Reject()
    {
        if (Status != MarkStatus.MarkPendingReview)
            throw new InvalidOperationException($"当前状态 {Status} 无法驳回，仅待审核状态可驳回");

        Status = MarkStatus.MarkRejected;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     是否已通过审核（对外可见）
    /// </summary>
    public bool IsApproved => Status == MarkStatus.MarkApproved;

    /// <summary>
    ///     软删除文档
    /// </summary>
    public void SoftDelete()
    {
        IsDelete = true;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     恢复已删除的文档
    /// </summary>
    public void Restore()
    {
        IsDelete = false;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     添加标签到标签板
    /// </summary>
    /// <param name="tag">要添加的标签</param>
    public void AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !MarkDownTagboard.Contains(tag))
        {
            MarkDownTagboard.Add(tag);
            UpdateAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    ///     批量添加标签到标签板
    /// </summary>
    /// <param name="tags">要添加的标签集合</param>
    public void AddTags(IEnumerable<string> tags)
    {
        foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            MarkDownTagboard.Add(tag);
        }

        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     移除标签
    /// </summary>
    /// <param name="tag">要移除的标签</param>
    /// <returns>如果成功移除返回 true，标签不存在返回 false</returns>
    public bool RemoveTag(string tag)
    {
        if (MarkDownTagboard.Remove(tag))
        {
            UpdateAt = DateTimeOffset.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     清空所有标签
    /// </summary>
    public void ClearTags()
    {
        if (MarkDownTagboard.Count > 0)
        {
            MarkDownTagboard.Clear();
            UpdateAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    ///     检查是否包含指定标签
    /// </summary>
    /// <param name="tag">要检查的标签</param>
    /// <returns>如果包含返回 true</returns>
    public bool HasTag(string tag)
    {
        return MarkDownTagboard.Contains(tag);
    }

    /// <summary>
    ///     更新文档权限设置
    /// </summary>
    /// <param name="markOption">新的权限选项</param>
    public void UpdateMarkOption(MarkDownAuth markOption)
    {
        MarkDownAuth = markOption;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 获取文档统计信息
    /// </summary>
    /// <returns>包含评论数、标签数等信息的匿名对象</returns>
    public object GetStatistics()
    {
        return new
        {
            ReviewCount = MarkReviews?.Count ?? 0,
            TagCount = MarkDownTagboard?.Count ?? 0,
            HistoryCount = OldMarkDowns?.Count ?? 0,
            ContentLength = MarkDownContent?.Length ?? 0,
            LastUpdateTime = UpdateAt,
            CreateTime = CreateAt
        };
    }

    /// <summary>
    /// 验证用户是否有权限操作此文档
    /// </summary>
    /// <param name="userGuid">用户 GUID</param>
    /// <returns>如果有权限返回 true</returns>
    public bool HasPermission(Guid userGuid)
    {
        // 文档所有者始终有权限
        if (MarkUserGuid == userGuid)
            return true;
        var markDownAuth = MarkDownAuth;
        // 根据权限类型判断
        return markDownAuth switch
        {
            MarkDownAuth.PublicMark => true, // 公开文档所有人可访问
            MarkDownAuth.PrivateMark => false, // 私有文档只有所有者可访问
            MarkDownAuth.ProtectedMark => false, // 受保护文档需要额外验证
            MarkDownAuth.AdminMark => false, // 管理员文档
            MarkDownAuth.RootMark => false, // 根管理员文档
            _ => false
        };
    }

    /// <summary>
    ///     创建历史版本快照并纳入聚合管理（用于更新前保存旧版本）
    ///     快照自动加入 OldMarkDowns 集合，由 EF Core 级联持久化
    /// </summary>
    /// <returns>新创建的 OldMarkDown 实例</returns>
    public OldMarkDown CreateHistorySnapshot()
    {
        var oldVersion = new OldMarkDown(
            MarkDownGuid,
            MarkUserGuid,
            MarkDownContent,
            MarkDownHash,
            MarkDownAuth.PublicMark
        );

        OldMarkDowns.Add(oldVersion);
        return oldVersion;
    }

    /// <summary>
    /// 从历史版本还原
    /// </summary>
    /// <param name="oldMarkDown">要还原的历史版本</param>
    /// <returns>当前文档实例（支持链式调用）</returns>
    public Task<MarkDown> RestoreFromHistory(OldMarkDown oldMarkDown)
    {
        ArgumentNullException.ThrowIfNull(oldMarkDown);

        // 使用历史版本的内容更新当前文档
        return UpDataByMarkDownAsync(
            $"{MarkDownName}_v{oldMarkDown.OldMarkDownGuid}",
            oldMarkDown.OldMarkDownContent,
            oldMarkDown.OldMarkDownHash
        );
    }

    /// <summary>
    /// MarkDown 构建器（创建者类）
    /// </summary>
    public class MarkDownBuilder
    {
        private readonly string _markDownContent;
        private readonly string _markDownHash;
        private readonly string _markDownName;
        private readonly Guid _markUserGuid;
        private readonly HashSet<string> _tags = [];
        private MarkDownAuth _markDownAuth = MarkDownAuth.PublicMark;
        private MarkOption _markOption = MarkOption.Default;
        private Guid _markReviewGuid;

        public MarkDownBuilder(Guid markUserGuid, string markDownName, string markDownContent, string markDownHash)
        {
            _markUserGuid = markUserGuid;
            _markDownName = markDownName;
            _markDownContent = markDownContent;
            _markDownHash = markDownHash;
        }

        public MarkDownBuilder WithMarkReviewGuid(Guid markReviewGuid)
        {
            _markReviewGuid = markReviewGuid;
            return this;
        }

        public MarkDownBuilder WithTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
                _tags.Add(tag);
            return this;
        }

        public MarkDownBuilder WithTags(IEnumerable<string> tags)
        {
            foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
                _tags.Add(tag);
            return this;
        }

        public MarkDownBuilder WithMarkOption(MarkOption option)
        {
            _markOption = option;
            return this;
        }

        public MarkDownBuilder WithMarkDownAuth(MarkDownAuth auth)
        {
            _markDownAuth = auth;
            return this;
        }

        public MarkDown Build()
        {
            return new MarkDown(
                _markUserGuid,
                _markDownName,
                _markDownContent,
                _markDownHash,
                _markReviewGuid,
                _tags,
                _markDownAuth,
                _markOption);
        }
    }
}