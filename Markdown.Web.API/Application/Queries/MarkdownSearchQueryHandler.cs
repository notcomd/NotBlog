using Markdown.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Application.Queries;

/// <summary>
/// 文章搜索查询处理器：按名称/内容/标签模糊搜索，仅返回已审核通过且对当前查看者可见的文章（摘要投影）
/// </summary>
public class MarkdownSearchQueryHandler(
    MarkDownDbContext dbContext,
    ILogger<MarkdownSearchQueryHandler> logger) : NotMediator.IRequestHandler<MarkdownSearchQuery, List<MarkdownSummaryResponse>>
{
    public async Task<List<MarkdownSummaryResponse>> Handler(MarkdownSearchQuery request, CancellationToken cancellationToken)
    {
        var keyword = request.Keyword?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var viewer = request.ViewerGuid;

        // 分页直接下推 SQL。注：MarkDownContent.Contains 会被翻译为 LIKE '%kw%'，
        // 前导通配符无法走索引，对 1MB 级正文列是全表扫描——建议后续引入 PG 全文检索
        // （tsvector + GIN）或外部搜索索引替代。
        var markdowns = await dbContext.Markdowns.AsNoTracking()
            .Where(m => !m.IsDelete
                        && m.Status == MarkStatus.MarkApproved
                        && (m.MarkDownAuth == MarkDownAuth.PublicMark || m.MarkUserGuid == viewer)
                        && (m.MarkDownName.Contains(keyword) || m.MarkDownContent.Contains(keyword)))
            .OrderByDescending(m => m.CreateAt)
            .Skip(Math.Max(0, request.Skip))
            .Take(Math.Clamp(request.Take, 1, 100))
            .ToListAsync(cancellationToken);

        var result = markdowns
            .Select(m => new MarkdownSummaryResponse
            {
                MarkDownGuid = m.MarkDownGuid,
                Name = m.MarkDownName,
                Tags = [.. m.MarkDownTagboard],
                Auth = m.MarkDownAuth.ToString(),
                Status = m.Status.ToString(),
                CreateAt = m.CreateAt,
                UpdateAt = m.UpdateAt
            })
            .ToList();

        logger.LogInformation("文章搜索完成，共 {Count} 条（Keyword={Keyword}）", result.Count, keyword);
        return result;
    }
}