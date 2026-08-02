using Markdown.Web.API.Application.Dto;

namespace Markdown.Web.API.Application.Queries;

/// <summary>
/// 文章搜索查询（按名称/内容/标签，摘要投影，仅已审核通过且对查看者可见的文章）
/// </summary>
public record MarkdownSearchQuery(
    string Keyword,
    int Skip = 0,
    int Take = 20,
    Guid? ViewerGuid = null
) : IRequest<List<MarkdownSummaryResponse>>;