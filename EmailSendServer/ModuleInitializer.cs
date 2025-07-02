using DomainCommonst;
using Microsoft.Extensions.DependencyInjection;

namespace EmailSendServer;

public class ModuleInitializer : IModuleInitializer
{

    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<IEmail, Email>();
    }
}