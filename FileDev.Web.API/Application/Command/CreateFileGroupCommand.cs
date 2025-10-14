using NotMediator;
using FileDev.Domain.DomainEntities;

namespace FileDev.Web.API.Application.Command;

/// <summary>
/// 创建文件组的命令类
/// </summary>
public class CreateFileGroupCommand : IRequest<bool>
{
  
    /// <summary>
    /// 文件组所属用户的唯一标识
    /// </summary>
    public required Guid FileGroupBelongToUserGuid { get; set; }

    /// <summary>
    /// 文件组名称
    /// </summary>
    public required string FileGroupName { get; set; }

    public required FileSafety FileGroupSafety { get; set; }
    /// <summary>
    /// 文件组标签列表
    /// </summary>
    public List<string> FileGroupTags { get; set; } = new();

    /// <summary>
    /// 文件组所属文件仓库的唯一标识，可为空
    /// </summary>
    public Guid FileGroupBelongToFileRepositoryGuid { get; set; }

    /// <summary>
    /// 创建文件组的日期
    /// </summary>
    public DateTime CreateFileGroupDate { get; init; } = DateTime.UtcNow;

    public CreateFileGroupCommand(        
         Guid fileGroupBelongToUserGuid,
         Guid? fileGroupBelongToFileRepositoryGuid,
         string fileGroupName,
         FileSafety fileGroupSafety,
        List<string>? fileGroupTags = null
         )
    {      
        FileGroupBelongToUserGuid = fileGroupBelongToUserGuid;
        FileGroupName = fileGroupName;
        FileGroupSafety = fileGroupSafety;
        FileGroupTags = fileGroupTags ?? new List<string>();
        FileGroupBelongToFileRepositoryGuid = fileGroupBelongToFileRepositoryGuid ?? Guid.Empty;
    }

}
