using Microsoft.Extensions.DependencyInjection;

namespace DomainCommons;

public interface IModuleInitializer
{
    public void Initialize(IServiceCollection service);
}