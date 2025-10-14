using DomainCommon;

namespace FileDev.Domain.DomainEntities;

public sealed class NotFileRepository : Entity, IAggregateRoot
{
    public Guid UserGuid { get; private set; }

    public string NotFileRepositoryName { get; private set; } = null!;

    public List<Guid>? FileGroups { get; private set; } = new List<Guid>();

    public List<NotFile>?  NotFiles { get; private set; }= new List<NotFile>();

    public FileSafety FileSafety { get; private set; }

    public DateTime LastModified { get; private set; }

    public long FileCount { get; private set; } = 0;

    public string? RepositoryBrief { get; private set; }

    public Uri? RepositoryCover { get; private set; }


    protected NotFileRepository() { }

    public NotFileRepository(Guid userGuid, string notFileRepositoryName, FileSafety fileSafety, 
        DateTime lastModified, long fileCount, string? repositoryBrief,
        List<Guid>? fileGroups,List<NotFile>? notFiles, Uri? repositoryCover)
    {
        Id = Guid.CreateVersion7();
        UserGuid = userGuid;
        NotFileRepositoryName = notFileRepositoryName;
        FileSafety = fileSafety;
        LastModified = lastModified;
        FileGroups = fileGroups;
        FileCount = fileCount;
        RepositoryBrief = repositoryBrief;
        NotFiles = notFiles;
        RepositoryCover = repositoryCover;
    }

    /// <summary>
    /// 添加文件组
    /// </summary>
    /// <param name="fileGroupId">文件组ID</param>
    public void AddFileGroup(Guid fileGroupId)
    {
        FileGroups ??= new List<Guid>();
        if (!FileGroups.Contains(fileGroupId))
        {
            FileGroups.Add(fileGroupId);
        }
    }

    /// <summary>
    /// 移除文件组
    /// </summary>
    /// <param name="fileGroupId">文件组ID</param>
    public void RemoveFileGroup(Guid fileGroupId)
    {
        FileGroups?.Remove(fileGroupId);
    }

    /// <summary>
    /// 添加非文件项
    /// </summary>
    /// <param name="notFile">非文件项</param>
    public void AddNotFile(NotFile notFile)
    {
        NotFiles ??= new List<NotFile>();
        if (!NotFiles.Contains(notFile))
        {
            NotFiles.Add(notFile);
            FileCount++;
            LastModified = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 移除非文件项
    /// </summary>
    /// <param name="notFile">非文件项</param>
    public void RemoveNotFile(NotFile notFile)
    {
        if (NotFiles?.Remove(notFile) == true)
        {
            FileCount--;
            LastModified = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 更新仓库名称
    /// </summary>
    /// <param name="newName">新的仓库名称</param>
    public void UpdateRepositoryName(string newName)
    {
        if (!string.IsNullOrEmpty(newName))
        {
            NotFileRepositoryName = newName;
            LastModified = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 更新仓库简介
    /// </summary>
    /// <param name="newBrief">新的仓库简介</param>
    public void UpdateRepositoryBrief(string? newBrief)
    {
        RepositoryBrief = newBrief;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新仓库封面
    /// </summary>
    /// <param name="newCover">新的仓库封面URI</param>
    public void UpdateRepositoryCover(Uri? newCover)
    {
        RepositoryCover = newCover;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新文件安全设置
    /// </summary>
    /// <param name="newFileSafety">新的文件安全设置</param>
    public void UpdateFileSafety(FileSafety newFileSafety)
    {
        FileSafety = newFileSafety;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取所有文件组ID
    /// </summary>
    /// <returns>文件组ID列表</returns>
    public List<Guid>? GetFileGroups()
    {
        return FileGroups;
    }

    /// <summary>
    /// 获取所有非文件项
    /// </summary>
    /// <returns>非文件项列表</returns>
    public List<NotFile>? GetNotFiles()
    {
        return NotFiles;
    }
}
