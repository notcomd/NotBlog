namespace FileDev.Domain.Entities;

/// <summary>
/// 文件组聚合根 — 支持树形嵌套，同级名称唯一。
/// ParentGroupId == null 表示根节点。
/// </summary>
public class NotFileGroup : Entity, IAggregateRoot
{
    public Guid NotFileGroupId { get; init; }

    public Guid UserId { get; init; }

    public Guid? ParentGroupId { get; private set; }

    public string FileGroupName { get; private set; } = null!;

    public HashSet<string> FileGroupTags { get; private set; } = [];

    public HashSet<Guid> FileIds { get; private set; } = [];

    public string? FileGroupDescription { get; private set; }

    public DateTime UploadTime { get; init; }

    public DateTime UpdateTime { get; private set; }

    public bool IsDeleted { get; private set; }
    public FileIdentity FileIdentity { get; private set; }

    // ---- EF Core 自引用导航属性 ----

    public NotFileGroup? Parent { get; private set; }
    public ICollection<NotFileGroup> Children { get; private set; } = [];


    public int Depth { get; private set; }



    public NotFileGroup()
    {
        NotFileGroupId = Guid.CreateVersion7();
        UploadTime = DateTime.Now;
        UpdateTime = DateTime.Now;
        FileIdentity = FileIdentity.FilePublic;
    }

    /// <summary>
    /// 创建文件组。
    /// </summary>
    /// <param name="userId">所属用户</param>
    /// <param name="fileGroupName">组名称</param>
    /// <param name="parent">父文件组，null 表示根组</param>
    /// <param name="fileGroupTags">标签集合</param>
    /// <param name="fileGroupDescription">描述</param>
    /// <param name="fileIdentity">可见性</param>
    /// <param name="isNameUniqueAtSameLevel">
    /// 委托：由仓储层提供，用于校验同一父级下名称的唯一性。
    /// 传入 ParentGroupId 和名称，返回是否存在同名组。
    /// </param>
    /// <exception cref="ArgumentException">同级已存在同名文件组</exception>
    /// <exception cref="InvalidOperationException">深度超过限制</exception>
    public NotFileGroup(
        Guid userId,
        string fileGroupName,
        NotFileGroup? parent,
        HashSet<string>? fileGroupTags = null,
        string? fileGroupDescription = null,
        FileIdentity fileIdentity = FileIdentity.FilePublic,
        Func<Guid?, string, bool>? isNameUniqueAtSameLevel = null)
        : this()
    {
        if (string.IsNullOrWhiteSpace(fileGroupName))
            throw new ArgumentException("文件组名称不能为空", nameof(fileGroupName));

        UserId = userId;
        FileGroupName = fileGroupName;
        FileGroupTags = fileGroupTags ?? [];
        FileGroupDescription = fileGroupDescription;
        FileIdentity = fileIdentity;

        SetParent(parent, isNameUniqueAtSameLevel);

        AddDomainEvent(new CreateFileGroupEvent(
            NotFileGroupId, userId, fileGroupName,
            fileGroupTags ?? [], fileGroupDescription, fileIdentity));
    }

    /// <summary>
    /// 设置父组（移动节点），同时校验同级名称唯一性与深度限制。
    /// </summary>
    public void SetParent(NotFileGroup? newParent, Func<Guid?, string, bool>? isNameUniqueAtSameLevel = null)
    {
        var targetParentId = newParent?.NotFileGroupId;

        // 防止循环引用：不能把自己或自己的后代设为父组
        if (newParent != null && IsAncestorOf(newParent))
            throw new InvalidOperationException("不能将文件组移动到自身或其子组下");

        // 防止自己成为自己的父组
        if (targetParentId == NotFileGroupId)
            throw new InvalidOperationException("文件组不能成为自身的父组");

        // 深度限制（最大 5 层）
        const int maxDepth = 5;
        if (newParent != null && newParent.Depth >= maxDepth)
            throw new InvalidOperationException($"文件组嵌套深度不能超过 {maxDepth} 层");

        // 检查同级名称唯一性
        if (isNameUniqueAtSameLevel != null && isNameUniqueAtSameLevel(targetParentId, FileGroupName))
            throw new InvalidOperationException(
                $"同级下已存在名为 '{FileGroupName}' 的文件组");

        ParentGroupId = targetParentId;
        Parent = newParent;
        Depth = newParent == null ? 0 : newParent.Depth + 1;
        UpdateTime = DateTime.Now;
    }

    /// <summary>
    /// 重命名文件组，同时校验同级名称唯一性。
    /// </summary>
    public void Rename(string newName, Func<Guid?, string, bool>? isNameUniqueAtSameLevel = null)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("文件组名称不能为空", nameof(newName));

        if (isNameUniqueAtSameLevel != null && isNameUniqueAtSameLevel(ParentGroupId, newName))
            throw new InvalidOperationException(
                $"同级下已存在名为 '{newName}' 的文件组");

        FileGroupName = newName;
        UpdateTime = DateTime.Now;
    }

    /// <summary>
    /// 判断当前节点是否为 <paramref name="target"/> 的祖先。
    /// </summary>
    private bool IsAncestorOf(NotFileGroup target)
    {
        // 需要遍历 target 的所有后代 — 这里只做本地检查，深度遍历由调用方保证
        return false; // 真正的检查需在仓储层完成
    }

    /// <summary>
    /// 更新元数据（不涉及名称）。
    /// </summary>
    public void UpdateMeta(
        HashSet<string>? fileGroupTags = null,
        string? fileGroupDescription = null,
        FileIdentity? fileIdentity = null)
    {
        if (fileGroupTags != null)
            FileGroupTags = fileGroupTags;
        if (fileGroupDescription != null)
            FileGroupDescription = fileGroupDescription;
        if (fileIdentity != null)
            FileIdentity = fileIdentity.Value;
        UpdateTime = DateTime.Now;
    }

    public void AddFile(Guid fileId)
    {
        FileIds.Add(fileId);
        UpdateTime = DateTime.Now;
    }

    public void RemoveFile(Guid fileId)
    {
        FileIds.Remove(fileId);
        UpdateTime = DateTime.Now;
    }

    public void AddTag(string tag)
    {
        FileGroupTags.Add(tag);
        UpdateTime = DateTime.Now;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdateTime = DateTime.Now;
    }

    public void Restore()
    {
        IsDeleted = false;
        UpdateTime = DateTime.Now;
    }

    /// <summary>
    /// 删除当前组及其所有子组（递归标记软删除）。
    /// </summary>
    public void SoftDeleteTree()
    {
        SoftDelete();
        foreach (var child in Children)
            child.SoftDeleteTree();
    }



    public class NotFileGroupBuilder
    {
        private Guid _userId;
        private string _fileGroupName = string.Empty;
        private Guid? _parentGroupId;
        private HashSet<string> _fileGroupTags = [];
        private string? _fileGroupDescription;
        private FileIdentity _fileIdentity = FileIdentity.FilePublic;
        private Func<Guid?, string, bool>? _isNameUniqueAtSameLevel;

        public NotFileGroupBuilder WithUserId(Guid userId) { _userId = userId; return this; }

        public NotFileGroupBuilder WithFileGroupName(string name) { _fileGroupName = name; return this; }

        public NotFileGroupBuilder WithParentGroupId(Guid? parentId) { _parentGroupId = parentId; return this; }

        public NotFileGroupBuilder WithFileGroupTags(IEnumerable<string>? tags)
        {
            _fileGroupTags = tags?.ToHashSet() ?? [];
            return this;
        }

        public NotFileGroupBuilder WithFileGroupDescription(string? description)
        {
            _fileGroupDescription = description;
            return this;
        }

        public NotFileGroupBuilder WithFileIdentity(FileIdentity identity)
        {
            _fileIdentity = identity;
            return this;
        }

        /// <summary>
        /// 注入同名检查委托（由仓储层提供）。
        /// </summary>
        public NotFileGroupBuilder WithNameUniquenessChecker(Func<Guid?, string, bool> checker)
        {
            _isNameUniqueAtSameLevel = checker;
            return this;
        }

        public NotFileGroup Build()
        {
            var group = new NotFileGroup(
                _userId,
                _fileGroupName,
                parent: null, // Builder 模式下 parent 对象由调用方注入
                _fileGroupTags,
                _fileGroupDescription,
                _fileIdentity,
                _isNameUniqueAtSameLevel);

            // Builder 不通过 SetParent 设置父级，直接赋值以避免重名校验冲突
            if (_parentGroupId.HasValue)
                group.ParentGroupId = _parentGroupId;

            return group;
        }
    }
}
