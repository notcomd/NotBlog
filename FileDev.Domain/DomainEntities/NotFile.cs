// 修正命名空间引用拼写错误
using DomainCommon;

namespace FileDev.Domain.DomainEntities;

public class NotFile : Entity
{
    
    public Guid? FileGroupGuid { get; private set; }
    public Guid? FileRepositoryGuid { get; private set; }
    public Guid UserGuid { get; private set; }
    public string FileName { get; private set; } = null!;
    public string FileDescription { get; private set; } = null!;
    public string FileType { get; private set; } = null!;
    public double FileSize { get; private set; }
    public FileSafety FileSafety { get; private set; }
    public string FileHash { get; private set; }
    public ObjectMap ObjectMap { get; private set; } = null!;

    private NotFile() { }

    // 添加 ObjectMap 参数并初始化
    public NotFile(Guid? fileGroupGuid, Guid? fileRepositoryGuid, Guid userGuid, string fileName, 
                  string fileDescription, string fileType, double fileSize, 
                  FileSafety fileSafety, string fileHash, ObjectMap objectMap)
    {
        FileGroupGuid = fileGroupGuid;
        FileRepositoryGuid = fileRepositoryGuid;
        UserGuid = userGuid;
        FileName = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentNullException(nameof(fileName)) : fileName.Trim();
        FileDescription = string.IsNullOrWhiteSpace(fileDescription) ? throw new ArgumentNullException(nameof(fileDescription)) : fileDescription.Trim();
        FileType = string.IsNullOrWhiteSpace(fileType) ? throw new ArgumentNullException(nameof(fileType)) : fileType.Trim();
        FileSize = fileSize >= 0 ? fileSize : throw new ArgumentOutOfRangeException(nameof(fileSize), "文件大小不能为负数");
        FileSafety = fileSafety;
        FileHash = string.IsNullOrWhiteSpace(fileHash) ? throw new ArgumentNullException(nameof(fileHash)) : fileHash.Trim();
        ObjectMap = objectMap ?? throw new ArgumentNullException(nameof(objectMap));
    }

    public void ReFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        var newFileName = fileName.Trim();
        if (string.Equals(newFileName, FileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        FileName = newFileName;
    }

    // 修正参数名大小写问题，修正逻辑错误
    public void MoveTo(Guid? groupGuid, Guid? repositoryGuid)
    {
        if (groupGuid != null && !Guid.Equals(this.FileGroupGuid, groupGuid))
        {
            this.FileGroupGuid = groupGuid;
        }
        if (repositoryGuid != null && !Guid.Equals(this.FileRepositoryGuid, repositoryGuid))
        {
            this.FileRepositoryGuid = repositoryGuid;
        }
    }

    // 修正拼写错误
    public void ReFreshPath(string freshPath, string rightPath)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(freshPath, nameof(freshPath));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(rightPath, nameof(rightPath));
        
        if (ObjectMap is null)
        {
            throw new InvalidOperationException("ObjectMap 未初始化");
        }
        
        this.ObjectMap.PhysicalName = rightPath + FileName;
        this.ObjectMap.ObjectKey = freshPath + FileName;
    }
}