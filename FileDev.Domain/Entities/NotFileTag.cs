namespace FileDev.Domain.Entities;

using FileDev.Domain.Enum;

/// <summary>
/// 标签聚合根 — 用于替代原"文件组"概念，实现类文件组管理。
/// <para>
/// 一个标签即一个可多对多的文件集合：标签聚合持有 <see cref="FileIds"/>，文件可挂多个标签。
/// 相比原文件组，本实体去掉了树形结构与深度限制，只有扁平的一层标签（唯一约束：用户内标签名唯一）。
/// 新用户注册时预置 4 个默认标签（图片/文档/文件/视频），上传文件按类型自动归类到对应默认标签。
/// 默认标签的稳定语义由 <see cref="DefaultKind"/> 承载：允许改名，改名后自动归类依然生效。
/// </para>
/// </summary>
public class NotFileTag : Entity<Guid>, IAggregateRoot
{
    /// <summary>标签主键（业务标识，非 EF 的 Id）</summary>
    public Guid TagId { get; init; }

    /// <summary>所属用户</summary>
    public Guid UserId { get; init; }

    /// <summary>标签名称（同一用户内唯一）</summary>
    public string TagName { get; private set; } = null!;

    /// <summary>标签描述</summary>
    public string? TagDescription { get; private set; }

    /// <summary>该标签下的文件 ID 集合</summary>
    public List<Guid> FileIds { get; private set; } = [];

    /// <summary>默认标签种类（图片/文档/文件/视频），<see cref="TagDefaultKind.None"/> 表示用户自定义标签。</summary>
    public TagDefaultKind DefaultKind { get; private set; } = TagDefaultKind.None;

    public DateTimeOffset UploadTime { get; init; }

    public DateTimeOffset UpdateTime { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeleteTime { get; private set; }

    private NotFileTag()
    {
        TagId = Guid.CreateVersion7();
        UploadTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建标签。</summary>
    public NotFileTag(Guid userId, string tagName, string? tagDescription = null,
        TagDefaultKind defaultKind = TagDefaultKind.None)
        : this()
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("标签名称不能为空", nameof(tagName));

        UserId = userId;
        TagName = tagName.Trim();
        TagDescription = tagDescription;
        DefaultKind = defaultKind;
    }

    /// <summary>重命名标签。</summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("标签名称不能为空", nameof(newName));
        TagName = newName.Trim();
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>更新描述（为空时不修改，保留原值）。</summary>
    public void UpdateDescription(string? tagDescription)
    {
        if (tagDescription is not null)
            TagDescription = tagDescription;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>向标签内追加一个文件（自动去重）。</summary>
    public void AddFile(Guid fileId)
    {
        if (fileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空", nameof(fileId));
        if (!FileIds.Contains(fileId))
            FileIds.Add(fileId);
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>从标签内移除一个文件。</summary>
    public void RemoveFile(Guid fileId)
    {
        if (FileIds.Remove(fileId))
            UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>软删除标签。</summary>
    public void SoftDelete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("标签已经被删除");
        IsDeleted = true;
        DeleteTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>恢复已软删除的标签。</summary>
    public void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("标签没有被删除");
        IsDeleted = false;
        DeleteTime = null;
        UpdateTime = DateTimeOffset.UtcNow;
    }
}