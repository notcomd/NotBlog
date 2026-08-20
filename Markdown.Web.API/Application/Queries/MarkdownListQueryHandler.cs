using Markdown.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Application.Queries;

/// <summary>
/// 文章列表查询处理器：仅返回已审核通过且对当前查看者可见的文章（摘要投影，不返回正文）
/// 可见性规则（与 S-10 私有过滤一致）：公开文章所有人可见，非公开文章仅作者可见
/// </summary>
public class MarkdownListQueryHandler(
    MarkDownDbContext dbContext,
    ILogger<MarkdownListQueryHandler> logger) :  IRequestHandler<MarkdownListQuery, List<MarkdownSummaryResponse>>
{
    public async Task<List<MarkdownSummaryResponse>> Handler(MarkdownListQuery request, CancellationToken cancellationToken)
    {
        var viewer = request.ViewerGuid;

        var query = dbContext.Markdowns.AsNoTracking()
            .Where(m => !m.IsDelete
                        && m.Status == MarkStatus.MarkApproved
                        && (m.MarkDownAuth == MarkDownAuth.PublicMark || m.MarkUserGuid == viewer));

        if (request.UserGuid.HasValue)
            query = query.Where(m => m.MarkUserGuid == request.UserGuid.Value);

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        List<MarkDown> markdowns;
        if (string.IsNullOrWhiteSpace(request.Tag))
        {
            // 无标签过滤：分页直接下推 SQL，避免全表加载进内存
            markdowns = await query
                .OrderByDescending(m => m.CreateAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Tagboard 为 JSON 转换列（text），服务端无法翻译 Contains：
            // 先取回候选行的轻量投影（不含 Content 大字段）做标签过滤与分页，
            // 再回查完整实体，避免全量加载 1MB 级正文。
            var all = await query
                .Select(m => new { m.MarkDownGuid, m.MarkDownTagboard, m.CreateAt })
                .OrderByDescending(x => x.CreateAt)
                .ToListAsync(cancellationToken);

            var matchedGuids = all
                .Where(x => x.MarkDownTagboard.Contains(request.Tag))
                .Select(x => x.MarkDownGuid)
                .Skip(skip)
                .Take(take)
                .ToList();

            if (matchedGuids.Count == 0)
            {
                markdowns = [];
            }
            else
            {
                var fetched = await query
                    .Where(m => matchedGuids.Contains(m.MarkDownGuid))
                    .ToListAsync(cancellationToken);
                var orderIndex = matchedGuids
                    .Select((g, i) => (g, i))
                    .ToDictionary(x => x.g, x => x.i);
                markdowns = fetched
                    .OrderBy(m => orderIndex[m.MarkDownGuid])
                    .ToList();
            }
        }

        var result = markdowns
            .Select(m => new MarkdownSummaryResponse
            {
                MarkDownGuid = m.MarkDownGuid,
                Name = m.MarkDownName,
                Tags = [.. m.MarkDownTagboard],
                CoverUrl = m.CoverUrl,
                Auth = m.MarkDownAuth.ToString(),
                Status = m.Status.ToString(),
                CreateAt = m.CreateAt,
                UpdateAt = m.UpdateAt
            })
            .ToList();

        logger.LogInformation("文章列表查询完成，共 {Count} 条（Tag={Tag}, User={User}）", result.Count, request.Tag, request.UserGuid);
        return result;
    }
}