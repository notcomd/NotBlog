namespace Markdown.Domain.Entities;

/// <summary>
///     文档（仅保存文件元数据，正文内容以文件形式存储于文件存储后端）
/// </summary>
public class MarkDown : Entity<int>, IAggregateRoot
{
    private MarkDown()
    {
        MarkDownGuid = Guid.CreateVersion7();
        MarkDownTagboard = [];
        MarkReviews = [];
        OldMarkDowns = [];
        MarkQuote = new MarkQuote();
        CreateAt = DateTimeOffset.UtcNow;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    // 私有全参数构造函数，供 Builder 调用
    private MarkDown(Guid markUserGuid, string markDownName, string fileId, string fileUri,
        long fileSize, string fileExt, string markDownHash,
        Guid markReviewGuid, HashSet<string> markDownTagboard, MarkDownAuth markDownAuth) : this()
    {
        MarkUserGuid = markUserGuid;
        MarkDownName = markDownName;
        FileId = fileId;
        FileUri = fileUri;
        FileSize = fileSize;
        FileExt = fileExt;
        MarkDownHash = markDownHash;
        MarkReviewGuid = markReviewGuid;
        MarkDownTagboard = markDownTagboard;
        MarkDownAuth = markDownAuth;
        IsDelete = false;
    }

    /// <summary>
    ///     公有简化构造函数，使用默认值调用私有构造函数
    /// </summary>
    public MarkDown(Guid markUserGuid, string markDownName, string fileId, string fileUri,
        long fileSize, string fileExt, string markDownHash)
        : this(markUserGuid, markDownName, fileId, fileUri, fileSize, fileExt, markDownHash,
            Guid.Empty, [], MarkDownAuth.PublicMark)
    {
    }

    public Guid MarkDownGuid { get; init; }

    public Guid MarkReviewGuid { get; init; }

    public Guid MarkUserGuid { get; init; }

    public string MarkDownName { get; private set; } = null!;

    public HashSet<string> MarkDownTagboard { get; private set; }

    public MarkDownAuth MarkDownAuth { get; private set; } = MarkDownAuth.PublicMark;

    /// <summary>
    ///     当前正文文件 SHA-256 哈希
    /// </summary>
    public string MarkDownHash { get; private set; } = null!;

    /// <summary>
    ///     正文文件标识（文件存储后端 ID，如 FileDev file_id）
    /// </summary>
    public string FileId { get; private set; } = null!;

    /// <summary>
    ///     正文文件访问 URI（内部定位用，不对外暴露）
    /// </summary>
    public string FileUri { get; private set; } = null!;

    /// <summary>
    ///     正文文件字节数
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    ///     正文文件扩展名（如 .md / .markdown）
    /// </summary>
    public string FileExt { get; private set; } = null!;

    public bool IsDelete { get; private set; }

    /// <summary>
    ///     审核状态（草稿/待审核/通过/驳回），默认草稿
    /// </summary>
    public MarkStatus Status { get; private set; } = MarkStatus.MarkDraft;

    public DateTimeOffset CreateAt { get; init; }

    public DateTimeOffset UpdateAt { get; private set; }

    /// <summary>
    ///     文档交互统计（浏览/点赞/收藏/分享/硬币/热度，值对象）
    /// </summary>
    public MarkQuote MarkQuote { get; private set; }

    public ICollection<MarkReview> MarkReviews { get; private set; }

    public ICollection<OldMarkDown> OldMarkDowns { get; private set; }

    // ==================== 文档交互计数（委托 MarkQuote） ====================

    /// <summary>
    ///     文档点赞 +1（配合 MarkDocumentLike 唯一约束防重）
    /// </summary>
    public long AddLove(long count = 1) => MarkQuote.AddLove(count);

    /// <summary>
    ///     文档取消点赞 -1（下限钳制 0）
    /// </summary>
    public long RemoveLove(long count = 1) => MarkQuote.RemoveLove(count);

    /// <summary>
    ///     文档收藏 +1（配合 MarkFavorite 唯一约束防重，收藏命令事务内调用）
    /// </summary>
    public long AddFavorite(long count = 1) => MarkQuote.AddFavorite(count);

    /// <summary>
    ///     文档取消收藏 -1（下限钳制 0）
    /// </summary>
    public long RemoveFavorite(long count = 1) => MarkQuote.RemoveFavorite(count);

    /// <summary>
    ///     文档分享 +1
    /// </summary>
    public long AddShare(long count = 1) => MarkQuote.AddShare(count);

    /// <summary>
    ///     文档打赏硬币 +1（配合 MarkCoin 记录）
    /// </summary>
    public long AddCoin(long count = 1) => MarkQuote.AddCoin(count);

    /// <summary>
    ///     文档浏览 +1（配合 Redis Set 防刷）
    /// </summary>
    public long AddView(long count = 1) => MarkQuote.AddView(count);

    /// <summary>
    ///     重算热点分并写回 MarkQuote.HeatScore（公式见 MarkdownHeatFormula），返回新热度分。
    ///     由写侧钩子（交互端点）与定时重建任务调用
    /// </summary>
    public double RecalculateHotScore(DateTimeOffset now)
    {
        var score = MarkdownHeatFormula.Calculate(MarkQuote, CreateAt, now);
        MarkQuote.SetHeatScore(score);
        return score;
    }

    // ==================== 评论聚合操作 ====================

    /// <summary>
    ///     添加评论到文档（聚合根统一入口）
    /// </summary>
    public Task<MarkDown> AddByMarkReviewAsync(MarkReview markReview)
    {
        ArgumentNullException.ThrowIfNull(markReview);
        MarkReviews.Add(markReview);
        return Task.FromResult(this);
    }

    /// <summary>
    ///     软删除评论及其所有后代评论（递归，聚合根统一入口）。
    ///     仅标记 IsDelete 保留记录与评论树结构（可审计、可追溯），
    ///     已删除评论在所有对外查询中不可见；注意：删除整棵子树而非仅单条评论
    /// </summary>
    public void SoftDeleteReview(Guid reviewGuid)
    {
        var review = FindReview(reviewGuid);
        if (review is null)
            throw new InvalidOperationException($"评论 {reviewGuid} 不存在于当前文档聚合中");

        // 广度优先收集所有后代评论（子、孙…），确保整棵评论子树一并软删除
        var descendants = new List<MarkReview>();
        var queue = new Queue<MarkReview>();
        queue.Enqueue(review);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in MarkReviews.Where(r => r.MarkAggregateRootGuid == current.MarkReviewGuid))
            {
                descendants.Add(child);
                queue.Enqueue(child);
            }
        }

        review.SoftDelete();
        foreach (var descendant in descendants)
        {
            descendant.SoftDelete();
        }

        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     添加子评论到父评论（聚合根统一入口，维护聚合内一致性）
    /// </summary>
    public MarkReview AddChildReview(Guid parentReviewGuid, MarkReview childReview)
    {
        ArgumentNullException.ThrowIfNull(childReview);

        var parentReview = FindReview(parentReviewGuid)
            ?? throw new InvalidOperationException($"父评论 {parentReviewGuid} 不存在于当前文档聚合中");

        childReview.SetParentReviewGuid(parentReviewGuid);
        parentReview.MarkReviews.Add(childReview);
        parentReview.ReviewQuote.AddReply();
        MarkReviews.Add(childReview);

        UpdateAt = DateTimeOffset.UtcNow;
        return childReview;
    }

    /// <summary>
    ///     在聚合内查找指定评论
    /// </summary>
    public MarkReview? FindReview(Guid reviewGuid)
    {
        return MarkReviews.FirstOrDefault(r => r.MarkReviewGuid == reviewGuid);
    }

    /// <summary>
    ///     更新文档（文件化：元数据 + 文件引用变更；正文文件由应用层先行保存）
    /// </summary>
    public Task<MarkDown> UpdateByMarkDownAsync(string markDownName, string fileId, string fileUri,
        long fileSize, string fileExt, string markDownHash)
    {
        if (string.IsNullOrWhiteSpace(markDownName))
            throw new ArgumentNullException(nameof(markDownName));
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentNullException(nameof(fileId));
        if (string.IsNullOrWhiteSpace(fileUri))
            throw new ArgumentNullException(nameof(fileUri));
        if (string.IsNullOrWhiteSpace(fileExt))
            throw new ArgumentNullException(nameof(fileExt));
        if (string.IsNullOrWhiteSpace(markDownHash))
            throw new ArgumentNullException(nameof(markDownHash));

        MarkDownName = markDownName;
        FileId = fileId;
        FileUri = fileUri;
        FileSize = fileSize;
        FileExt = fileExt;
        MarkDownHash = markDownHash;
        UpdateAt = DateTimeOffset.UtcNow;

        return Task.FromResult(this);
    }

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
    public bool HasTag(string tag)
    {
        return MarkDownTagboard.Contains(tag);
    }

    /// <summary>
    /// 验证用户是否有权限操作此文档
    /// </summary>
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
    ///     创建历史版本快照并纳入聚合管理（用于更新前保存旧版本）。
    ///     快照自动加入 OldMarkDowns 集合，由 EF Core 级联持久化；
    ///     正文内容不再直接获取自实体属性（文档只存元数据），
    ///     由应用层先从文件存储读取旧内容后作为参数传入；
    ///     内容去重：当前内容已存在于历史版本时不再重复快照，
    ///     避免反复更新/还原同一内容导致历史版本无限膨胀（P1-8）
    /// </summary>
    /// <param name="content">旧版本文档内容（应用层从文件流读取）</param>
    /// <returns>新创建的 OldMarkDown 实例；内容已存在历史记录时返回 null</returns>
    public OldMarkDown? CreateHistorySnapshot(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // 快照权限应与当前文档一致，避免私有文档的历史版本被标记为公开
        if (OldMarkDowns.Any(o => !o.IsDelete && o.OldMarkDownHash == MarkDownHash))
            return null;

        var oldVersion = new OldMarkDown(
            MarkDownGuid,
            MarkUserGuid,
            content,
            MarkDownHash,
            MarkDownAuth
        );

        OldMarkDowns.Add(oldVersion);
        return oldVersion;
    }

    /// <summary>
    ///     从历史版本还原（文件化：历史版本内容由应用层重新保存为文件后更新元数据引用）
    /// </summary>
    /// <param name="oldMarkDown">要还原的历史版本</param>
    /// <param name="fileId">还原内容的新文件标识</param>
    /// <param name="fileUri">还原内容的新文件 URI</param>
    /// <param name="fileSize">还原内容的新文件字节数</param>
    /// <param name="fileExt">还原内容的新文件扩展名</param>
    /// <returns>当前文档实例（支持链式调用）</returns>
    public Task<MarkDown> RestoreFromHistory(OldMarkDown oldMarkDown, string fileId, string fileUri,
        long fileSize, string fileExt)
    {
        ArgumentNullException.ThrowIfNull(oldMarkDown);

        // 使用历史版本的内容更新当前文档（名称不变），文件由应用层已保存
        return UpdateByMarkDownAsync(
            MarkDownName,
            fileId,
            fileUri,
            fileSize,
            fileExt,
            oldMarkDown.OldMarkDownHash
        );
    }

    /// <summary>
    /// MarkDown 构建器（创建者类）
    /// </summary>
    public class MarkDownBuilder
    {
        private readonly string _fileId;
        private readonly string _fileUri;
        private readonly long _fileSize;
        private readonly string _fileExt;
        private readonly string _markDownHash;
        private readonly string _markDownName;
        private readonly Guid _markUserGuid;
        private readonly HashSet<string> _tags = [];
        private MarkDownAuth _markDownAuth = MarkDownAuth.PublicMark;
        private Guid _markReviewGuid;

        public MarkDownBuilder(Guid markUserGuid, string markDownName, string fileId, string fileUri,
            long fileSize, string fileExt, string markDownHash)
        {
            _markUserGuid = markUserGuid;
            _markDownName = markDownName;
            _fileId = fileId;
            _fileUri = fileUri;
            _fileSize = fileSize;
            _fileExt = fileExt;
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
                _fileId,
                _fileUri,
                _fileSize,
                _fileExt,
                _markDownHash,
                _markReviewGuid,
                _tags,
                _markDownAuth);
        }
    }
}
