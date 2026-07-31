using Markdown.Domain.IRepository;


namespace Markdown.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarkdownInfrastructure(this IServiceCollection services)
    {
        // 仓储（仅暴露聚合根仓储）
        services.AddScoped<IMarkdownRepository, MarkDownRepository>();

        return services;
    }
}
