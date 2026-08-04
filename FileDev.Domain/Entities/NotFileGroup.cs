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

    public DateTimeOffset UploadTime { get; init; }

    public DateTimeOffset UpdateTime { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeleteTime { get; private set; }

    public FileIdentity FileIdentity { get; private set; }



    public NotFileGroup? Parent { get; private set; }
    public ICollection<NotFileGroup> Children { get; private set; } = [];


    public int Depth { get; private set; }



    private NotFileGroup()
    {
        NotFileGroupId = Guid.CreateVersion7();
        UploadTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
        FileIdentity = FileIdentity.FilePrivate;
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
        FileIdentity fileIdentity = FileIdentity.FilePrivate,
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
        UpdateTime = DateTimeOffset.UtcNow;
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
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 判断当前节点是否为 <paramref name="target"/> 的祖先。
    /// 从目标节点沿父链（ParentGroupId）向上遍历，若能在到达根节点前遇到当前节点，
    /// 则说明当前节点是目标节点的祖先（使用访问集合防止环形引用导致死循环）。
    /// </summary>
    private bool IsAncestorOf(NotFileGroup target)
    {
        var visited = new HashSet<NotFileGroup>(ReferenceEqualityComparer.Instance);
        var current = target;
        while (current != null && visited.Add(current))
        {
            if (ReferenceEquals(current, this))
                return true;
            current = current.Parent;
        }
        return false;
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
        UpdateTime = DateTimeOffset.UtcNow;
    }

    public void AddFile(Guid fileId)
    {
        if (fileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空", nameof(fileId));
        FileIds.Add(fileId);
        UpdateTime = DateTimeOffset.UtcNow;
    }

    public void RemoveFile(Guid fileId)
    {
        if (fileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空", nameof(fileId));
        FileIds.Remove(fileId);
        UpdateTime = DateTimeOffset.UtcNow;
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("标签不能为 null 或空白", nameof(tag));

        FileGroupTags.Add(tag);
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 软删除当前文件组，标记删除时间并触发领域事件。
    /// </summary>
    /// <exception cref="InvalidOperationException">文件组已被删除</exception>
    public void SoftDelete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("文件组已经被删除");

        IsDeleted = true;
        DeleteTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new DeleteFileGroupEvent(NotFileGroupId, UserId));
    }

    /// <summary>
    /// 恢复已软删除的文件组。
    /// </summary>
    /// <exception cref="InvalidOperationException">文件组未被删除</exception>
    public void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("文件组没有被删除");
        IsDeleted = false;
        DeleteTime = null;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 删除当前组及其所有子组（递归标记软删除），并为每个子组触发领域事件。
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
        private NotFileGroup? _parent;
        private HashSet<string> _fileGroupTags = [];
        private string? _fileGroupDescription;
        private FileIdentity _fileIdentity = FileIdentity.FilePrivate;
        private Func<Guid?, string, bool>? _isNameUniqueAtSameLevel;

        public NotFileGroupBuilder WithUserId(Guid userId) { _userId = userId; return this; }

        public NotFileGroupBuilder WithFileGroupName(string name) { _fileGroupName = name; return this; }

        public NotFileGroupBuilder WithParentGroupId(Guid? parentId) { _parentGroupId = parentId; return this; }

        /// <summary>
        /// 注入父文件组实体，构建时通过 SetParent 走完整业务校验。
        /// </summary>
        public NotFileGroupBuilder WithParent(NotFileGroup? parent) { _parent = parent; return this; }

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
            // 验证必需字段
            if (_userId == Guid.Empty)
                throw new InvalidOperationException("UserId 必须提供且不能为空。");
            if (string.IsNullOrWhiteSpace(_fileGroupName))
                throw new InvalidOperationException("FileGroupName 必须提供且不能为空。");

            var group = new NotFileGroup(
                _userId,
                _fileGroupName,
                _parent, // 注入父级实体时走构造函数内 SetParent 的完整业务校验
                _fileGroupTags,
                _fileGroupDescription,
                _fileIdentity,
                _isNameUniqueAtSameLevel);

            // 兼容仅传入父级 ID（未注入父级实体）的场景：
            // 显式执行与 SetParent 相同的自引用校验，保证两条路径校验一致
            if (_parent is null && _parentGroupId.HasValue)
            {
                if (_parentGroupId == group.NotFileGroupId)
                    throw new InvalidOperationException("文件组不能成为自身的父组");
                group.ParentGroupId = _parentGroupId;
            }

            return group;
        }
    }
}
