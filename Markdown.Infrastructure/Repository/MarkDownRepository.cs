using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Domain.SeedWork;
using Markdown.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Markdown.Infrastructure.Repository;

public class MarkDownRepository(MarkDownDbContext markDownDbContext, ILogger<IMarkdownRepository> logger)
    : IMarkdownRepository
{
    public IUnitOfWork UnitOfWork => markDownDbContext;

    /// <summary>
    /// 插入 Markdown 文档
    /// </summary>
    public async Task InsertMarkDownAsync(MarkDown markDown)
    {
        if (markDown is null)
            throw new ArgumentNullException(nameof(markDown));

        try
        {
            await markDownDbContext.Markdowns.AddAsync(markDown);
            logger.LogInformation("Markdown 文档已添加到队列：{MarkDownGuid}", markDown.MarkDownGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加 Markdown 文档失败：{MarkDownGuid}", markDown.MarkDownGuid);
            throw;
        }
    }

    /// <summary>
    /// 根据 GUID 查找 Markdown 文档
    /// </summary>
    public async Task<MarkDown?> FindMarkDownAsync(Guid markDownGuid)
    {
        try
        {
            var markdown = await markDownDbContext.Markdowns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MarkDownGuid == markDownGuid);

            if (markdown is null)
            {
                logger.LogWarning("Markdown 文档不存在：{MarkDownGuid}", markDownGuid);
            }

            return markdown;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查找 Markdown 文档失败：{MarkDownGuid}", markDownGuid);
            throw;
        }
    }

    /// <summary>
    /// 根据用户 GUID 查找所有 Markdown 文档
    /// </summary>
    public async Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(Guid userGuid)
    {
        try
        {
            var markdowns = await markDownDbContext.Markdowns
                .AsNoTracking()
                .Where(x => x.MarkUserGuid == userGuid && !x.IsDelete)
                .OrderByDescending(x => x.UplaodAt)
                .ToListAsync();

            logger.LogInformation("用户 {UserGuid} 共有 {Count} 篇 Markdown 文档", userGuid, markdowns.Count);
            return markdowns;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查找用户文档失败：{UserGuid}", userGuid);
            throw;
        }
    }

    /// <summary>
    ///     根据文档名称查找 Markdown 文档（精确匹配）
    /// </summary>
    public async Task<MarkDown?> FindMarkDownAsync(string markDownName)
    {
        try
        {
            var markdown = await markDownDbContext.Markdowns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MarkDownName == markDownName && !x.IsDelete);

            if (markdown is null)
            {
                logger.LogWarning("未找到名为 {MarkDownName} 的 Markdown 文档", markDownName);
            }

            return markdown;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "按名称查找 Markdown 文档失败：{MarkDownName}", markDownName);
            throw;
        }
    }

    /// <summary>
    /// 根据文档名称模糊查找所有 Markdown 文档
    /// </summary>
    public async Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(string markDownName)
    {
        try
        {
            var markdowns = await markDownDbContext.Markdowns
                .AsNoTracking()
                .Where(x => x.MarkDownName.Contains(markDownName) && !x.IsDelete)
                .OrderByDescending(x => x.UplaodAt)
                .ToListAsync();

            logger.LogInformation("模糊查找到 {Count} 篇名为 {MarkDownName} 的 Markdown 文档", markdowns.Count, markDownName);
            return markdowns;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "模糊查找 Markdown 文档失败：{MarkDownName}", markDownName);
            throw;
        }
    }

    /// <summary>
    /// 根据权限类型查找所有 Markdown 文档
    /// </summary>
    public async Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(MarkDownAuth markDownAuth)
    {
        try
        {
            var markdowns = await markDownDbContext.Markdowns
                .AsNoTracking()
                .Where(x => x.MarkOption.Equals(markDownAuth) && !x.IsDelete)
                .OrderByDescending(x => x.UplaodAt)
                .ToListAsync();

            logger.LogInformation("查找到 {Count} 篇权限为 {MarkDownAuth} 的 Markdown 文档", markdowns.Count, markDownAuth);
            return markdowns;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "按权限查找 Markdown 文档失败：{MarkDownAuth}", markDownAuth);
            throw;
        }
    }

    /// <summary>
    ///  更新 Markdown 文档
    /// </summary>
    public async Task<bool> UpdateMarkDownAsync(MarkDown markDown)
    {
        if (markDown is null)
            throw new ArgumentNullException(nameof(markDown));

        try
        {
            var existing = await markDownDbContext.Markdowns
                .FirstOrDefaultAsync(x => x.MarkDownGuid == markDown.MarkDownGuid);

            if (existing is null)
            {
                logger.LogWarning("尝试更新不存在的 Markdown 文档：{MarkDownGuid}", markDown.MarkDownGuid);
                return false;
            }

            // 使用实体的更新方法
            await existing.UpDataByMarkDownAsync(
                markDown.MarkDownName,
                markDown.MarkDownContent,
                markDown.MarkDownHash);

            logger.LogInformation("Markdown 文档已更新：{MarkDownGuid}", markDown.MarkDownGuid);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "更新 Markdown 文档失败：{MarkDownGuid}", markDown.MarkDownGuid);
            return false;
        }
    }

    /// <summary>
    /// 删除 Markdown 文档（软删除）
    /// </summary>
    public async Task<bool> DeleteMarkDownAsync(MarkDown markDown)
    {
        if (markDown is null)
            throw new ArgumentNullException(nameof(markDown));

        try
        {
            var existing = await markDownDbContext.Markdowns
                .FirstOrDefaultAsync(x => x.MarkDownGuid == markDown.MarkDownGuid);

            if (existing is null)
            {
                logger.LogWarning("尝试删除不存在的 Markdown 文档：{MarkDownGuid}", markDown.MarkDownGuid);
                return false;
            }

            // 使用实体的软删除方法
            existing.SoftDelete();

            logger.LogInformation("Markdown 文档已软删除：{MarkDownGuid}", markDown.MarkDownGuid);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除 Markdown 文档失败：{MarkDownGuid}", markDown.MarkDownGuid);
            return false;
        }
    }
}