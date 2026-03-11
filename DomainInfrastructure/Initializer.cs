using DomainCommons;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public class Initializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        // service.AddScoped<BaseDbContext>();
        // throw new NotImplementedException();
        //service.AddDbContext<BaseDbContext>();
    }
}