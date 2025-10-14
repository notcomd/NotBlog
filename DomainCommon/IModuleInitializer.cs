using Microsoft.Extensions.DependencyInjection;

namespace DomainCommon;

public interface IModuleInitializer
{   
    public void Initialize(IServiceCollection service);
}