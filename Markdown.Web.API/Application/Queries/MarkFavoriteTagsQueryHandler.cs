using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Application.Queries;

/// <summary>
///     我的收藏标签库查询处理器：返回用户使用过的收藏标签（去重），
///     按使用次数优先、最近使用次之排序，支持按关键字模糊过滤
/// </summary>
public class MarkFavoriteTagsQueryHandler(
    MarkDownDbContext dbContext,
    ILogger<MarkFavoriteTagsQueryHandler> logger) :  IRequestHandler<MarkFavoriteTagsQuery, List<MarkFavoriteTagResponse>>
{
    public async Task<List<MarkFavoriteTagResponse>> Handler(MarkFavoriteTagsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = dbContext.MarkFavoriteTags.AsNoTracking()
            .Where(t => t.UserGuid == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
            query = query.Where(t => t.Tag.Contains(request.Keyword.Trim()));

        var tags = await query
            .OrderByDescending(t => t.UseCount)
            .ThenByDescending(t => t.LastUsedAt)
            .Take(limit)
            .Select(t => new MarkFavoriteTagResponse
            {
                Tag = t.Tag,
                UseCount = t.UseCount,
                LastUsedAt = t.LastUsedAt
            })
            .ToListAsync(cancellationToken);

        logger.LogInformation("标签库查询完成，共 {Count} 个（用户 {UserGuid}，Keyword={Keyword}）",
            tags.Count, request.UserId, request.Keyword);
        return tags;
    }
}
