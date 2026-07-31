namespace Markdown.Web.API.Apis;

/// <summary>
///     服务提供者聚合记录，仅暴露聚合根仓储 IMarkdownRepository（遵循 DDD 聚合根访问规则）
/// </summary>
public record IDServiceProvider(INotMediator NotMediator, 
ICurrentUserService CurrentUserService,
 IMarkdownRepository MarkdownRepository);
