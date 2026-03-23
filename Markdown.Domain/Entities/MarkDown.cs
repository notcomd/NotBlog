using Markdown.Domain.SeedWork;

namespace Markdown.Domain.Entities;

/// <summary>
///     文档
/// </summary>
public class MarkDown : Entity, IAggregateRoot
{
    private MarkDown()
    {
        MarkDownGuid = Guid.CreateVersion7();
        MarkDownTagboard = new HashSet<string>();
        MarkReviews = new List<MarkReview>();
        MarkDowns = new List<MarkDown>();
        CreateAt = DateTime.UtcNow;
        UplaodAt = DateTime.UtcNow;
    }

    // 私有全参数构造函数，供 Builder 调用
    private MarkDown(Guid markUserGuid, string markDownName, string markDownContent, string markDownHash,
        Guid markReviewGuid, HashSet<string> markDownTagboard, MarkOption markOption) : this()
    {
        MarkUserGuid = markUserGuid;
        MarkDownName = markDownName;
        MarkDownContent = markDownContent;
        MarkDownHash = markDownHash;
        MarkReviewGuid = markReviewGuid;
        MarkDownTagboard = markDownTagboard;
        MarkOption = markOption;
        IsDelete = false;
    }

    // 公有简化构造函数，使用默认值调用私有构造函数
    public MarkDown(Guid markUserGuid, string markDownName, string markDownContent, string markDownHash)
        : this(markUserGuid, markDownName, markDownContent, markDownHash, Guid.Empty, new HashSet<string>(),
            MarkOption.Default)
    {
    }

    public Guid MarkDownGuid { get; init; }

    public Guid MarkHistoryGuid { get; private set; }

    public Guid MarkReviewGuid { get; init; }

    public Guid MarkUserGuid { get; init; }

    public string MarkDownName { get; private set; } = null!;

    public HashSet<string> MarkDownTagboard { get; private set; }

    public MarkOption MarkOption { get; private set; } = MarkOption.Default;

    public string MarkDownHash { get; private set; } = null!;

    public DateTime CreateAt { get; init; }

    public string MarkDownContent { get; private set; } = null!;

    public bool IsDelete { get; private set; }

    public ICollection<MarkDown> MarkDowns { get; private set; }

    public DateTime UplaodAt { get; private set; }

    public DateTime UpdateAt { get; private set; }

    public ICollection<MarkReview> MarkReviews { get; private set; }

    public ICollection<OldMarkDown> OldMarkDowns { get; private set; }

    public Task<MarkDown> AddByMarkReviewAsync(MarkReview markReview)
    {
        MarkReviews.Add(markReview);
        return Task.FromResult(this);
    }

    public Task<MarkDown> UpDataByMarkDownAsync(string markDownName, string markDownContent, string markDownHash)
    {
        MarkDownName = markDownName;
        MarkDownContent = markDownContent;
        MarkDownHash = markDownHash;
        UplaodAt = DateTime.Now;
        return Task.FromResult(this);
    }

    public bool IsMarkDownEques(string markMd5) => MarkDownHash == markMd5;

    public void SoftDelete()
    {
        IsDelete = true;
    }

    /// <summary>
    ///     MarkDown 构建器（创建者类）
    /// </summary>
    public class Builder
    {
        private readonly string _markDownContent;
        private readonly string _markDownHash;
        private readonly string _markDownName;
        private readonly Guid _markUserGuid;
        private readonly HashSet<string> _tags = new();
        private MarkOption _markOption = MarkOption.Default;
        private Guid _markReviewGuid;

        public Builder(Guid markUserGuid, string markDownName, string markDownContent, string markDownHash)
        {
            _markUserGuid = markUserGuid;
            _markDownName = markDownName;
            _markDownContent = markDownContent;
            _markDownHash = markDownHash;
        }

        public Builder WithMarkReviewGuid(Guid markReviewGuid)
        {
            _markReviewGuid = markReviewGuid;
            return this;
        }

        public Builder WithTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
                _tags.Add(tag);
            return this;
        }

        public Builder WithTags(IEnumerable<string> tags)
        {
            foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
                _tags.Add(tag);
            return this;
        }

        public Builder WithMarkOption(MarkOption option)
        {
            _markOption = option;
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
                _markOption);
        }
    }
}