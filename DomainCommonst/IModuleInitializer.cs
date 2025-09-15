using Microsoft.Extensions.DependencyInjection;

namespace DomainCommonst;

public interface IModuleInitializer
{   
    public void Initialize(IServiceCollection service);
}