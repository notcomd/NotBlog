using FileDev.Domain.Entities;
using FileDev.Domain.SeedWork;
namespace FileDev.Domain.IRepository;

public interface INotFileGroupRepository: IRepository<NotFileGroup>
{
    Task InsertNotFileGroupAsync(NotFileGroup notFileGroup);
    Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId);
    Task<IEnumerable<NotFileGroup>> GetAllNotFileGroupsAsync();
    Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId);
    Task<IEnumerable<NotFileGroup>> GetPublicNotFileGroupsAsync();
    Task<NotFileGroup?> GetNotFileGroupByNameAsync(string fileGroupName);
    Task<NotFileGroup?> UpdateNotFileGroupAsync(NotFileGroup notFileGroup);
    Task DeleteNotFileGroupAsync(Guid notFileGroupId);

    /// <summary>
    /// 检查指定父级下是否存在同名文件组（排除自身，用于更新场景）。
    /// </summary>
    /// <param name="parentGroupId">父组 ID，null 表示根级</param>
    /// <param name="name">文件组名称</param>
    /// <param name="excludeId">需要排除的自身 ID（可选，创建时传 null）</param>
    /// <returns>true = 同名已存在</returns>
    Task<bool> ExistsByNameAtSameLevelAsync(Guid? parentGroupId, string name, Guid? excludeId = null);

    /// <summary>
    /// 获取指定父组下的所有子组。
    /// </summary>
    Task<IEnumerable<NotFileGroup>> GetChildrenAsync(Guid parentGroupId);

    /// <summary>
    /// 获取用户的所有根级文件组（ParentGroupId == null）。
    /// </summary>
    Task<IEnumerable<NotFileGroup>> GetRootGroupsByUserIdAsync(Guid userId);
}