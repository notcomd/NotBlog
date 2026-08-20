using Markdown.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Application.Queries;

/// <summary>
/// 文章搜索查询处理器：按名称/内容/标签模糊搜索，仅返回已审核通过且对当前查看者可见的文章（摘要投影）
/// </summary>
public class MarkdownSearchQueryHandler(
    MarkDownDbContext dbContext,
    ILogger<MarkdownSearchQueryHandler> logger) :  IRequestHandler<MarkdownSearchQuery, List<MarkdownSummaryResponse>>
{
    public async Task<List<MarkdownSummaryResponse>> Handler(MarkdownSearchQuery request, CancellationToken cancellationToken)
    {
        var keyword = request.Keyword?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var viewer = request.ViewerGuid;

        // 分页直接下推 SQL。正文已文件化（不存 DB），搜索仅匹配元数据（名称）；
        // 如需全文检索，后续引入 PG tsvector + GIN 或外部搜索索引。
        var markdowns = await dbContext.Markdowns.AsNoTracking()
            .Where(m => !m.IsDelete
                        && m.Status == MarkStatus.MarkApproved
                        && (m.MarkDownAuth == MarkDownAuth.PublicMark || m.MarkUserGuid == viewer)
                        && m.MarkDownName.Contains(keyword))
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