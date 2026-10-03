
namespace Markdown.Web.API.Application.Queries.Markdown;

/// <summary>
/// 文章列表查询（分页/按标签/按用户，摘要投影，仅已审核通过且对查看者可见的文章）
/// </summary>
public record MarkdownListQuery(
    int Skip = 0,
    int Take = 20,
    string? Tag = null,
    Guid? UserGuid = null,
    Guid? ViewerGuid = null
) : IRequest<List<MarkdownSummaryResponse>>;