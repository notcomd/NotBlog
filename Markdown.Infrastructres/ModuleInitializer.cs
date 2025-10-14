

using DomainCommon;

using Markdown.Domain.IRepository;
using Markdown.Infrastructres.Repository;

using Microsoft.Extensions.DependencyInjection;

namespace Markdown.Infrastructures;

public class ModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<IMarkDownGroupRepository, MarkDownGroupRepository>();
        service.AddScoped<IMarkdownRepository, MarkDownRepository>();
        service.AddScoped<IMarkReviewRepository, MarkReviewRepository>();
    }
}
