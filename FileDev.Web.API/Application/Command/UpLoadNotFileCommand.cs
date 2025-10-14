using FileDev.Domain.DomainEntities;
using NotMediator;

namespace FileDev.Web.API.Application.Command;

/// <summary>
/// 上传文件命令
/// </summary>
public class UpLoadNotFileCommand : IRequest<bool>
{
    // 使用 init 访问器替代 private set，使属性只能在对象初始化时设置，增强不可变性
    public Guid? FileGroupGuid { get; init; }
    public Guid? FileRepositoryGuid { get; init; }
    public Guid UserGuid { get; init; }
    public string FileName { get; init; } = null!;
    public string FileDescription { get; init; } = null!;
    public string FileType { get; init; } = null!;
    public double FileSize { get; init; }
    public FileSafety FileSafety { get; init; }
    public string FileHash { get; init; }
    public ObjectMap ObjectMap { get; init; }

    // 添加构造函数，确保必要属性在创建对象时初始化
    public UpLoadNotFileCommand(
        Guid userGuid,
        string fileName,
        string fileDescription,
        string fileType,
        double fileSize,
        FileSafety fileSafety,
        string fileHash,
        ObjectMap objectMap,
        Guid? fileGroupGuid = null,
        Guid? fileRepositoryGuid = null)
    {
        UserGuid = userGuid;
        FileName = fileName;
        FileDescription = fileDescription;
        FileType = fileType;
        FileSize = fileSize;
        FileSafety = fileSafety;
        FileHash = fileHash;
        ObjectMap = objectMap;
        FileGroupGuid = fileGroupGuid;
        FileRepositoryGuid = fileRepositoryGuid;
    }
}
