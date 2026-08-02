using Markdown.Infrastructure.EntityFramework;
using Markdown.Web.API.Application.Dto;
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

        var markdowns = await dbContext.Markdowns.AsNoTracking()
            .Where(m => !m.IsDelete
                        && m.Status == MarkStatus.MarkApproved
                        && (m.MarkDownAuth == MarkDownAuth.PublicMark || m.MarkUserGuid == viewer)
                        && (m.MarkDownName.Contains(keyword) || m.MarkDownContent.Contains(keyword)))
            .OrderByDescending(m => m.CreateAt)
            .ToListAsync(cancellationToken);

        // 标签模糊匹配（Tagboard 为 JSON 文本列，无法在服务端翻译，取回后内存过滤）
        markdowns = markdowns
            .Where(m => m.MarkDownName.Contains(keyword)
                        || m.MarkDownContent.Contains(keyword)
                        || m.MarkDownTagboard.Any(t => t.Contains(keyword)))
            .ToList();

        var result = markdowns
            .Skip(Math.Max(0, request.Skip))
            .Take(Math.Clamp(request.Take, 1, 100))
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