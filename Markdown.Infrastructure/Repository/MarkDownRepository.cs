

namespace Markdown.Infrastructure.Repository;

/// <summary>
///     MarkDown 聚合根仓储实现（唯一的数据访问入口，所有聚合内实体的操作均通过聚合根进行）
/// </summary>
public class MarkDownRepository(
    MarkDownDbContext markDownDbContext,
    ILogger<MarkDownRepository> logger) : IMarkdownRepository
{
    public IUnitOfWork UnitOfWork => markDownDbContext;

    // ==================== MarkDown 聚合根查询 ====================

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
                logger.LogWarning("Markdown 文档不存在：{MarkDownGuid}", markDownGuid);

            return markdown;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查找 Markdown 文档失败：{MarkDownGuid}", markDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     根据 GUID 查找 Markdown 文档
    /// </summary>
    public async Task<MarkDown?> FindMarkDownAsync(Guid markDownGuid)
    {
        try
        {
            var markdown = await markDownDbContext.Markdowns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MarkDownGuid == markDownGuid);

            if (markdown is null)
                logger.LogWarning("Markdown 文档不存在：{MarkDownGuid}", markDownGuid);

            return markdown;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查找 Markdown 文档失败：{MarkDownGuid}", markDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     根据用户 GUID 查找所有 Markdown 文档
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
                logger.LogWarning("未找到名为 {MarkDownName} 的 Markdown 文档", markDownName);

            return markdown;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "按名称查找 Markdown 文档失败：{MarkDownName}", markDownName);
            throw;
        }
    }

    /// <summary>
    ///     根据文档名称模糊查找所有 Markdown 文档
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
    ///     根据权限类型查找所有 Markdown 文档
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

    // ==================== 评论查询（通过聚合根导航属性访问，不暴露 MarkReview 独立仓储） ====================

    /// <summary>
    ///     根据 Markdown ID 获取所有顶级评论（通过聚合根导航属性访问）
    /// </summary>
    public async Task<IEnumerable<MarkReview>> GetReviewsByMarkdownIdAsync(Guid markdownGuid)
    {
        try
        {
            var reviews = await markDownDbContext.Markdowns
                .Where(x => x.MarkDownGuid == markdownGuid)
                .SelectMany(m => m.MarkReviews)
                .Where(r => r.MarkAggregateRootGuid == null)
                .OrderByDescending(r => r.MarkReviewTime)
                .ToListAsync();

            logger.LogInformation("获取到 Markdown {MarkdownGuid} 的 {Count} 条评论", markdownGuid, reviews.Count);
            return reviews;
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
            var review = await markDownDbContext.Markdowns
                .SelectMany(m => m.MarkReviews)
                .FirstOrDefaultAsync(r => r.MarkReviewGuid == reviewGuid);

            if (review is null)
                logger.LogWarning("评论不存在：{ReviewGuid}", reviewGuid);

            return review;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取评论失败：{ReviewGuid}", reviewGuid);
            throw;
        }
    }

    /// <summary>
    ///     获取某条评论的所有子评论（通过聚合根查找）
    /// </summary>
    public async Task<IEnumerable<MarkReview>> GetChildReviewsAsync(Guid parentReviewGuid)
    {
        try
        {
            var childReviews = await markDownDbContext.Markdowns
                .SelectMany(m => m.MarkReviews)
                .Where(r => r.MarkAggregateRootGuid == parentReviewGuid)
                .OrderBy(r => r.MarkReviewTime)
                .ToListAsync();

            logger.LogInformation("获取到父评论 {ParentGuid} 的 {Count} 条子评论", parentReviewGuid, childReviews.Count);
            return childReviews;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取子评论失败：{ParentGuid}", parentReviewGuid);
            throw;
        }
    }

    // ==================== 聚合根持久化操作（增删改） ====================

    /// <summary>
    ///     添加新的 MarkDown 聚合根
    /// </summary>
    public async Task<MarkDown> AddAsync(MarkDown markDown)
    {
        ArgumentNullException.ThrowIfNull(markDown);

        try
        {
            await markDownDbContext.Markdowns.AddAsync(markDown);
            logger.LogInformation("MarkDown 聚合根已添加：{MarkDownGuid}", markDown.MarkDownGuid);
            return markDown;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加 MarkDown 聚合根失败：{MarkDownGuid}", markDown.MarkDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     更新 MarkDown 聚合根（EF Core 追踪变更，由 UnitOfWork 统一持久化）
    /// </summary>
    public Task<MarkDown> UpdateAsync(MarkDown markDown)
    {
        ArgumentNullException.ThrowIfNull(markDown);

        try
        {
            // EF Core 变更追踪器自动检测实体状态，无需显式调用 Update
            markDownDbContext.Markdowns.Update(markDown);
            logger.LogInformation("MarkDown 聚合根已标记更新：{MarkDownGuid}", markDown.MarkDownGuid);
            return Task.FromResult(markDown);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "更新 MarkDown 聚合根失败：{MarkDownGuid}", markDown.MarkDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     删除 MarkDown 聚合根（软删除，通过聚合根方法执行）
    /// </summary>
    public async Task DeleteAsync(MarkDown markDown)
    {
        ArgumentNullException.ThrowIfNull(markDown);

        try
        {
            // 通过聚合根方法执行软删除，维护领域一致性
            markDown.SoftDelete();
            logger.LogInformation("MarkDown 聚合根已软删除：{MarkDownGuid}", markDown.MarkDownGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除 MarkDown 聚合根失败：{MarkDownGuid}", markDown.MarkDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     根据 GUID 删除 MarkDown 聚合根（软删除）
    /// </summary>
    public async Task DeleteAsync(Guid markDownGuid)
    {
        try
        {
            var markDown = await GetMarkDownTrackedAsync(markDownGuid)
                ?? throw new KeyNotFoundException($"MarkDown 文档不存在：{markDownGuid}");

            // 通过聚合根方法执行软删除，维护领域一致性
            markDown.SoftDelete();
            logger.LogInformation("MarkDown 聚合根已软删除：{MarkDownGuid}", markDownGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除 MarkDown 聚合根失败：{MarkDownGuid}", markDownGuid);
            throw;
        }
    }

    // ==================== OldMarkDown 历史版本查询 ====================

    /// <summary>
    ///     获取指定文档的所有历史版本
    /// </summary>
    public async Task<IEnumerable<OldMarkDown>> GetOldMarkDownsByMarkDownGuidAsync(Guid markDownGuid)
    {
        try
        {
            var oldVersions = await markDownDbContext.Markdowns
                .Where(m => m.MarkDownGuid == markDownGuid)
                .SelectMany(m => m.OldMarkDowns)
                .Where(o => !o.IsDelete)
                .OrderByDescending(o => o.CreateAt)
                .ToListAsync();

            logger.LogInformation("获取到 Markdown {Guid} 的 {Count} 个历史版本", markDownGuid, oldVersions.Count);
            return oldVersions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取历史版本失败：{MarkDownGuid}", markDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     根据 GUID 获取单个历史版本
    /// </summary>
    public async Task<OldMarkDown?> GetOldMarkDownByGuidAsync(Guid oldMarkDownGuid)
    {
        try
        {
            var oldVersion = await markDownDbContext.Markdowns
                .SelectMany(m => m.OldMarkDowns)
                .FirstOrDefaultAsync(o => o.OldMarkDownGuid == oldMarkDownGuid);

            if (oldVersion is null)
                logger.LogWarning("历史版本不存在：{OldMarkDownGuid}", oldMarkDownGuid);

            return oldVersion;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取历史版本失败：{OldMarkDownGuid}", oldMarkDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     软删除历史版本记录
    /// </summary>
    public async Task DeleteOldMarkDownAsync(Guid oldMarkDownGuid)
    {
        try
        {
            var oldVersion = await markDownDbContext.Markdowns
                .SelectMany(m => m.OldMarkDowns)
                .FirstOrDefaultAsync(o => o.OldMarkDownGuid == oldMarkDownGuid)
                ?? throw new KeyNotFoundException($"历史版本不存在：{oldMarkDownGuid}");

            oldVersion.SoftDelete();
            logger.LogInformation("历史版本已软删除：{OldMarkDownGuid}", oldMarkDownGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除历史版本失败：{OldMarkDownGuid}", oldMarkDownGuid);
            throw;
        }
    }

    // ==================== 评论持久化操作（通过聚合根） ====================

    /// <summary>
    ///     通过聚合根添加评论
    /// </summary>
    public async Task<MarkReview> AddReviewAsync(Guid markDownGuid, MarkReview review)
    {
        ArgumentNullException.ThrowIfNull(review);

        try
        {
            var markDown = await GetMarkDownTrackedAsync(markDownGuid)
                ?? throw new KeyNotFoundException($"MarkDown 文档不存在：{markDownGuid}");

            if (markDown.IsDelete)
                throw new InvalidOperationException("已删除的文档无法添加评论");

            await markDown.AddByMarkReviewAsync(review);
            logger.LogInformation("已为文档 {MarkDownGuid} 添加评论 {ReviewGuid}", markDownGuid, review.MarkReviewGuid);
            return review;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加评论失败：{MarkDownGuid}", markDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     更新评论内容
    /// </summary>
    public async Task<MarkReview> UpdateReviewAsync(Guid reviewGuid, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentNullException(nameof(content));

        try
        {
            var review = await markDownDbContext.Markdowns
                .SelectMany(m => m.MarkReviews)
                .FirstOrDefaultAsync(r => r.MarkReviewGuid == reviewGuid)
                ?? throw new KeyNotFoundException($"评论不存在：{reviewGuid}");

            if (review.IsDelete)
                throw new InvalidOperationException("已删除的评论无法修改");

            // 使用领域方法更新内容
            review.UpdateContent(content);

            logger.LogInformation("评论已更新：{ReviewGuid}", reviewGuid);
            return review;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "更新评论失败：{ReviewGuid}", reviewGuid);
            throw;
        }
    }

    /// <summary>
    ///     软删除评论（通过聚合根）
    /// </summary>
    public async Task DeleteReviewAsync(Guid reviewGuid)
    {
        try
        {
            // 获取评论所属的文档，通过聚合根删除
            var markDown = await markDownDbContext.Markdowns
                .Include(m => m.MarkReviews)
                .FirstOrDefaultAsync(m => m.MarkReviews.Any(r => r.MarkReviewGuid == reviewGuid))
                ?? throw new KeyNotFoundException($"未找到包含评论 {reviewGuid} 的文档");

            if (markDown.IsDelete)
                throw new InvalidOperationException("已删除的文档无法操作评论");

            markDown.RemoveReview(reviewGuid);
            logger.LogInformation("评论已软删除：{ReviewGuid}", reviewGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除评论失败：{ReviewGuid}", reviewGuid);
            throw;
        }
    }

    // ==================== 列表查询 ====================

    /// <summary>
    ///     获取所有非删除的公开 Markdown 文档（分页）
    /// </summary>
    public async Task<IEnumerable<MarkDown>> FindAllMarkDownsAsync(int skip = 0, int take = 20)
    {
        try
        {
            var markdowns = await markDownDbContext.Markdowns
                .AsNoTracking()
                .Where(x => !x.IsDelete && x.MarkDownAuth == MarkDownAuth.PublicMark && x.Status == MarkStatus.MarkApproved)
                .OrderByDescending(x => x.CreateAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            logger.LogInformation("获取公开文档列表，共 {Count} 篇", markdowns.Count);
            return markdowns;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取公开文档列表失败");
            throw;
        }
    }

    // ==================== 子评论（F-10.4）与计数（F-10.5） ====================

    /// <summary>
    ///     通过聚合根添加子评论到指定父评论
    /// </summary>
    public async Task<MarkReview> AddChildReviewAsync(Guid markDownGuid, Guid parentReviewGuid, MarkReview childReview)
    {
        ArgumentNullException.ThrowIfNull(childReview);

        try
        {
            var markDown = await markDownDbContext.Markdowns
                .Include(m => m.MarkReviews)
                .FirstOrDefaultAsync(m => m.MarkDownGuid == markDownGuid)
                ?? throw new KeyNotFoundException($"MarkDown 文档不存在：{markDownGuid}");

            if (markDown.IsDelete)
                throw new InvalidOperationException("已删除的文档无法添加评论");

            if (markDown.FindReview(parentReviewGuid) is null)
                throw new KeyNotFoundException($"父评论不存在：{parentReviewGuid}");

            markDown.AddChildReview(parentReviewGuid, childReview);
            logger.LogInformation("已为文档 {MarkDownGuid} 的父评论 {ParentGuid} 添加子评论 {ChildGuid}",
                markDownGuid, parentReviewGuid, childReview.MarkReviewGuid);
            return childReview;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加子评论失败：{MarkDownGuid}", markDownGuid);
            throw;
        }
    }

    /// <summary>
    ///     评论点赞 +1（线程安全），返回最新点赞数
    /// </summary>
    public async Task<long> LikeReviewAsync(Guid reviewGuid)
    {
        try
        {
            var review = await LoadTrackedReviewAsync(reviewGuid);
            var count = review.MarkQuote.AddLove();
            logger.LogInformation("评论 {ReviewGuid} 点赞数更新为 {Count}", reviewGuid, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "评论点赞失败：{ReviewGuid}", reviewGuid);
            throw;
        }
    }

    /// <summary>
    ///     取消评论点赞 -1（不低于 0），返回最新点赞数
    /// </summary>
    public async Task<long> RemoveLikeReviewAsync(Guid reviewGuid)
    {
        try
        {
            var review = await LoadTrackedReviewAsync(reviewGuid);
            var count = review.MarkQuote.RemoveLove();
            logger.LogInformation("评论 {ReviewGuid} 取消点赞后点赞数为 {Count}", reviewGuid, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "评论取消点赞失败：{ReviewGuid}", reviewGuid);
            throw;
        }
    }

    /// <summary>
    ///     评论浏览量 +1（线程安全），返回最新浏览数
    /// </summary>
    public async Task<long> IncreaseReviewViewAsync(Guid reviewGuid)
    {
        try
        {
            var review = await LoadTrackedReviewAsync(reviewGuid);
            var count = review.MarkQuote.AddView();
            logger.LogInformation("评论 {ReviewGuid} 浏览数更新为 {Count}", reviewGuid, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "评论浏览计数失败：{ReviewGuid}", reviewGuid);
            throw;
        }
    }

    /// <summary>
    ///     加载追踪态评论（用于计数更新），校验存在性与删除状态
    /// </summary>
    private async Task<MarkReview> LoadTrackedReviewAsync(Guid reviewGuid)
    {
        var review = await markDownDbContext.Markdowns
            .SelectMany(m => m.MarkReviews)
            .FirstOrDefaultAsync(r => r.MarkReviewGuid == reviewGuid)
            ?? throw new KeyNotFoundException($"评论不存在：{reviewGuid}");

        if (review.IsDelete)
            throw new InvalidOperationException("已删除的评论无法操作");

        return review;
    }
}
