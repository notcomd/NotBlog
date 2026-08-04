using Microsoft.Extensions.DependencyInjection;

namespace Commons.Core;

public interface IModuleInitializer
{
    public void Initialize(IServiceCollection service);
}
