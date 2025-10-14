// 修正命名错误
using DomainCommon;

namespace FileDev.Domain.DomainEntities;

public class FileGroup : Entity, IAggregateRoot
{
    public Guid RepositoryGuid { get; private set; }
    public Guid UserGuid { get; private set; }
    public string FileGroupName { get; private set; } = null!;
    public FileSafety FileSafety { get; private set; }
    public bool IsDeleted { get; private set; }
    public string MaterializedPath { get; private set; } = null!; // 初始化默认值
    public DateTime CreateTime { get; init; }
    public Guid? ParentId { get; private set; }
    public FileGroup? Parent { get; private set; }
    public List<string>? FileGroupTags { get; private set; } = new List<string>();
    // 修正拼写错误
    public List<FileGroup>? ChildFileGroups { get; private set; } = new List<FileGroup>(); 
    public List<NotFile>? Files { get; private set; } = new List<NotFile>();

    protected FileGroup() { }

    public FileGroup(Guid userGuid, Guid repositoryGuid, string fileGroupName, FileSafety fileSafety, DateTime createTime, List<string>? fileGroupTags = null
    )
    {
        Id=Guid.CreateVersion7();
        UserGuid = userGuid;
        RepositoryGuid = repositoryGuid;
        FileGroupName = fileGroupName;
        FileSafety = fileSafety;
        CreateTime = createTime;
        FileGroupTags = fileGroupTags ?? new List<string>();
    }

    public static FileGroup CreateRoot(Guid userId, Guid repositoryGuid) =>
        new FileGroup(userId, repositoryGuid, "根目录", FileSafety.FilePublic, DateTime.UtcNow) 
        { 
            ParentId = null, 
            MaterializedPath = "/" 
        };

    public void ReFileGroupName(string fileGroupName)
    {
        // 统一空值检查
        if (string.IsNullOrWhiteSpace(fileGroupName))
            return;

        var trimmedName = fileGroupName.Trim();
        if (string.Equals(trimmedName, FileGroupName, StringComparison.OrdinalIgnoreCase))
            return;

        if (HasSibling(trimmedName))
            return;

        FileGroupName = trimmedName;
    }

    public void AddWithFile(NotFile file)
    {
        // 简化条件判断
        if (file != null && !Files!.Contains(file))
            Files.Add(file);
    }

    public void ReMoveFile(NotFile file)
    {
        // 移除多余的 return
        if (file != null && Files!.Contains(file))
            Files.Remove(file);
    }

    private void RefreshPath()
    {
        if (ParentId is null)
        {
            MaterializedPath = $"/{Id}/";
            return;
        }

        Parent!.RefreshPath();
        // 修正路径拼接逻辑
        MaterializedPath = $"{Parent.MaterializedPath}{Id}/"; 
    }

    public void MoveTo(FileGroup? fileGroup)
    {
        if (fileGroup == this) return; // 防止自我移动

        if (fileGroup is null)
        {
            Parent?.ChildFileGroups?.Remove(this);
            Parent = null;
            ParentId = null;
            RefreshPath(); // 移动后刷新路径
            return;
        }

        if (fileGroup.ChildFileGroups?.Contains(this) == true) 
            return;

        if (HasSibling(fileGroup.FileGroupName))
            throw new ArgumentException("同层名称已存在");

        Parent?.ChildFileGroups?.Remove(this);
        Parent = fileGroup;
        ParentId = fileGroup.Id;
        fileGroup.ChildFileGroups?.Add(this);
        RefreshPath(); // 移动后刷新路径
    }

    bool HasSibling(string name) =>
        Parent?.ChildFileGroups?.Any(c => c.Id != Id && string.Equals(c.FileGroupName, name, StringComparison.OrdinalIgnoreCase)) == true;

    public void SoftDelete()
    {
        IsDeleted = true;
        RefreshPath();
        // 空值检查
        if (ChildFileGroups != null) 
        {
            foreach (var c in ChildFileGroups) 
                c.SoftDelete();
        }
    }
}
