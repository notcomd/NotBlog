using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Domain.SeedWork;
using Markdown.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Markdown.Infrastructure.Repository;

public class MarkDownRepository(MarkDownDbContext markDownDbContext, ILogger<IMarkdownRepository> logger)
    : IMarkDownDbContextRepository<MarkDown>
{
    public IUnitOfWork UnitOfWork => markDownDbContext;

    /// <summary>
    ///     添加 Markdown 文档
    /// </summary>
    public async Task AddMarkDownAsync()
    {
        // 由调用方通过 DbContext 的 Add 方法添加实体
        // 这里不负责具体添加逻辑，只负责仓储操作
        await Task.CompletedTask;
    }

    /// <summary>
    ///     根据条件查找 Markdown 文档
    /// </summary>
    public async Task<MarkDown> FindAsync(MarkDown entityDbcontext)
    {
        if (entityDbcontext is null)
            throw new ArgumentNullException(nameof(entityDbcontext));

        var markdown = await markDownDbContext.Markdowns
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MarkDownGuid == entityDbcontext.MarkDownGuid);

        if (markdown is null)
        {
            logger.LogWarning("未找到 Markdown 文档：{MarkDownGuid}", entityDbcontext.MarkDownGuid);
            throw new KeyNotFoundException($"Markdown 文档不存在：{entityDbcontext.MarkDownGuid}");
        }

        return markdown;
    }

    /// <summary>
    ///     更新 Markdown 文档
    /// </summary>
    public async Task UpDataAsync(MarkDown entityDbcontext)
    {
        if (entityDbcontext is null)
            throw new ArgumentNullException(nameof(entityDbcontext));

        try
        {
            var existing = await markDownDbContext.Markdowns
                .FirstOrDefaultAsync(x => x.MarkDownGuid == entityDbcontext.MarkDownGuid);

            if (existing is null)
            {
                logger.LogWarning("尝试更新不存在的 Markdown 文档：{MarkDownGuid}", entityDbcontext.MarkDownGuid);
                throw new KeyNotFoundException($"Markdown 文档不存在：{entityDbcontext.MarkDownGuid}");
            }

            // 使用实体的更新方法
            await existing.UpDataByMarkDownAsync(
                entityDbcontext.MarkDownName,
                entityDbcontext.MarkDownContent,
                entityDbcontext.MarkDownHash);
            logger.LogInformation("Markdown 文档已更新：{MarkDownGuid}", entityDbcontext.MarkDownGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "更新 Markdown 文档失败：{MarkDownGuid}", entityDbcontext.MarkDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     删除 Markdown 文档（软删除）
    /// </summary>
    public async Task DeleteAsync(MarkDown entityDbContext)
    {
        if (entityDbContext is null)
            throw new ArgumentNullException(nameof(entityDbContext));

        try
        {
            var existing = await markDownDbContext.Markdowns
                .FirstOrDefaultAsync(x => x.MarkDownGuid == entityDbContext.MarkDownGuid);

            if (existing is null)
            {
                logger.LogWarning("尝试删除不存在的 Markdown 文档：{MarkDownGuid}", entityDbContext.MarkDownGuid);
                throw new KeyNotFoundException($"Markdown 文档不存在：{entityDbContext.MarkDownGuid}");
            }

            // 调用实体的软删除方法（如果有的话）
            // 这里直接标记为删除
            markDownDbContext.Markdowns.Remove(existing);
            logger.LogInformation("Markdown 文档已删除：{MarkDownGuid}", entityDbContext.MarkDownGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除 Markdown 文档失败：{MarkDownGuid}", entityDbContext.MarkDownGuid);
            throw;
        }
    }
}