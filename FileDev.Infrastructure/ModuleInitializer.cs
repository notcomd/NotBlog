using DomainCommons;
using DomainCommonst;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace FileDev.Infrastructure;

public class ModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<INotFileGroupRepository, NotFileGroupRepository>();
        service.AddScoped<INotFileRepository, NotFileRepository>();
    }
}