using FileDev.Domain.DomainEntities;
using DomainCommon;
namespace FileDev.Domain.IRepository;
/// <summary>
/// FileGroup 仓储接口
/// </summary>
public interface IFileGroupRepository : IRepository<FileGroup>
{
    /// <summary>
    /// 根据 ID 查询 FileGroup
    /// </summary>
    /// <param name="fileGroupId">FileGroup 的 ID</param>
    /// <returns>FileGroup 实体</returns>
    ValueTask<FileGroup> QueryFileGroupByIdAsync(Guid fileGroupId);


    ValueTask<FileGroup> QueryFileGroupByNameAsync(string fileGroupName);



    /// <summary>
    /// 创建新的 FileGroup
    /// </summary>
    /// <param name="fileGroup">要创建的 FileGroup 实体</param>
    /// <returns>创建后的 FileGroup 实体</returns>
    ValueTask<FileGroup> CreateFileGroupAsync(FileGroup fileGroup);



    /// <summary>
    /// 删除指定 ID 的 FileGroup
    /// </summary>
    /// <param name="fileGroupId">要删除的 FileGroup 的 ID</param>
    /// <returns>删除操作是否成功</returns>
    ValueTask<bool> DeleteFileGroupByIdAsync(Guid fileGroupId);

    /// <summary>
    /// 查询所有 FileGroup
    /// </summary>
    /// <returns>FileGroup 列表</returns>
    ValueTask<List<FileGroup>> QueryAllFileGroupsAsync();


    /// <summary>
    /// 根据 ID 更新 FileGroup 信息，使用指定的更新操作
    /// </summary>
    /// <param name="fileGroupId">要更新的 FileGroup 的 ID</param>
    /// <param name="updateAction">对 FileGroup 进行更新的操作</param>
    /// <returns>表示异步操作的任务</returns>
    ValueTask UpdateFileGroupAsync(Guid fileGroupId, Func<FileGroup, Task> updateAction);

    /// <summary>
    /// 直接使用完整实体更新 FileGroup 信息
    /// </summary>
    /// <param name="fileGroup">要更新的 FileGroup 实体</param>
    /// <returns>更新操作是否成功</returns>
    ValueTask<bool> UpdateFileGroupAsync(FileGroup fileGroup);

}
