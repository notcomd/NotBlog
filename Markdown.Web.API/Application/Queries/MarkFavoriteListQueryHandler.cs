using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Application.Queries;

/// <summary>
///     我的收藏列表查询处理器：按用户 + 可选 tag 分类过滤（SQL LIKE 精确匹配 JSON 数组元素），
///     分页取收藏后按 ID 批量联查文章名称；已删除文章（IsDelete）不展示
/// </summary>
public class MarkFavoriteListQueryHandler(
    MarkDownDbContext dbContext,
    ILogger<MarkFavoriteListQueryHandler> logger) : NotMediator.IRequestHandler<MarkFavoriteListQuery, List<MarkFavoriteResponse>>
{
    public async Task<List<MarkFavoriteResponse>> Handler(MarkFavoriteListQuery request, CancellationToken cancellationToken)
    {
        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        var query = dbContext.MarkFavorites.AsNoTracking()
            .Where(f => f.UserGuid == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            // TagsJson 为 JSON 数组文本（如 ["a","b"]）：转义 tag 后精确匹配元素，避免子串误匹配/注入
            var escaped = request.Tag.Trim().Replace("\\", "\\\\").Replace("\"", "\\\"");
            var pattern = $"%\"{escaped}\"%";
            query = query.Where(f => EF.Functions.Like(f.TagsJson, pattern));
        }

        // 第一步：分页查询收藏实体（TagsJson 含标签，文章名随后批量回查）
        var favorites = await query
            .OrderByDescending(f => f.CreateAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (favorites.Count == 0)
            return [];

        // 第二步：按收藏文章 ID 批量取文章名称（过滤已删除文章）
        var markDownIds = favorites.Select(f => f.MarkDownGuid).ToList();
        var markdownNames = await dbContext.Markdowns.AsNoTracking()
            .Where(m => !m.IsDelete && markDownIds.Contains(m.MarkDownGuid))
            .ToDictionaryAsync(m => m.MarkDownGuid, m => m.MarkDownName, cancellationToken);

        var result = favorites
            .Where(f => markdownNames.ContainsKey(f.MarkDownGuid))
            .Select(f => new MarkFavoriteResponse
            {
                MarkFavoriteGuid = f.MarkFavoriteGuid,
                MarkDownGuid = f.MarkDownGuid,
                MarkDownName = markdownNames[f.MarkDownGuid],
                Tags = JsonSerializer.Deserialize<List<string>>(f.TagsJson) ?? [],
                CreateAt = f.CreateAt
            })
            .ToList();

        logger.LogInformation("收藏列表查询完成，共 {Count} 条（用户 {UserGuid}，Tag={Tag}）",
            result.Count, request.UserId, request.Tag);
        return result;
    }
}
