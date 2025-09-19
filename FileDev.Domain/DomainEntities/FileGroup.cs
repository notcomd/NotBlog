using DomainCommonst;

namespace FileDev.Domain.DomainEntities;

public class FileGroup : Entity
{

    public Guid RepositoryGuid { get; private set; }

    public Guid UserGuid { get; private set; }

    public string FileGroupName { get; private set; } = null!;

    public FileSafety FileSafety { get; private set; }

    public bool IsDeleted { get; private set; }

    public string MaterializedPath { get; private set; }

    public DateTime CreateTime { get; init; }

    public Guid? ParentId { get; private set; }

    public FileGroup? Parent { get; private set; }

    public List<string>? FileGroupTags { get; private set; } = new List<string>();

    public List<FileGroup>? ChildredFileGroup { get; private set; } = new List<FileGroup>();

    public List<NotFile>? Files { get; private set; } = new List<NotFile>();

    protected FileGroup() { }

    public FileGroup(Guid userGuid, string fileGroupName, FileSafety fileSafety, DateTime createTime)
    {
        UserGuid = userGuid;
        FileGroupName = fileGroupName;
        FileSafety = fileSafety;
        CreateTime = createTime;
    }

    public static FileGroup CreateRoot(Guid userId) =>
        new FileGroup(userId, "根目录", FileSafety.FilePublic, DateTime.UtcNow) { ParentId = null, MaterializedPath = "/" };

    public void ReFileGroupName(string fileGroupName)
    {
        if (string.Equals(fileGroupName, FileGroupName, StringComparison.OrdinalIgnoreCase))
        { return; }
        if (HasSibling(FileGroupName))
            return;
        FileGroupName = fileGroupName.Trim();
    }

    public void AddWithFile(NotFile file)
    {
        if (!Files!.Contains(file) || Files.Count == 0)
            Files.Add(file);
        return;
    }

    public void ReMoveFile(NotFile file)
    {
        if (Files!.Contains(file))
            Files.Remove(file);
        return;
    }

    private void RefreshPath()
    {
        if (ParentId is null)
        {
            MaterializedPath = $"/{Id}/";
            return;
        }
        Parent!.RefreshPath();
        MaterializedPath = $"{Parent?.MaterializedPath}";
    }

    public void MoveTo(FileGroup? fileGroup)
    {
        if (fileGroup is null)
        {
            Parent?.ChildredFileGroup.Remove(this);
            Parent = null;
            ParentId = null;
            return;
        }
        if (fileGroup?.ChildredFileGroup.Contains(this) == true) return;
        if (HasSibling(fileGroup.FileGroupName))
            throw new ArgumentException("同层名称已存在");
        Parent?.ChildredFileGroup.Remove(this);
        Parent = fileGroup;
        ParentId = fileGroup?.Id;
        fileGroup?.ChildredFileGroup.Add(this);
    }

    bool HasSibling(string name) =>
    Parent?.ChildredFileGroup.Any(c => c.Id != Id && c.FileGroupName == name) == true;

    public void SoftDelete()
    {
        IsDeleted = true;
        RefreshPath();
        foreach (var c in ChildredFileGroup) c.SoftDelete();
    }

}
