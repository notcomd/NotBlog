using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Domain.SeedWork;
using Markdown.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Markdown.Infrastructure.Repository;

/// <summary>
///     MarkReview 仓储实现（通过聚合根 MarkDown 访问）
/// </summary>
public class MarkReviewRepository(
    MarkDownDbContext markDownDbContext,
    ILogger<IMarkReviewRepository> logger) : IMarkReviewRepository
{
    public IUnitOfWork UnitOfWork => markDownDbContext ?? throw new ArgumentNullException(nameof(markDownDbContext));

    /// <summary>
    ///     根据 Markdown ID 获取所有评论（通过聚合根导航属性访问）
    /// </summary>
    public async Task<IEnumerable<MarkReview>> GetReviewsByMarkdownIdAsync(Guid markdownGuid)
    {
        try
        {
            // 通过聚合根 MarkDown 访问子聚合 MarkReview
            var markdown = await markDownDbContext.Markdowns
                .Where(x => x.MarkDownGuid == markdownGuid)
                .SelectMany(m => m.MarkReviews) // 使用 SelectMany 展开评论集合
                .Where(r => r.MarkAggregateRootGuid == null) // 只获取顶级评论
                .OrderByDescending(r => r.MarkReviewTime)
                .ToListAsync();

            logger.LogInformation("获取到 Markdown {MarkdownGuid} 的 {Count} 条评论", markdownGuid, markdown.Count);
            return markdown;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取评论失败：{MarkdownGuid}", markdownGuid);
            throw;
        }
    }

    /// <summary>
    ///     根据评论 ID 获取评论（通过聚合根查找）
    /// </summary>
    public async Task<MarkReview?> GetReviewByIdAsync(Guid reviewGuid)
    {
        try
        {
            // 通过聚合根查找子评论
            var review = await markDownDbContext.Markdowns
                .SelectMany(m => m.MarkReviews)
                .FirstOrDefaultAsync(r => r.MarkReviewGuid == reviewGuid);

            if (review is null)
            {
                logger.LogWarning("评论不存在：{ReviewGuid}", reviewGuid);
            }

            return review;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取评论失败：{ReviewGuid}", reviewGuid);
            throw;
        }
    }

    /// <summary>
    ///     获取某条评论的所有子评论
    /// </summary>
    public async Task<IEnumerable<MarkReview>> GetChildReviewsAsync(Guid aggregateRootGuid)
    {
        try
        {
            // 通过聚合根查找子评论
            var childReviews = await markDownDbContext.Markdowns
                .SelectMany(m => m.MarkReviews)
                .Where(r => r.MarkAggregateRootGuid == aggregateRootGuid)
                .OrderBy(r => r.MarkReviewTime)
                .ToListAsync();

            logger.LogInformation("获取到父评论 {ParentGuid} 的 {Count} 条子评论", aggregateRootGuid, childReviews.Count);
            return childReviews;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取子评论失败：{ParentGuid}", aggregateRootGuid);
            throw;
        }
    }

    /// <summary>
    ///     添加评论到聚合根
    /// </summary>
    public async Task AddReviewAsync(MarkReview review)
    {
        if (review is null)
            throw new ArgumentNullException(nameof(review));

        try
        {
            // 通过聚合根添加评论
            var markdown = await markDownDbContext.Markdowns
                .FirstOrDefaultAsync(x => x.MarkDownGuid == review.MarkDownGuid);

            if (markdown is null)
            {
                logger.LogWarning("尝试为不存在的 MarkDown 添加评论：{MarkDownGuid}", review.MarkDownGuid);
                throw new KeyNotFoundException($"MarkDown 文档不存在：{review.MarkDownGuid}");
            }

            // 使用聚合根的方法添加评论
            await markdown.AddByMarkReviewAsync(review);

            // EF Core 会自动追踪添加到导航属性的实体
            logger.LogInformation("评论已添加到聚合根：{ReviewGuid}", review.MarkReviewGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加评论失败：{ReviewGuid}", review.MarkReviewGuid);
            throw;
        }
    }

    /// <summary>
    ///     删除评论（包括级联删除子评论）
    /// </summary>
    public async Task DeleteReviewAsync(MarkReview review)
    {
        if (review is null)
            throw new ArgumentNullException(nameof(review));

        try
        {
            // 先获取所有子评论
            var childReviews = await GetChildReviewsAsync(review.MarkReviewGuid);

            // 递归删除子评论
            foreach (var childReview in childReviews)
            {
                await DeleteReviewAsync(childReview);
            }

            // 从聚合根的导航属性中移除
            var markdown = await markDownDbContext.Markdowns
                .FirstOrDefaultAsync(x => x.MarkDownGuid == review.MarkDownGuid);

            if (markdown is not null)
            {
                markdown.MarkReviews.Remove(review);
            }

            // 同时从 DbContext 中移除实体
            markDownDbContext.Entry(review).State = EntityState.Deleted;

            logger.LogInformation("评论已删除：{ReviewGuid}", review.MarkReviewGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除评论失败：{ReviewGuid}", review.MarkReviewGuid);
            throw;
        }
    }

    /// <summary>
    ///     添加子评论到父评论
    /// </summary>
    public async Task AddChildReviewAsync(Guid parentReviewGuid, MarkReview childReview)
    {
        if (childReview is null)
            throw new ArgumentNullException(nameof(childReview));

        try
        {
            // 验证父评论是否存在
            var parentReview = await GetReviewByIdAsync(parentReviewGuid);
            if (parentReview is null)
            {
                logger.LogWarning("父评论不存在：{ParentGuid}", parentReviewGuid);
                throw new KeyNotFoundException($"父评论不存在：{parentReviewGuid}");
            }

            var review = new MarkReview(childReview.MarkDownGuid, childReview.UserId, childReview.MarkReviewContent,
                childReview?.ReviewImages?.ToList());
            // 设置聚合根 GUID
            await parentReview.AddToChildReviewAsync(parentReviewGuid, review);

            // 增加父评论的回复计数
            parentReview.MarkQuote.AddReview();

            // 将子评论添加到聚合根
            var markdown = await markDownDbContext.Markdowns
                .FirstOrDefaultAsync(x => x.MarkDownGuid == parentReview.MarkDownGuid);

            if (markdown is not null)
            {
                await markdown.AddByMarkReviewAsync(childReview);
            }

            logger.LogInformation("子评论已添加到父评论：{ParentGuid} -> {ChildGuid}",
                parentReviewGuid, childReview.MarkReviewGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加子评论失败：{ParentGuid}", parentReviewGuid);
            throw;
        }
    }
}