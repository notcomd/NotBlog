using Markdown.Domain.IRepository;
using Markdown.Infrastructure.Idempotent;


namespace Markdown.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarkdownInfrastructure(this IServiceCollection services)
    {
        // 仓储（仅暴露聚合根仓储）
        services.AddScoped<IMarkdownRepository, MarkDownRepository>();

        // 幂等性请求管理
        services.AddScoped<IRequestManagement, RequestManagement>();

        return services;
    }
}
