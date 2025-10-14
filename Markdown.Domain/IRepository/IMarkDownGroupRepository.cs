


using DomainCommon;
using Markdown.Domain.Entities;

namespace Markdown.Domain.IRepository;

public interface IMarkDownGroupRepository : IRepository<MarkDownGroup>
{
    /// <summary>
    /// 根据MarkDownUserId获取MarkDownGroup实体的列表
    /// </summary>
    /// <param name="markDownUserId">MarkDownUser实体的ID</param>
    /// <returns>MarkDownGroup实体的列表</returns>
    Task<List<MarkDownGroup>> GetMarkDownGroupsByMarkDownUserIdAsync(Guid markDownUserId);

    /// <summary>
    /// 根据MarkDownGroupId获取MarkDownGroup实体的列表
    /// </summary>
    /// <param name="markDownGroupId">MarkDownGroup实体的ID</param>
    /// <returns>MarkDownGroup实体的列表</returns>
    ValueTask<List<MarkDownGroup>> GetMarkDownGroupsByMarkDownGroupIdAsync(Guid markDownGroupId);

    /// <summary>
    /// 添加MarkDownGroup实体
    /// </summary>
    /// <param name="markDownGroup">MarkDownGroup实体</param>
    /// <returns>任务</returns>
    Task AddMarkDownGroupAsync(MarkDownGroup markDownGroup);

    /// <summary>
    /// 更新MarkDownGroup实体
    /// </summary>
    /// <param name="markDownGroup">MarkDownGroup实体</param>
    /// <returns>任务</returns>
    Task UpdateMarkDownGroupAsync(MarkDownGroup markDownGroup);

    /// <summary>
    /// 删除MarkDownGroup实体
    /// </summary>
    /// <param name="markDownGroup">MarkDownGroup实体</param>
    /// <returns>任务</returns>
    Task DeleteMarkDownGroupAsync(MarkDownGroup markDownGroup);

    
}
