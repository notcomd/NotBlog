using DomainCommon;

using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Infrastructures.DbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Markdown.Infrastructres.Repository;

public class MarkDownGroupRepository : IMarkDownGroupRepository
{

    private readonly MarkdownDbContext _dbContext;

    public IUnitOfWork UnitOfWork => _dbContext;

    private readonly ILogger<MarkDownGroupRepository> _logger;

    private readonly INotDateTime _notDateTimeProvider;

    public MarkDownGroupRepository(MarkdownDbContext dbContext, ILogger<MarkDownGroupRepository> logger, INotDateTime notDateTimeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notDateTimeProvider = notDateTimeProvider;
    }

    /// <summary>
    /// 根据MarkDownUserId获取MarkDownGroup实体的列表
    /// </summary>
    /// <param name="markDownUserId">MarkDownUser实体的ID</param>
    /// <returns>MarkDownGroup实体的列表</returns>
    async Task<List<MarkDownGroup>> GetMarkDownGroupsByMarkDownUserIdAsync(Guid markDownUserId)
    {
        return await _dbContext.MarkDownGroup
            .Where(x => x.MarkDownUserId == markDownUserId)
            .ToListAsync();
    }

    /// <summary>
    /// 根据MarkDownGroupId获取MarkDownGroup实体的列表
    /// </summary>
    /// <param name="markDownGroupId">MarkDownGroup实体的ID</param>
    /// <returns>MarkDownGroup实体的列表</returns>
    async ValueTask<List<MarkDownGroup>> GetMarkDownGroupsByMarkDownGroupIdAsync(Guid markDownGroupId)
    {
        return await _dbContext.MarkDownGroup.Where(x => x.Id == markDownGroupId).ToListAsync();
    }

    /// <summary>
    /// 添加MarkDownGroup实体
    /// </summary>
    /// <param name="markDownGroup">MarkDownGroup实体</param>
    /// <returns>任务</returns>
    async Task AddMarkDownGroupAsync(MarkDownGroup markDownGroup)
    {
        await _dbContext.MarkDownGroup.AddAsync(markDownGroup);
    }

    /// <summary>
    /// 更新MarkDownGroup实体
    /// </summary>
    /// <param name="markDownGroup">MarkDownGroup实体</param>
    /// <returns>任务</returns>
    async Task UpdateMarkDownGroupAsync(MarkDownGroup markDownGroup)
    {
        markDownGroup.= _notDateTimeProvider.Now;
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 删除MarkDownGroup实体
    /// </summary>
    /// <param name="markDownGroup">MarkDownGroup实体</param>
    /// <returns>任务</returns>
    Task DeleteMarkDownGroupAsync(MarkDownGroup markDownGroup)
    {
        _dbContext.MarkDownGroups.Remove(markDownGroup);
        await _dbContext.SaveChangesAsync();
    }

    Task<List<MarkDownGroup>> IMarkDownGroupRepository.GetMarkDownGroupsByMarkDownUserIdAsync(Guid markDownUserId)
    {
        return GetMarkDownGroupsByMarkDownUserIdAsync(markDownUserId);
    }

    ValueTask<List<MarkDownGroup>> IMarkDownGroupRepository.GetMarkDownGroupsByMarkDownGroupIdAsync(Guid markDownGroupId)
    {
        return GetMarkDownGroupsByMarkDownGroupIdAsync(markDownGroupId);
    }

    Task IMarkDownGroupRepository.AddMarkDownGroupAsync(MarkDownGroup markDownGroup)
    {
        return AddMarkDownGroupAsync(markDownGroup);
    }

    Task IMarkDownGroupRepository.UpdateMarkDownGroupAsync(MarkDownGroup markDownGroup)
    {
        return UpdateMarkDownGroupAsync(markDownGroup);
    }

    Task IMarkDownGroupRepository.DeleteMarkDownGroupAsync(MarkDownGroup markDownGroup)
    {
        return DeleteMarkDownGroupAsync(markDownGroup);
    }
}