using FileDev.Domain.Entities;
namespace FileDev.Domain.IRepository;

public interface INotFileGroupRepository: IRepository<NotFileGroup, IUnitOfWork>
{
    
    /// <summary>
    /// 插入文件组。
    /// </summary>
    /// <param name="notFileGroup">要插入的文件组</param>
    Task InsertNotFileGroupAsync(NotFileGroup notFileGroup);

    /// <summary>
    /// 根据 ID 获取文件组。
    /// </summary>
    /// <param name="notFileGroupId">文件组 ID</param>
    /// <returns>文件组实例</returns>
    Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId);

    /// <summary>
    /// 获取所有文件组。
    /// </summary>
    /// <returns>所有文件组实例</returns>
    Task<IEnumerable<NotFileGroup>> GetAllNotFileGroupsAsync();

    /// <summary>
    /// 获取用户的所有文件组。
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <returns>用户的所有文件组实例</returns>
    Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId);

    /// <summary>
    /// 获取所有公共文件组。
    /// </summary>
    /// <returns>所有公共文件组实例</returns>
    Task<IEnumerable<NotFileGroup>> GetPublicNotFileGroupsAsync();

    /// <summary>
    /// 根据名称获取文件组。
    /// </summary>
    /// <param name="fileGroupName">文件组名称</param>
    /// <returns>文件组实例（如果存在）</returns>
    Task<NotFileGroup?> GetNotFileGroupByNameAsync(string fileGroupName);

    /// <summary>
    /// 更新文件组。
    /// </summary>
    /// <param name="notFileGroup">要更新的文件组</param>
    /// <returns>更新后的文件组实例（如果存在）</returns>
    /// <exception cref="ArgumentException">如果文件组不存在</exception>
    /// <exception cref="ArgumentNullException">如果文件组名称为空</exception>
    /// <exception cref="ArgumentException">如果文件组名称已存在</exception>
    /// <exception cref="ArgumentException">如果文件组身份与用户身份不匹配</exception>
    /// <exception cref="ArgumentException">如果文件组标签包含空字符串</exception>
    /// <exception cref="ArgumentException">如果文件组描述包含空字符串</exception>
    Task<NotFileGroup?> UpdateNotFileGroupAsync(NotFileGroup notFileGroup);

    /// <summary>
    /// 删除文件组。
    /// </summary>
    /// <param name="notFileGroupId">要删除的文件组 ID</param>
    /// <exception cref="ArgumentException">如果文件组不存在</exception>
    Task DeleteNotFileGroupAsync(Guid notFileGroupId);

    /// <summary>
    /// 检查指定用户、指定父级下是否存在同名文件组（排除自身，用于更新场景）。
    /// </summary>
    /// <param name="userId">所属用户 ID</param>
    /// <param name="parentGroupId">父组 ID，null 表示根级</param>
    /// <param name="name">文件组名称</param>
    /// <param name="excludeId">需要排除的自身 ID（可选，创建时传 null）</param>
    /// <returns>true = 同名已存在</returns>
    Task<bool> ExistsByNameAtSameLevelAsync(Guid userId, Guid? parentGroupId, string name, Guid? excludeId = null);

    /// <summary>
    /// 获取指定父组下的所有子组。
    /// </summary>
    Task<IEnumerable<NotFileGroup>> GetChildrenAsync(Guid parentGroupId);

    /// <summary>
    /// 获取用户的所有根级文件组（ParentGroupId == null）。
    /// </summary>
    Task<IEnumerable<NotFileGroup>> GetRootGroupsByUserIdAsync(Guid userId);
}
