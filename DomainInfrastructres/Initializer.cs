

using DomainCommon;

using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public class Initializer : IModuleInitializer
{

    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<INotDateTime, NotDateTime>();
        service.AddScoped<INotMemoryCache, NotMemoryCache>();
    }
}