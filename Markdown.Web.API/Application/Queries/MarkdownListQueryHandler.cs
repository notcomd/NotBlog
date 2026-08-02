using Markdown.Infrastructure.EntityFramework;
using Markdown.Web.API.Application.Dto;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Application.Queries;

/// <summary>
/// 文章列表查询处理器：仅返回已审核通过且对当前查看者可见的文章（摘要投影，不返回正文）
/// 可见性规则（与 S-10 私有过滤一致）：公开文章所有人可见，非公开文章仅作者可见
/// </summary>
public class MarkdownListQueryHandler(
    MarkDownDbContext dbContext,
    ILogger<MarkdownListQueryHandler> logger) : NotMediator.IRequestHandler<MarkdownListQuery, List<MarkdownSummaryResponse>>
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

        var markdowns = await query
            .OrderByDescending(m => m.CreateAt)
            .ToListAsync(cancellationToken);

        // 标签过滤：Tagboard 为 JSON 文本列，无法在服务端翻译，取回后内存过滤
        if (!string.IsNullOrWhiteSpace(request.Tag))
            markdowns = markdowns.Where(m => m.MarkDownTagboard.Contains(request.Tag)).ToList();

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

        logger.LogInformation("文章列表查询完成，共 {Count} 条（Tag={Tag}, User={User}）", result.Count, request.Tag, request.UserGuid);
        return result;
    }
}