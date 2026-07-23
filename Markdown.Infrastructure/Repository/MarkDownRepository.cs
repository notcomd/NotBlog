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
    /// 根据 GUID 查找 Markdown 文档（追踪态，用于更新操作）
    /// </summary>
    public async Task<MarkDown?> GetMarkDownTrackedAsync(Guid markDownGuid)
    {
        try
        {
            var markdown = await markDownDbContext.Markdowns
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
                .OrderByDescending(x => x.CreateAt)
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
                .OrderByDescending(x => x.CreateAt)
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
                .Where(x => x.MarkDownAuth.Equals(markDownAuth) && !x.IsDelete)
                .OrderByDescending(x => x.CreateAt)
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
}