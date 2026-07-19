using FileDev.Domain.Entities;

namespace FileDev.Domain.IServices;

public interface INotFileGroupService
{
    /// <summary>
    /// 获取用户的所有文件组。
    /// </summary>
    Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId);
   
    /// <summary>
    /// 获取指定文件组的详细信息。
    /// </summary>
    Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId);

    /// <summary>
    /// 获取用户的所有根级文件组（ParentGroupId == null）。
    /// </summary>
    Task<IEnumerable<NotFileGroup>> GetRootGroupsByUserIdAsync(Guid userId);

    /// <summary>
    /// 获取指定父组下的直接子组。
    /// </summary>
    Task<IEnumerable<NotFileGroup>> GetChildrenAsync(Guid parentGroupId);
}